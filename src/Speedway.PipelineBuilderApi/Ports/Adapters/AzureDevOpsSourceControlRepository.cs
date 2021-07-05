using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.AzureAD.UI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using Newtonsoft.Json;
using Speedway.Api.Extensions;
using Speedway.PipelineBuilderApi.Ports.Adapters.DevOpsTypes;
using Operation = Speedway.PipelineBuilderApi.Ports.Adapters.DevOpsTypes.Operation;

namespace Speedway.PipelineBuilderApi.Ports.Adapters
{
    class AzureDevOpsSourceControlRepository : ISourceControlRepository
    {
        private readonly IDownstreamWebApi _downstreamWebApi;
        private readonly IOptions<PipelineBuilderSettings> _settings;
        private readonly IOptions<AzureADOptions> _azureAdSettings;
        private readonly ILogger<AzureDevOpsSourceControlRepository> _logger;

        public AzureDevOpsSourceControlRepository(
            IDownstreamWebApi downstreamWebApi,
            IOptions<PipelineBuilderSettings> settings,
            IOptions<AzureADOptions> azureAdSettings,
            ILogger<AzureDevOpsSourceControlRepository> logger)
        {
            _downstreamWebApi = downstreamWebApi;
            _settings = settings;
            _azureAdSettings = azureAdSettings;
            _logger = logger;
        }

        public async Task<Domain.Project[]> List()
        {
            _logger.LogInformation("Listing containers in Speedway");

            var result = await _downstreamWebApi.GetForUserSafeAsync<DevOpsListResponse<Project>>(
                "DevOps",
                $"_apis/projects?api-version=6.0");

            return result.Value.Select(x => new Domain.Project(x.Id, x.Name, x.Url)).ToArray();
        }

        public async Task<NewRepositoryResponse> New(string slug, string displayName, string initialTeamMemberEmail,
            Guid initialTeamMemberId)
        {
            var project = (await List()).FirstOrDefault(x => x.Name == slug);
            var shouldAddEnvironmentGates = true;
            if (project == null)
            {
                project = await CreateDevopsProject(slug, displayName);
            }
            else
            {
                _logger.LogInformation("Project {slug} already exists. Brining it to correct state.", slug);

                //No way to check existence via API right now and I don't want to keep doubling these up :(
                shouldAddEnvironmentGates = false;
            }

            var serviceEndpoint = await CreateServiceEndpointInProjectForDeployment(project.Id, slug);

            var repositories = await _downstreamWebApi.GetForUserAsync<DevOpsListResponse<Repository>>(
                "DevOps",
                $"{project.Id}/_apis/git/repositories?api-version=6.0");

            var repository = await PostManifestFileToMain(project!, slug, displayName, initialTeamMemberEmail,
                repositories!.Value[0]);
            await SetupBranchPolicyOnDevOpsProject(project, repository);

            await CreatePipeline(project.Id, repository, serviceEndpoint!.Id);

            if (shouldAddEnvironmentGates)
            {
                // TODO - call policy check api. Cannot use Azure AD for this. Just a generic rest call. This will need another service endpoint.
                var policyServiceEndpoint = await CreateServiceEndpointForPolicyApi(project.Id, slug);
                await SetupEnvironmentChecksOnDevOpsProject(project, policyServiceEndpoint.Name, initialTeamMemberId);
            }

            return new NewRepositoryResponse(project.Id, project.Name, project.Url, repository.RemoteUrl);
        }

        private async Task SetupBranchPolicyOnDevOpsProject(Domain.Project project, Repository repository)
        {
            var minimumApprovals = "fa4e907d-c16b-4a4c-9dfa-4906e5d171dd";

            var existing = await _downstreamWebApi.GetForUserSafeAsync<DevOpsListResponse<Policy>>(
                "DevOps",
                $"{project.Id}/_apis/policy/configurations?repositoryId={repository.Id}?api-version=6.0-preview");

            var minimumApproverPolicy = existing.Value.Where(x => x.Type.Id == minimumApprovals);
            var minimumApproverScopes =
                minimumApproverPolicy.SelectMany(x => x.Settings["scope"].ToObject<PolicyScope[]>());
            var hasExistingPolicy = minimumApproverScopes.Any(x => x.RepositoryId == repository.Id);
            if (hasExistingPolicy)
            {
                return;
            }

            var repositoryScope = new
            {
                repositoryId = repository.Id,
                refName = "refs/heads/main",
                matchKind = "exact"
            };

            await _downstreamWebApi.PostForUserAsync<Policy, object>(
                "DevOps",
                $"{project.Id}/_apis/policy/configurations?api-version=6.1-preview",
                new
                {
                    isEnabled = true,
                    isBlocking = true,
                    type = new
                    {
                        id = minimumApprovals //minimum approvalS
                    },
                    settings = new
                    {
                        minimumApproverCount = 1,
                        resetOnSourcePush = true,
                        creatorVoteCounts = true, //TODO - in the real world this would be false!!
                        scope = new[] {repositoryScope}
                    }
                });
        }

        /// <summary>
        /// https://stackoverflow.com/questions/61471634/add-remove-pipeline-checks-using-rest-api
        /// Eek - surely this will become a better api at some point :(
        /// </summary>
        /// <returns></returns>
        private async Task SetupEnvironmentChecksOnDevOpsProject(Domain.Project project, string serviceEndpointName,
            Guid initialUserId)
        {
            var environments = await _downstreamWebApi.GetForUserSafeAsync<DevOpsListResponse<SpeedwayEnvironment>>(
                "DevOps",
                $"{project.Id}/_apis/distributedtask/environments?api-version=6.0-preview.1");

            foreach (var env in _settings.Value.Environments)
            {
                var environmentInfo = environments.Value.Single(x => x.Name == env);

                var policyApiCallCheck = BuildPolicyApiCheckCall(project.Id, serviceEndpointName, environmentInfo);
                await _downstreamWebApi.PostForUserAsync<object, object>(
                    "DevOps",
                    $"{project.Id}/_apis/pipelines/checks/configurations?api-version=6.0-preview.1",
                    policyApiCallCheck);
            }

            foreach (var env in _settings.Value.Environments.Except(new[] {"Development"}))
            {
                var environmentInfo = environments.Value.Single(x => x.Name == env);
                var manualApprovalPolicy = BuildManualCheckCall(environmentInfo, initialUserId);

                await _downstreamWebApi.PostForUserAsync<object, object>(
                    "DevOps",
                    $"{project.Id}/_apis/pipelines/checks/configurations?api-version=6.0-preview.1",
                    manualApprovalPolicy);
            }

            foreach (var env in _settings.Value.Environments.Except(new[] {"Development"}))
            {
                var environmentInfo = environments.Value.Single(x => x.Name == env);
                var branchCheckPolicy = BuildMainOrReleaseBranchCheckCall(environmentInfo);

                await _downstreamWebApi.PostForUserAsync<object, object>(
                    "DevOps",
                    $"{project.Id}/_apis/pipelines/checks/configurations?api-version=6.0-preview.1",
                    branchCheckPolicy);
            }
        }

        private static object BuildPolicyApiCheckCall(Guid projectId, string serviceEndpointName,
            SpeedwayEnvironment environmentInfo)
        {
            var taskCheckId = "fe1de3ee-a436-41b4-bb20-f6eb4cb879a7";

            var policyApiCallCheck = new
            {
                type = new
                {
                    id = taskCheckId,
                    name = "Policy Api check"
                },
                settings = new
                {
                    definitionRef = new
                    {
                        id = "9C3E8943-130D-4C78-AC63-8AF81DF62DFB", //invoke rest api 
                        name = "InvokeRESTAPI",
                        version = "1.152.3"
                    },
                    displayName = "Invoke Rest API",
                    inputs = new
                    {
                        connectedServiceNameSelector = "connectedServiceName",
                        connectedServiceName = serviceEndpointName,
                        method = "POST",
                        body = JsonConvert.SerializeObject(new
                        {
                            SpeedwayProjectId = projectId,
                            Environment = environmentInfo.Name,
                            BuildId = "$(Build.BuildId)",
                            Release = "$(Build.gi)"
                        }),
                        waitForCompletion = "false",
                        successCriteria = "eq(root['policyEvaluation']['satisfied'], true)",
                        urlSuffix = "compliance" // These are all task inputs
                    },
                    executionOrder = 2,
                    retryInterval = 5, // The re-try time specified.
                },
                resource = new
                {
                    type = "environment",
                    id = environmentInfo.Id,
                    name = environmentInfo.Name
                },
                timeout = 43200
            };
            return policyApiCallCheck;
        }

        private object BuildMainOrReleaseBranchCheckCall(SpeedwayEnvironment environmentInfo)
        {
            var branchCheckId = "fe1de3ee-a436-41b4-bb20-f6eb4cb879a7";

            var branchCheckPolicy = new
            {
                type = new
                {
                    id = branchCheckId,
                    name = "Task Check"
                },
                settings = new
                {
                    definitionRef = new
                    {
                        id="86b05a0c-73e6-4f7d-b3cf-e38f3b39a75b",
                        name="evaluatebranchProtection",
                        version="0.0.1"
                    },
                    displayName = "Restrict Branches",
                    inputs = new
                    {
                        allowedBranches = "refs/heads/master,refs/heads/main,refs/heads/release*",
                        ensureProtectionOfBranch = true,
                        allowUnknownStatusBranch = false
                    },
                    executionOrder = 1,
                },
                resource = new
                {
                    id = environmentInfo.Id,
                    name = environmentInfo.Name,
                    type = "environment"
                },
                timeout = 43200
            };
            return branchCheckPolicy;
        }

        private object BuildManualCheckCall(SpeedwayEnvironment environmentInfo, Guid initialUserId)
        {
            var approvalCheckId = "8C6F20A7-A545-4486-9777-F762FAFE0D4D";

            var manualApprovalPolicy = new
            {
                type = new
                {
                    id = approvalCheckId,
                    name = "Approval"
                },
                settings = new
                {
                    approvers = new[]
                    {
                        new
                        {
                            id = initialUserId
                        }
                    },
                    executionOrder = 3,
                    instructions = $"Approve the build to allow it to release to {environmentInfo.Name}",
                    minRequiredApprovers = 1,
                    requesterCannotBeApprover = false
                },
                resource = new
                {
                    type = "environment",
                    id = environmentInfo.Id,
                    name = environmentInfo.Name
                },
                timeout = 43200
            };
            return manualApprovalPolicy;
        }


        private async Task<Domain.Project> CreateDevopsProject(string slug, string displayName)
        {
            var project = await CreateDevOpsProject(slug, displayName);
            return project;
        }

        private async Task<Domain.Project> CreateDevOpsProject(string slug, string displayName)
        {
            var operation = await _downstreamWebApi.PostForUserAsync<Operation, object>(
                "DevOps",
                $"_apis/projects?api-version=6.0",
                new
                {
                    name = slug,
                    description = displayName,
                    visibility = "private",
                    capabilities = new
                    {
                        versioncontrol = new
                        {
                            sourceControlType = "Git",
                        },
                        processTemplate = new
                        {
                            templateTypeId = "6b724908-ef14-45cf-84f8-768b5384da45" //SCRUM
                        }
                    }
                });

            if (operation == null)
            {
                throw new InvalidOperationException("Failed to create repository....");
            }

            var project =
                await WaitForOperation<Domain.Project>(operation, $"_apis/projects/{slug}?api-version=6.0");
            return project;
        }

        private async Task CreatePipeline(Guid projectId, Repository repository, Guid serviceEndpointId)
        {
            var environments = await _downstreamWebApi.GetForUserSafeAsync<DevOpsListResponse<SpeedwayEnvironment>>(
                "DevOps",
                $"{projectId}/_apis/distributedtask/environments?api-version=6.0-preview.1");

            foreach (var environment in _settings.Value.Environments)
            {
                if (environments.Value.All(x => x.Name != environment))
                {
                    await _downstreamWebApi.PostForUserAsync<object, object>(
                        "DevOps",
                        $"{projectId}/_apis/distributedtask/environments?api-version=6.0-preview.1",
                        new
                        {
                            name = environment,
                            description = "Speedway generated environment"
                        }
                    );
                }
            }

            var pipelines = await _downstreamWebApi.GetForUserSafeAsync<DevOpsListResponse<SpeedwayEnvironment>>(
                "DevOps",
                $"{projectId}/_apis/pipelines?api-version=6.0-preview.1");

            if (pipelines.Count == 0 || pipelines.Value.All(x => x.Name != "speedway-pipeline"))
            {
                var pipeline = await _downstreamWebApi.PostForUserAsync<Pipeline, object>(
                    "DevOps",
                    $"{projectId}/_apis/pipelines?api-version=6.0-preview.1",
                    new
                    {
                        name = "speedway-pipeline",
                        folder = "\\",
                        configuration = new
                        {
                            path = "azure-pipelines.yml",
                            repository = new
                            {
                                id = repository.Id,
                                name = repository.Name,
                                type = "azureReposGit"
                            },
                            type = "yaml"
                        }
                    });

                await _downstreamWebApi.CallWebApiForUserAsync<object, object>(
                    "DevOps",
                    new
                    {
                        pipelines = new[]
                        {
                            new
                            {
                                id = pipeline!.Id,
                                authorized = true
                            }
                        },
                        resource = new
                        {
                            id = serviceEndpointId,
                            type = "endpoint"
                        }
                    }, downstreamWebApiOptions =>
                    {
                        downstreamWebApiOptions.HttpMethod = HttpMethod.Patch;
                        downstreamWebApiOptions.RelativePath =
                            $"{projectId}/_apis/pipelines/pipelinePermissions/endpoint/{serviceEndpointId}?api-version=6.0-preview.1";
                    });
            }
        }

        private async Task<ServiceEndpoint> CreateServiceEndpointInProjectForDeployment(Guid projectId,
            string slug)
        {
            //check for existing service endpoint
            var existing = await _downstreamWebApi.GetForUserSafeAsync<DevOpsListResponse<ServiceEndpoint>>(
                "DevOps",
                $"{projectId}/_apis/serviceendpoint/endpoints?api-version=6.0-preview");

            var newEndpointName = $"SpeedwayDeployment-{projectId}";
            ServiceEndpoint? serviceEndpoint;
            if (existing.Value.Any(x => x.Name == newEndpointName))
            {
                _logger.LogInformation("Found service endpoint named {name}. Will reuse it.", newEndpointName);
                serviceEndpoint = existing.Value.Single(x => x.Name == newEndpointName);
            }
            else
            {
                _logger.LogInformation("Creating new service endpoint named {name} to access Deploy API",
                    newEndpointName);

                serviceEndpoint = await _downstreamWebApi.PostForUserAsync<ServiceEndpoint, object>(
                    "DevOps",
                    $"_apis/serviceendpoint/endpoints?api-version=6.0-preview",
                    new
                    {
                        data = new
                        {
                            subscriptionId = _settings.Value.SubscriptionId,
                            subscriptionName = _settings.Value.SubscriptionName
                        },
                        name = newEndpointName,
                        type = "azurerm",
                        url = "https://management.azure.com/",
                        authorization = new
                        {
                            parameters = new
                            {
                                authenticationType = "spnKey",
                                tenantid = _azureAdSettings.Value.TenantId,
                                serviceprincipalid = _settings.Value.DevOpsPipelineAdApplicationId,
                                serviceprincipalkey = _settings.Value.DevOpsPipelineServicePrincipalSecret,
                            },
                            scheme = "ServicePrincipal"
                        },
                        isShared = false,
                        owner = "library",
                        serviceEndpointProjectReferences = new[]
                        {
                            new
                            {
                                projectReference = new
                                {
                                    id = projectId,
                                    name = slug
                                },
                                name = newEndpointName,
                            }
                        }
                    });

                if (serviceEndpoint == null)
                    throw new InvalidOperationException("Failed to create service endpoint in Devops project");
            }

            return serviceEndpoint;
        }

        private async Task<ServiceEndpoint> CreateServiceEndpointForPolicyApi(Guid projectId, string slug)
        {
            //check for existing service endpoint
            var existing = await _downstreamWebApi.GetForUserSafeAsync<DevOpsListResponse<ServiceEndpoint>>(
                "DevOps",
                $"{projectId}/_apis/serviceendpoint/endpoints?api-version=6.0-preview");

            var newEndpointName = $"SpeedwayDeployment-{projectId}-policy";
            ServiceEndpoint? serviceEndpoint;
            if (existing.Value.Any(x => x.Name == newEndpointName))
            {
                _logger.LogInformation("Found service endpoint named {name}. Will reuse it.", newEndpointName);
                serviceEndpoint = existing.Value.Single(x => x.Name == newEndpointName);
            }
            else
            {
                _logger.LogInformation("Creating new service endpoint named {name} to access Deploy API",
                    newEndpointName);

                serviceEndpoint = await _downstreamWebApi.PostForUserAsync<ServiceEndpoint, object>(
                    "DevOps",
                    $"_apis/serviceendpoint/endpoints?api-version=6.0-preview",
                    new
                    {
                        name = newEndpointName,
                        type = "Generic",
                        url = _settings.Value.PolicyApiUriBase,
                        authorization = new
                        {
                            parameters = new
                            {
                                username = "speedway-devops-pipeline",
                                password = _settings.Value.PolicyApiAuthorisationToken
                            },
                            scheme = "UsernamePassword"
                        },
                        isShared = false,
                        owner = "library",
                        serviceEndpointProjectReferences = new[]
                        {
                            new
                            {
                                projectReference = new
                                {
                                    id = projectId,
                                    name = slug
                                },
                                name = newEndpointName,
                            }
                        }
                    });

                if (serviceEndpoint == null)
                    throw new InvalidOperationException("Failed to create service endpoint in Devops project");
            }

            return serviceEndpoint;
        }

        private async Task<Repository> PostManifestFileToMain(
            Domain.Project project,
            string slug,
            string displayName,
            string initialTeamMember,
            Repository repository)
        {
            //only do if there are no pushes to master. We won't update a repository otherwise.
            var existingPushes = await _downstreamWebApi.GetForUserSafeAsync<DevOpsListResponse<Push>>(
                "DevOps",
                $"{project.Id}/_apis/git/repositories/{repository.Id}/pushes?api-version=6.0");

            if (existingPushes.Count > 0)
            {
                _logger.LogInformation("Detected existing push to repository. Will not push again");
                return existingPushes.Value[0].Repository;
            }

            _logger.LogInformation("Pushing initial content to repository");

            var manifestContent = InitialContent.SpeedwayManifest(project.Id, slug, displayName, initialTeamMember);
            var pipelineContent = InitialContent.SpeedwayDevopsPipeline(
                project.Id,
                _settings.Value.ArtifactStorageName,
                _settings.Value.DeployApiUri,
                _settings.Value.DeployApiAzureAdApplicationId);

            var buildShContent = InitialContent.BuildSh();
            var gitIgnoreContent = InitialContent.GitIgnore();

            var post = new
            {
                refUpdates = new[]
                {
                    new
                    {
                        name = "refs/heads/main",
                        oldObjectId = "0000000000000000000000000000000000000000"
                    }
                },
                commits = new[]
                {
                    new
                    {
                        comment = "Manifest commit generated by Speedway",
                        changes = new[]
                        {
                            new
                            {
                                changeType = "add",
                                item = new
                                {
                                    path = "/.speedway/manifest.json"
                                },
                                newContent = new
                                {
                                    content = manifestContent.SerializeToJson(),
                                    contentType = "rawText"
                                }
                            },
                            new
                            {
                                changeType = "add",
                                item = new
                                {
                                    path = "/azure-pipelines.yml"
                                },
                                newContent = new
                                {
                                    content = pipelineContent,
                                    contentType = "rawText"
                                }
                            },
                            new
                            {
                                changeType = "add",
                                item = new
                                {
                                    path = "/build.sh"
                                },
                                newContent = new
                                {
                                    content = buildShContent,
                                    contentType = "rawText"
                                }
                            },
                            new
                            {
                                changeType = "add",
                                item = new
                                {
                                    path = "/.gitignore"
                                },
                                newContent = new
                                {
                                    content = gitIgnoreContent,
                                    contentType = "rawText"
                                }
                            },
                            new
                            {
                                changeType = "add",
                                item = new
                                {
                                    path = "/VersionConfig.yml"
                                },
                                newContent = new
                                {
                                    content = InitialContent.GitVersionConfig,
                                    contentType = "rawText"
                                }
                            }
                        }
                    }
                }
            };

            await _downstreamWebApi.PostForUserAsync<object, object>(
                "DevOps",
                $"{project.Id}/_apis/git/repositories/{repository.Id}/pushes?api-version=6.0",
                post);
            return repository;
        }

        private async Task<T> WaitForOperation<T>(Operation operation, string resourceUri) where T : class

        {
            while (true)
            {
                await Task.Delay(1000);
                var operationStatus = await _downstreamWebApi.GetForUserSafeAsync<Operation>("DevOps",
                    $"_apis/operations/{operation.Id}?api-version=6.0");

                if (operationStatus.Status != "queued" && operationStatus.Status != "inProgress")
                {
                    return await _downstreamWebApi.GetForUserSafeAsync<T>("DevOps", resourceUri);
                }
            }
        }
    }
}