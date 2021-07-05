using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.KeyVault.Fluent.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Newtonsoft.Json;
using Speedway.AzureSdk.Extensions;
using Speedway.Core;
using Speedway.Core.Resources;
using Speedway.Deploy.Core;
using Speedway.Deploy.Core.Resources.Speedway;
using Speedway.Deploy.Core.Resources.Speedway.Context;
using Speedway.Deploy.Core.Resources.Speedway.OAuthClient;
using Speedway.Deploy.Core.Resources.Speedway.SecretStore;
using Speedway.Deploy.Core.Resources.Speedway.WebApp;

namespace Speedway.Bootstrap
{
    public class Bootstrapper
    {

        private readonly ILogger<Bootstrapper> _logger;
        private readonly IManifestDeployer _deployer;
        private readonly SpeedwayManifest _manifest;
        private readonly IAzure _az;
        private readonly IGraphServiceClient _graphServiceClient;
        private readonly IOptions<SpeedwayBootstrapSettings> _settings;
        private readonly InteractiveAzureCredentials _credentials;

        public Bootstrapper(
            ILogger<Bootstrapper> logger,
            IManifestDeployer deployer,
            SpeedwayManifest manifest,
            IAzure az, 
            IGraphServiceClient graphServiceClient,
            IOptions<SpeedwayBootstrapSettings> settings,
            InteractiveAzureCredentials credentials)
        {
            _logger = logger;
            _deployer = deployer;
            _manifest = manifest;
            _az = az;
            _graphServiceClient = graphServiceClient;
            _settings = settings;
            _credentials = credentials;
        }

        public async Task Execute()
        {
            _logger.LogInformation("Speedway running");
            try
            {
                await _deployer.Deploy(
                    "Development", _manifest, 
                    null, 
                    ApplyBootstrapUserSecretsWriteOnKeyVault);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to bootstrap Speedway");
                Environment.Exit(-1);
            }
        }

        /// <summary>
        /// Bootstrap user will need permissions to write to the secrets keyvault.
        /// </summary>
        /// <param name="resources"></param>
        private async Task ApplyBootstrapUserSecretsWriteOnKeyVault(IDictionary<ISpeedwayResource, SpeedwayResourceOutputMetadata> resources)
        {
            _logger.LogInformation(JsonConvert.SerializeObject(
                resources.ToDictionary(x => x.Key.GetMetadata().Name, x => x.Value), Formatting.Indented));

            var aadUserObjectId = _credentials.IdToken.Claims.First(x => x.Type == "oid").Value;
            var aadUser = await _graphServiceClient.Users[aadUserObjectId].Request().GetAsync();
            var resourceGroup = resources.Single(x => x.Key is SpeedwayContextResource).Value;
            var defaultKeyVault = resources.Single(x => x.Key.GetMetadata() is SpeedwaySecretContainerResourceMetadata && x.Key.Name == SpeedwaySecretContainerResource.DefaultName).Value;

            var vault = await _az.Vaults.GetByResourceGroupAsync(resourceGroup.PlatformResourceName, defaultKeyVault.PlatformResourceName);
            var existingPolicy = vault.AccessPolicies.SingleOrDefault(x => x.ObjectId == aadUser.Id);
            if (existingPolicy == null)
            {
                await vault.Update().DefineAccessPolicy().ForObjectId(aadUser.Id)
                    .AllowSecretPermissions(SecretPermissions.Get, SecretPermissions.List, SecretPermissions.Set)
                    .Attach()
                    .ApplyAsync();
            }

            await SetupAadPermissions(resources);

            _logger.LogInformation("Assigned permissions and verified secrets access in KeyVault");
        }

        private async Task SetupAadPermissions(IDictionary<ISpeedwayResource, SpeedwayResourceOutputMetadata> allResources)
        {
            _logger.LogInformation("Applying runtime permissions");

            var deployWebAppOutput = (SpeedwayWebAppResourceOutputMetadata)allResources[GetSpeedwayResourceWithIdentity(allResources, "speedway-deploy")];
            var deployOutput = (SpeedwayOAuthClientResourceOutputMetadata)allResources[GetSpeedwayResourceWithIdentity(allResources, "speedway-deploy-application")];
            var pipelineOutput = (SpeedwayOAuthClientResourceOutputMetadata)allResources[GetSpeedwayResourceWithIdentity(allResources, "speedway-pipeline-builder-application")];

            if (_settings.Value.Environment == "dev")
            {
                var aadSp = await _graphServiceClient.ServicePrincipals.Request().Filter($"appId eq '{deployOutput.ClientId}'").GetAsync();
                var servicePrincipalId = aadSp.Single().Id;
                await GrantServicePrincipalAzurePlatformRoleAtSubscriptionLevel(servicePrincipalId, "Owner");
            }
            
            await GrantServicePrincipalAzurePlatformRoleAtSubscriptionLevel(deployWebAppOutput.EphemeralIdentityId, "Owner");
        }


        private static ISpeedwayResource GetSpeedwayResourceWithIdentity(IDictionary<ISpeedwayResource, SpeedwayResourceOutputMetadata> allResources, string from)
        {
            return allResources.Keys.Single(x => x.Name == from);
        }

        private async Task GrantUserAccessToRole(string fromUserName, string principalExposingRoles, params string[] roles)
        {
            var servicePrincipalExposingRoles =  await _graphServiceClient.GetApplicationServicePrincipalSafe(principalExposingRoles);
            await _graphServiceClient.ApplyRolesToUser(_logger, servicePrincipalExposingRoles, fromUserName, roles);
        }
        private async Task GrantServicePrincipalAzurePlatformRoleAtSubscriptionLevel(string fromServicePrincipalId, string role)
        {
            var servicePrincipalRequestingAccess = await _graphServiceClient.ServicePrincipals[fromServicePrincipalId].Request().GetAsync();
            _logger.LogInformation("Granting {from} {role} access to subscription.", fromServicePrincipalId, role);
            await _az.ReflectRolesOnSubscription(servicePrincipalRequestingAccess, _logger, role);
        }
    }
}