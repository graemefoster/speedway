using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.KeyVault.Fluent;
using Microsoft.Azure.Management.KeyVault.Fluent.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Speedway.Core.Resources;
using Speedway.Deploy.Core;
using Speedway.Deploy.Core.Resources.Speedway;
using Speedway.Deploy.Core.Resources.Speedway.Context;
using Speedway.Deploy.Core.Resources.Speedway.SecretStore;
using Speedway.Deploy.Provider.AzureAppService.Context;

namespace Speedway.Deploy.Provider.AzureAppService.SecretStore
{
    // ReSharper disable once ClassNeverInstantiated.Global
    internal class KeyVaultSpeedwayPlatformStorageTwin : ISpeedwayPlatformTwin, ISecretStorePlatformTwin
    {
        private readonly ILogger<KeyVaultSpeedwayPlatformStorageTwin> _logger;
        private readonly IAzure _azure;
        private readonly SpeedwayContextResource _context;
        private readonly SpeedwaySecretContainerResource _resource;
        private readonly IOptions<AzureSpeedwaySettings> _settings;
        private readonly IGraphServiceClient _graphServiceClient;
        private IVault? _keyVault;
        private IPagedCollection<ISecret>? _allExistingSecrets;

        public KeyVaultSpeedwayPlatformStorageTwin(
            ILogger<KeyVaultSpeedwayPlatformStorageTwin> logger,
            IAzure azure,
            SpeedwaySecretContainerResource resource,
            IOptions<AzureSpeedwaySettings> settings,
            IGraphServiceClient graphServiceClient)
        {
            _logger = logger;
            _azure = azure;
            _resource = resource;
            _settings = settings;
            _graphServiceClient = graphServiceClient;
            _context = resource.Context;
        }

        public async Task<SpeedwayResourceOutputMetadata> Reflect()
        {
            var resourceGroupTwin = _context.GetPlatformTwin<ResourceGroupSpeedwayPlatformContextTwin>();
            _logger.LogDebug("Ensuring keyvault {ResourceGroup}/{Name} exists", resourceGroupTwin.Name, _resource.Name);

            var existingVaults = _azure.Vaults.ListByResourceGroup(resourceGroupTwin.Name);
            if (existingVaults.SpeedwayResourceExists(_resource, _context.Environment, out _keyVault))
            {
                _logger.LogInformation("Found key vault {Id} which represents {ResourceGroup}/{Name} ",
                    _keyVault!.Name, resourceGroupTwin.Name, _resource.Name);
            }
            else
            {
                var randomName = AzureEx.SuggestResourceName(_resource.Context, _resource.Name, 24, false);
                var rg = await _azure.ResourceGroups.GetByNameAsync(resourceGroupTwin.Name);

                _keyVault = await _azure.Vaults.Define(randomName)
                    .WithRegion(rg.Region)
                    .WithExistingResourceGroup(rg)
                    .WithEmptyAccessPolicy()
                    .WithSpeedwayTag(_context.Manifest.Id, _resource.Name, _context.Environment)
                    .WithSku(SkuName.Standard)
                    .CreateAsync();

                _logger.LogInformation(
                    "Created new vault {Id} to represent {ResourceGroup}/{Name}", resourceGroupTwin.Name,
                    _resource.Name,
                    randomName);
            }

            await AssignDeployPrincipalAsSecretsOfficer();

            return new SpeedwayResourceOutputMetadata(
                _keyVault.Name,
                new Dictionary<string, string>(),
                new HashSet<string>());
        }

        /// <summary>
        /// Deploy Api needs to be able to work with secrets.
        /// </summary>
        /// <returns></returns>
        private async Task AssignDeployPrincipalAsSecretsOfficer()
        {
            //When bootstrapping we don't have a DeployApiAzureManagedIdentityId.
            if (string.IsNullOrEmpty(_settings.Value.DeployApiAzureManagedIdentityId)) return;

            await AttachSecretsPolicyToDeployApi(_settings.Value.DeployApiAzureManagedIdentityId);
            
            //and for the deploy application so this works local
            var deployAppServicePrincipal = await _graphServiceClient
                .ServicePrincipals
                .Request()
                .Filter($"appId eq '{_settings.Value.DeployApiAzureApplicationId}'")
                .GetAsync();

            await AttachSecretsPolicyToDeployApi(deployAppServicePrincipal.Single().Id);
        }

        private async Task AttachSecretsPolicyToDeployApi(string servicePrincipalId)
        {
            var servicePrincipal = await _graphServiceClient
                .ServicePrincipals[servicePrincipalId]
                .Request()
                .GetAsync();

            await _keyVault!.Update().DefineAccessPolicy().ForObjectId(servicePrincipal.Id)
                .AllowSecretPermissions(new[]
                {
                    SecretPermissions.Get,
                    SecretPermissions.List,
                    SecretPermissions.Set
                })
                .Attach()
                .ApplyAsync();
        }

        /// <summary>
        /// Make sure all the know secrets are stored, as-well as any secrets that are required, but outside of Speedway's management.
        /// </summary>
        public async Task StoreKnownSecrets(Dictionary<string, string> secrets,
            HashSet<string> externallySetSecrets)
        {
            _allExistingSecrets = await GetAllSecrets();
            await StoreKnownSecrets(secrets, _allExistingSecrets);
            await CreateAndStoreSpeedwayGeneratedSecrets(externallySetSecrets, _allExistingSecrets);
            _allExistingSecrets = await GetAllSecrets();
        }

        /// <summary>
        /// Not returning secret with version anymore.
        /// https://azure.microsoft.com/en-us/updates/versions-no-longer-required-for-key-vault-references-in-app-service-and-azure-functions/
        /// </summary>
        /// <param name="secretName"></param>
        /// <returns></returns>
        public string GetSecretUri(string secretName)
        {
            var secret = _allExistingSecrets!.SingleOrDefault(x => x.Name == secretName);
            if (secret == null)
            {
                _logger.LogWarning(
                    "Reference to secret {SecretName} in Vault {ResourceGroup}/{VaultName} which doesn't exist",
                    secretName, _keyVault!.ResourceGroupName, _keyVault.Name);

                return $"{_keyVault!.VaultUri}secrets/{secretName}";
            }

            return secret.Id;
        }

        private async Task StoreKnownSecrets(Dictionary<string, string> secrets,
            IPagedCollection<ISecret> allExistingSecrets)
        {
            foreach (var knownSecret in secrets)
            {
                if (allExistingSecrets.All(x => x.Name != knownSecret.Key))
                {
                    await SetSecret(knownSecret.Key, knownSecret.Value);
                    _logger.LogInformation("Created secret {Key}", knownSecret.Key);
                }
                else
                {
                    var existingSecret = allExistingSecrets.Single(x => x.Name == knownSecret.Key);
                    await existingSecret.Update().WithValue(knownSecret.Value).ApplyAsync();
                    _logger.LogInformation("Updated existing known secret {Key}", knownSecret.Key);
                }
            }
        }

        private async Task CreateAndStoreSpeedwayGeneratedSecrets(HashSet<string> secretNames,
            IPagedCollection<ISecret> allExistingSecrets)
        {
            foreach (var externallySetSecret in secretNames)
            {
                if (allExistingSecrets.All(x => x.Name != externallySetSecret))
                {
                    await SetSecret(externallySetSecret,
                        $"{Guid.NewGuid().ToString().ToLowerInvariant()}-{Guid.NewGuid().ToString().ToLowerInvariant()}");
                }
                else
                {
                    _logger.LogInformation("Was asked to set existing externally set secret for {Secret}. Ignoring request as it already exists", externallySetSecret);
                }
            }
        }

        public async Task GrantAccessTo(ISpeedwayResource resource, SecretsLink access)
        {
            if (_keyVault == null) throw new InvalidOperationException("Please call Reflect() before granting access");

            var twin = await resource.GetPlatformTwin<IHaveAnAzureServicePrincipal>().ServicePrincipal!;
            var existingPolicy = _keyVault!.AccessPolicies.SingleOrDefault(x => x.ObjectId == twin!.Id);
            if (existingPolicy != null)
            {
                return;
            }

            await _keyVault.Update().DefineAccessPolicy().ForObjectId(twin.Id)
                .AllowSecretPermissions(access.Access == LinkAccess.Read
                    ? new[] {SecretPermissions.Get, SecretPermissions.List, SecretPermissions.Set}
                    : new[] {SecretPermissions.Get})
                .Attach()
                .ApplyAsync();
        }

        private async Task<IPagedCollection<ISecret>> GetAllSecrets()
        {
            var secrets = await _keyVault!.Secrets.ListAsync();
            _logger.LogInformation("Fetched secrets from KeyVault {VaultName}", _keyVault.Name);
            return secrets;
        }

        private Task SetSecret(string key, string value)
        {
            _logger.LogInformation("Setting secret in KeyVault {VaultName}. Secret Name:{Name}", _keyVault!.Name, key);
            return _keyVault!.Secrets.Define(key).WithValue(value).CreateAsync();
        }
    }
}