using System;
using Microsoft.Extensions.DependencyInjection;
using Speedway.Core.Resources;
using Speedway.Deploy.Core.Resources.Speedway;
using Speedway.Deploy.Core.Resources.Speedway.Context;
using Speedway.Deploy.Core.Resources.Speedway.ExistingOAuthClient;
using Speedway.Deploy.Core.Resources.Speedway.NoSql;
using Speedway.Deploy.Core.Resources.Speedway.OAuthClient;
using Speedway.Deploy.Core.Resources.Speedway.SecretStore;
using Speedway.Deploy.Core.Resources.Speedway.Storage;
using Speedway.Deploy.Core.Resources.Speedway.WebApi;
using Speedway.Deploy.Core.Resources.Speedway.WebApp;
using Speedway.Deploy.Provider.AzureAppService.Context;
using Speedway.Deploy.Provider.AzureAppService.ExistingOAuthClient;
using Speedway.Deploy.Provider.AzureAppService.NoSql;
using Speedway.Deploy.Provider.AzureAppService.OAuthClient;
using Speedway.Deploy.Provider.AzureAppService.SecretStore;
using Speedway.Deploy.Provider.AzureAppService.Storage;
using Speedway.Deploy.Provider.AzureAppService.WebApi;
using Speedway.Deploy.Provider.AzureAppService.WebApp;

namespace Speedway.Deploy.Provider.AzureAppService
{
    /// <summary>
    /// Represents a platform based on Azure KeyVault, Storage, and App Service
    /// </summary>
    // ReSharper disable once UnusedType.Global
    internal class AzureTwinFactory : ISpeedwayPlatformTwinFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public AzureTwinFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public ISpeedwayPlatformTwin Build<T>(T speedwayResource) where T : ISpeedwayResource
        {
            if (speedwayResource is SpeedwayWebAppResource resource)
                return BuildTwin<AppServiceSpeedwayPlatformWebAppTwin<SpeedwayWebAppResourceMetadata>, SpeedwayWebAppResource>(resource);

            if (speedwayResource is SpeedwayWebApiResource apiResource)
                return BuildTwin<AppServiceSpeedwayPlatformWebApiTwin, SpeedwayWebApiResource>(apiResource);

            if (speedwayResource is SpeedwayStorageResource storageResource)
                return BuildTwin<AzureStorageServiceSpeedwayPlatformStorageTwin, SpeedwayStorageResource>(storageResource);

            if (speedwayResource is SpeedwaySecretContainerResource secretResource)
                return BuildTwin<KeyVaultSpeedwayPlatformStorageTwin, SpeedwaySecretContainerResource>(secretResource);

            if (speedwayResource is SpeedwayContextResource context)
                return BuildTwin<ResourceGroupSpeedwayPlatformContextTwin, SpeedwayContextResource>(context);

            if (speedwayResource is SpeedwayOAuthClientResource oauthContext)
                return BuildTwin<AzureActiveDirectoryApplicationPlatformTwin, SpeedwayOAuthClientResource>(oauthContext);

            if (speedwayResource is SpeedwayExistingOAuthClientResource existingOauthResource)
                return BuildTwin<ExistingAzureActiveDirectoryApplicationServicePrincipalPlatformTwin, SpeedwayExistingOAuthClientResource>(existingOauthResource);

            if (speedwayResource is SpeedwayNoSqlResource noSqlResource)
                return BuildTwin<AzureCosmosSpeedwayPlatformNoSqlTwin, SpeedwayNoSqlResource>(noSqlResource);

            throw new ArgumentException($"No twin registered for {speedwayResource.GetType().Name}");
        }

        private ISpeedwayPlatformTwin BuildTwin<TTwin, TSpeedwayResource>(
            TSpeedwayResource speedwayResource)
            where TTwin : ISpeedwayPlatformTwin
            where TSpeedwayResource : ISpeedwayResource
        {
            return ActivatorUtilities.CreateInstance<TTwin>(_serviceProvider, 
                speedwayResource);
        }
    }
}