using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Management.CosmosDB.Fluent;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Speedway.Core.Resources;
using Speedway.Deploy.Core;
using Speedway.Deploy.Core.Resources.Speedway;
using Speedway.Deploy.Core.Resources.Speedway.Context;
using Speedway.Deploy.Core.Resources.Speedway.NoSql;
using Speedway.Deploy.Core.Resources.Speedway.Storage;
using Speedway.Deploy.Provider.AzureAppService.Context;

namespace Speedway.Deploy.Provider.AzureAppService.NoSql
{
    // ReSharper disable once ClassNeverInstantiated.Global
    internal class AzureCosmosSpeedwayPlatformNoSqlTwin : ISpeedwayPlatformTwin, IStoragePlatformTwin
    {
        private readonly ILogger<AzureCosmosSpeedwayPlatformNoSqlTwin> _logger;
        private readonly IAzure _azure;
        private readonly SpeedwayContextResource _context;
        private readonly SpeedwayNoSqlResource _resource;
        private ICosmosDBAccount? _account;

        public AzureCosmosSpeedwayPlatformNoSqlTwin(
            ILogger<AzureCosmosSpeedwayPlatformNoSqlTwin> logger,
            IAzure azure,
            SpeedwayNoSqlResource resource)
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

            _logger.LogInformation("Reflecting nosql account {ResourceGroup}/{Name}",
                resourceGroupName,
                _resource.Name);

            var existingAccounts = await _azure.CosmosDBAccounts.ListByResourceGroupAsync(resourceGroupName);
            existingAccounts.SpeedwayResourceExists(_resource, _context.Environment, out _account);

            var resourceName = _account == null
                ? AzureEx.SuggestResourceName(_resource.Context, _resource.Name, 44, true).ToLowerInvariant()
                : _account.Name;

            var parameters = new
            {
                accountName = new { value = resourceName},
                primaryRegion = new { value = _context.Region},
                secondaryRegion = new { value = GetSecondaryRegion(_context.Region)},
                databaseName = new {value = $"{_resource.Name}"},
                containers = new {value = _resource.ResourceMetadata.Containers ?? Array.Empty<string>()},
                tags = new
                {
                    value = AzureEx.BuildSpeedwayTags(_context.Manifest.Id, _resource.Name, _context.Environment)
                }
            };

            var template = GetType()
                .Assembly
                .GetManifestResourceStream($"{this.GetType().Namespace}.NoSql.json")!;

            using var reader = new StreamReader(template);

            await _azure.Deployments.Define($"sw-nosql-{DateTime.Now.ToOADate()}")
                .WithExistingResourceGroup(resourceGroupName)
                .WithTemplate(JsonConvert.DeserializeObject(await reader.ReadToEndAsync()))
                .WithParameters(JsonConvert.SerializeObject(parameters))
                .WithMode(DeploymentMode.Incremental)
                .CreateAsync();

            _account = await _azure.CosmosDBAccounts.GetByResourceGroupAsync(resourceGroupName, resourceName);

            _logger.LogInformation(
                "Cosmos account {Id} represents {ResourceGroup}/{Name}",
                _account.Id,
                _resource.Name,
                resourceName);

            var secrets = new Dictionary<string, string>
                {{"connection-string", (await _account!.ListConnectionStringsAsync()).ConnectionStrings.First().ConnectionString} };

            return new SpeedwayResourceOutputMetadata(
                resourceName,
                secrets,
                new HashSet<string>());
        }

        private string GetSecondaryRegion(string region)
        {
            switch (region)
            {
                case "australiaeast" : return "australiasoutheast";
                default: throw new NotSupportedException($"Need to define a secondary region for {region}");
            }
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