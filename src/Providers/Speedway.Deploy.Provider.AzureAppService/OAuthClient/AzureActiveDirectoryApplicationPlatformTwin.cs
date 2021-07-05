using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Speedway.AzureSdk.Extensions;
using Speedway.Core.Resources;
using Speedway.Deploy.Core;
using Speedway.Deploy.Core.Resources.Speedway;
using Speedway.Deploy.Core.Resources.Speedway.OAuthClient;
using AppRole = Microsoft.Graph.AppRole;

namespace Speedway.Deploy.Provider.AzureAppService.OAuthClient
{
    // ReSharper disable once ClassNeverInstantiated.Global
    internal class AzureActiveDirectoryApplicationPlatformTwin : ISpeedwayOAuthClientPlatformTwin, IHaveAnAzureServicePrincipal
    {
        private readonly ILogger _logger;
        private readonly IGraphServiceClient _graphClient;
        private readonly SpeedwayOAuthClientResource _resource;
        private Application? _application;
        private ServicePrincipal? _servicePrincipal;
        private string? _secret;

        public AzureActiveDirectoryApplicationPlatformTwin(
            ILogger<AzureActiveDirectoryApplicationPlatformTwin> logger,
            IGraphServiceClient graphClient,
            SpeedwayOAuthClientResource resource)
        {
            _logger = logger;
            _graphClient = graphClient;
            _resource = resource;
        }

        public async Task<SpeedwayResourceOutputMetadata> Reflect()
        {
            await CreateActiveDirectoryApplication(
                _resource.Name,
                _resource.ResourceMetadata.SignOnUri,
                _resource.ResourceMetadata.ReplyUrls ?? Array.Empty<string>());

            await CreateServicePrincipal(_application!);
            await SetupRolesExperimental(_application!);
            await SetupScopesExperimental(_application!);

            //Make sure we've got the latest versions including any patches
            _application = await _graphClient.Applications[_application!.Id].Request().GetAsync();
            _servicePrincipal = await _graphClient.ServicePrincipals[_servicePrincipal!.Id].Request().GetAsync();

            if (_secret != null)
            {
                return new SpeedwayOAuthClientResourceOutputMetadata(
                    _application.DisplayName,
                    _application.AppId, new Dictionary<string, string>
                    {
                        {"ClientSecret", _secret}
                    });
            }

            return new SpeedwayOAuthClientResourceOutputMetadata(
                _application.DisplayName,
                _application.AppId,
                new Dictionary<string, string>());
        }

        private async Task SetupRolesExperimental(Application application)
        {
            if (_resource.ResourceMetadata.Roles == null) return;

            var newRoles =
                _resource.ResourceMetadata.Roles.Where(x => application.AppRoles.All(y => x.Name != y.Value))
                    .ToArray();

            if (newRoles.Any())
            {
                var updatedApplication = new Application()
                {
                    AppRoles = application.AppRoles.Concat(newRoles.Select(x => new AppRole()
                    {
                        Id = Guid.NewGuid(),
                        AllowedMemberTypes = x.AllowedTypes.Select(allowedType => allowedType.ToString()),
                        DisplayName = x.Name,
                        Description = x.Name,
                        Value = x.Name,
                        IsEnabled = true
                    }))
                };
                await _graphClient.Applications[application.Id].Request().UpdateAsync(updatedApplication);
            }
        }

        private async Task SetupScopesExperimental(Application application)
        {
            if (_resource.ResourceMetadata.Scopes == null) return;

            var newScopes = _resource.ResourceMetadata.Scopes
                .Where(x => application.Api.Oauth2PermissionScopes.All(y => x.Name != y.Value))
                .ToArray();

            if (newScopes.Any())
            {
                var updatedApplication = new Application()
                {
                    Api = new ApiApplication
                    {
                        Oauth2PermissionScopes = application.Api.Oauth2PermissionScopes
                            .Concat(newScopes.Select(x => new PermissionScope()
                            {
                                Id = Guid.NewGuid(),
                                Value = x.Name,
                                IsEnabled = true,
                                Type = "User",
                                AdminConsentDescription = $"Consent to {x}",
                                UserConsentDescription = $"Consent to {x}",
                                AdminConsentDisplayName = x.Name,
                                UserConsentDisplayName = x.Name
                            }))
                    }
                };
                await _graphClient.Applications[application.Id].Request().UpdateAsync(updatedApplication);
            }
        }

        private async Task CreateServicePrincipal(Application application)
        {
            var sps = await _graphClient.ServicePrincipals.Request().Filter($"appId eq '{application.AppId}'")
                .GetAsync();
            var sp = sps.SingleOrDefault();
            if (sp == null)
            {
                _logger.LogInformation(
                    "Creating new Azure Active Directory service principal to represent application {Name}",
                    application.DisplayName);

                sp = await _graphClient.ServicePrincipals.Request().AddAsync(new ServicePrincipal()
                {
                    AppId = application.AppId,
                });
            }

            _servicePrincipal = await _graphClient.WaitForGraph(x => x.ServicePrincipals[sp.Id].Request().GetAsync());
        }

        private async Task CreateActiveDirectoryApplication(string name, string signOnUrl, string[] replyUrls)
        {
            var aadApplications = await _graphClient.Applications.Request()
                .Filter($"tags/any(c:c eq '{_resource.Context.Manifest.Id}')")
                .GetAsync();

            var aadApplication = aadApplications.SingleOrDefault(x =>
                x.Tags.Contains(_resource.Name) && x.Tags.Contains(_resource.Context.Environment));

            if (aadApplication == null)
            {
                _logger.LogInformation("Creating new Azure Active Directory application {Name}", name);

                var applicationName = AzureEx.SuggestResourceName(_resource.Context, name, 50);
                var applicationDefinition = new Application()
                {
                    DisplayName = applicationName,
                    Tags = new[]
                        {_resource.Context.Manifest.Id.ToString(), _resource.Context.Environment, _resource.Name},
                    SignInAudience = GetSignInAudience()
                };

                if (_resource.ResourceMetadata.ClientType == SpeedwayClientType.Public)
                {
                    applicationDefinition.PublicClient = new PublicClientApplication
                    {
                        RedirectUris = replyUrls,
                    };
                }
                else
                {
                    applicationDefinition.Web = new WebApplication()
                    {
                        RedirectUris = replyUrls,
                        HomePageUrl = signOnUrl,
                        ImplicitGrantSettings = new ImplicitGrantSettings()
                        {
                            EnableAccessTokenIssuance = false,
                            EnableIdTokenIssuance = true
                        }
                    };
                }

                _application = await _graphClient.Applications.Request().AddAsync(applicationDefinition);

                await _graphClient.Applications[_application.Id].Request().UpdateAsync(new Application
                {
                    IdentifierUris = new[] {$"api://{_application.AppId}"}
                });

                if (_resource.ResourceMetadata.ClientType == SpeedwayClientType.Web)
                {
                    var password = await _graphClient.Applications[_application.Id].AddPassword(new PasswordCredential()
                    {
                        DisplayName = "Speedway Client Secret",
                    }).Request().PostAsync();
                    _secret = password.SecretText;
                }

                _logger.LogInformation("Created new Azure Active Directory application representing {Name}", name);
            }
            else
            {
                _logger.LogInformation("Found existing Azure Active Directory application representing {Name}", name);
                _application =
                    await _graphClient.WaitForGraph(x => x.Applications[aadApplication.Id].Request().GetAsync());
            }
        }

        // ReSharper disable once CommentTypo
        /// <summary>
        /// On Behalf of flow doesn't work if this isn't set properly. There goes 2 hours of my life.
        /// https://docs.microsoft.com/en-us/azure/active-directory/develop/reference-app-manifest
        /// You end up with a application that has accessTokenAcceptedVersion set to 2. But this doesn't get a token that appears to work with on behalf of. You need it set to null (as
        /// per when you create apps from the CLI). Using AzureADMyOrg allows that. Maybe AzureADMultipleOrgs does as-well.  
        /// </summary>
        /// <returns></returns>
        private static string GetSignInAudience()
        {
            return "AzureADMyOrg";
        }

        public Task<ServicePrincipal> ServicePrincipal => Task.FromResult(_servicePrincipal!);

        
        public async Task RequestApplicationAccess(string[] roles, ISpeedwayResource allowedApplication)
        {
            var otherServicePrincipal = await allowedApplication.GetPlatformTwin<IHaveAnAzureServicePrincipal>().ServicePrincipal;
            await _graphClient.ApplyRoles(await ServicePrincipal, otherServicePrincipal, roles);
        }

        public async Task RequestDelegatedAccess(string[] scopes, ISpeedwayResource applicationThatWantsToDelegateThis)
        {
            var otherServicePrincipal = await applicationThatWantsToDelegateThis.GetPlatformTwin<IHaveAnAzureServicePrincipal>().ServicePrincipal;
            await _graphClient.ApplyScopes(
                await ServicePrincipal,
                otherServicePrincipal,
                scopes);
        }

        public async Task SetKnownApplications(ISpeedwayPlatformTwin knownApplication)
        {
            var twinWithServicePrincipal = (IHaveAnAzureServicePrincipal) knownApplication;
            var knownClientAppId = Guid.Parse((await twinWithServicePrincipal.ServicePrincipal).AppId);
            if (!_application!.Api.KnownClientApplications.Contains(knownClientAppId))
            {
                _logger.LogInformation("Adding {KnownAppId} to known clients for {AppName}", knownClientAppId,
                    _application!.DisplayName);
                var updatedApplication = new Application()
                {
                    Api = new ApiApplication()
                    {
                        KnownClientApplications =
                            _application.Api.KnownClientApplications.Concat(new[] {knownClientAppId})
                    }
                };
                await _graphClient.Applications[_application.Id].Request().UpdateAsync(updatedApplication);
            }
            else
            {
                _logger.LogInformation("{KnownAppId} already exists in known clients for {AppName}", knownClientAppId,
                    _application!.DisplayName);
            }
        }

        public async Task AddRedirects(IEnumerable<string> redirectUris)
        {
            var asArray = redirectUris as string[] ?? redirectUris.ToArray();
            
            _logger.LogInformation("Adding redirect Uris: {Uris} to application {AppName}",
                string.Join(",", asArray), _application!.DisplayName);

            var updatedApplication = new Application()
            {
                Web = new WebApplication()
                {
                    RedirectUris = _application!.Web.RedirectUris.Union(asArray)
                }
            };
            await _graphClient.Applications[_application.Id].Request().UpdateAsync(updatedApplication);
        }
    }
}