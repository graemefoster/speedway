using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Models;
using Microsoft.Azure.Management.Storage.Fluent;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Speedway.Core.Resources;
using Speedway.Deploy.Core;
using Speedway.Deploy.Core.Resources.Speedway;
using Speedway.Deploy.Core.Resources.Speedway.Context;
using Speedway.Deploy.Core.Resources.Speedway.Storage;
using Speedway.Deploy.Provider.AzureAppService.Context;

namespace Speedway.Deploy.Provider.AzureAppService.Storage
{
    // ReSharper disable once ClassNeverInstantiated.Global
    internal class AzureStorageServiceSpeedwayPlatformStorageTwin : ISpeedwayPlatformTwin, IStoragePlatformTwin
    {
        private readonly ILogger<AzureStorageServiceSpeedwayPlatformStorageTwin> _logger;
        private readonly IAzure _azure;
        private readonly SpeedwayContextResource _context;
        private readonly SpeedwayStorageResource _resource;
        private IStorageAccount? _account;

        public AzureStorageServiceSpeedwayPlatformStorageTwin(
            ILogger<AzureStorageServiceSpeedwayPlatformStorageTwin> logger,
            IAzure azure,
            SpeedwayStorageResource resource)
        {
            _logger = logger;
            _azure = azure;
            _context = resource.Context;
            _resource = resource;
        }

        public async Task<SpeedwayResourceOutputMetadata> Reflect()
        {
            var resourceGroupTwin = _context.GetPlatformTwin<ResourceGroupSpeedwayPlatformContextTwin>();
            var resourceGroupName = resourceGroupTwin.Name;

            _logger.LogInformation("Reflecting storage account {ResourceGroup}/{Name}",
                resourceGroupName,
                _resource.Name);

            var existingAccounts = await _azure.StorageAccounts.ListByResourceGroupAsync(resourceGroupName);
            existingAccounts.SpeedwayResourceExists(_resource, _context.Environment, out _account);

            var resourceName = _account == null
                ? AzureEx.SuggestResourceName(_resource.Context, _resource.Name, 24, false)
                : _account.Name;

            var parameters = new
            {
                storageAccountName = new {value = resourceName},
                containers = new
                {
                    value = _resource.ResourceMetadata.Containers
                        .Where(x => x.Type == SpeedwayStorageResourceContainerType.Storage)
                        .Select(x => x.Name)
                        .ToArray()
                },
                tables = new
                {
                    value = _resource.ResourceMetadata.Containers
                        .Where(x => x.Type == SpeedwayStorageResourceContainerType.Table)
                        .Select(x => x.Name)
                        .ToArray()
                },
                queues = new
                {
                    value = _resource.ResourceMetadata.Containers
                        .Where(x => x.Type == SpeedwayStorageResourceContainerType.Queue)
                        .Select(x => x.Name)
                        .ToArray()
                },
                tags = new
                {
                    value = AzureEx.BuildSpeedwayTags(_context.Manifest.Id, _resource.Name, _context.Environment)
                }
            };

            var template = GetType()
                .Assembly
                .GetManifestResourceStream($"{this.GetType().Namespace}.StorageAccount.json")!;

            using var reader = new StreamReader(template);

            var deployment = await _azure.Deployments.Define($"sw-storage-{DateTime.Now.ToOADate()}")
                .WithExistingResourceGroup(resourceGroupName)
                .WithTemplate(JsonConvert.DeserializeObject(await reader.ReadToEndAsync()))
                .WithParameters(JsonConvert.SerializeObject(parameters))
                .WithMode(DeploymentMode.Incremental)
                .CreateAsync();

            _account = await _azure.StorageAccounts.GetByResourceGroupAsync(resourceGroupName, resourceName);

            _logger.LogInformation(
                "Storage group {Id} represents {ResourceGroup}/{Name}",
                _account.Id,
                _resource.Name,
                resourceName);

            var secrets = new Dictionary<string, string>
                {{"access-key", (await _account!.GetKeysAsync())[0].Value}};

            return new SpeedwayResourceOutputMetadata(
                resourceName,
                secrets,
                new HashSet<string>());
        }


        public async Task GrantAccessTo(ISpeedwayResource resource, StorageLink access)
        {
            if (_account == null) throw new InvalidOperationException("Please call Reflect() before linking");
            var rolesToApply = access.Access == LinkAccess.Read
                ? new[]
                {
                    AzureResourceExtensions.StorageBlobDataReaderRoleId,
                    AzureResourceExtensions.StorageQueueDataReaderRoleId
                }
                : new[]
                {
                    AzureResourceExtensions.StorageBlobDataContributorRoleId,
                    AzureResourceExtensions.StorageQueueDataContributorRoleId
                };

            await _azure.ReflectRolesOnResource(
                _account,
                await resource.GetPlatformTwin<IHaveAnAzureServicePrincipal>().ServicePrincipal,
                _logger,
                rolesToApply
            );
        }
    }
}