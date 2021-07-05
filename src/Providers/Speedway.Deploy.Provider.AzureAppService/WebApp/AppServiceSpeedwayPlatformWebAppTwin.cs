using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Speedway.Core.Resources;
using Speedway.Deploy.Core.Resources.Speedway;
using Speedway.Deploy.Core.Resources.Speedway.WebApp;
using Speedway.Deploy.Provider.AzureAppService.Context;
using OperatingSystem = Microsoft.Azure.Management.AppService.Fluent.OperatingSystem;

namespace Speedway.Deploy.Provider.AzureAppService.WebApp
{
    /// <summary>
    /// Azure App Service representation of a SpeedwayWebAppResource that uses AppInsights for APM
    /// </summary>
    // ReSharper disable once ClassNeverInstantiated.Global
    internal class AppServiceSpeedwayPlatformWebAppTwin<TMetadata> :
        ISpeedwayWebAppPlatformTwin,
        IHaveAnAzureServicePrincipal
        where TMetadata : SpeedwayWebAppResourceMetadata
    {
        private readonly ILogger<AppServiceSpeedwayPlatformWebAppTwin<TMetadata>> _logger;
        private readonly IGraphServiceClient _graphClient;
        private readonly IWellKnownPlatformComponentProvider _wellKnownPlatformComponentProvider;
        private readonly SpeedwayWebLikeResource<TMetadata> _resource;
        private readonly AppServiceWebAppBuilder<TMetadata> _appServiceWebAppBuilder;
        private readonly AppServiceApmBuilder<TMetadata> _appServiceApmBuilder;
        private readonly AppServiceWebAppArtifactDeployer<TMetadata> _appServiceWebAppArtifactDeployer;

        protected IWebApp? WebApp;
        private string? _apmKey;

        public AppServiceSpeedwayPlatformWebAppTwin(
            ILogger<AppServiceSpeedwayPlatformWebAppTwin<TMetadata>> logger,
            IGraphServiceClient graphClient,
            IWellKnownPlatformComponentProvider wellKnownPlatformComponentProvider,
            SpeedwayWebLikeResource<TMetadata> resource,
            AppServiceWebAppBuilder<TMetadata> appServiceWebAppBuilder,
            AppServiceApmBuilder<TMetadata> appServiceApmBuilder,
            AppServiceWebAppArtifactDeployer<TMetadata> appServiceWebAppArtifactDeployer)
        {
            _logger = logger;
            _graphClient = graphClient;
            _wellKnownPlatformComponentProvider = wellKnownPlatformComponentProvider;
            _resource = resource;
            _appServiceWebAppBuilder = appServiceWebAppBuilder;
            _appServiceApmBuilder = appServiceApmBuilder;
            _appServiceWebAppArtifactDeployer = appServiceWebAppArtifactDeployer;
        }

        public virtual async Task<SpeedwayResourceOutputMetadata> Reflect()
        {
            var context = _resource.Context;

            var resourceGroupTwin = context.GetPlatformTwin<ResourceGroupSpeedwayPlatformContextTwin>();
            var resourceGroupName = resourceGroupTwin.Name;

            _logger.LogDebug("Ensuring app {ResourceGroup}/{Name} exists in environment {Env}", resourceGroupName,
                _resource.Name, context.Environment);

            WebApp = await _appServiceWebAppBuilder.Build(resourceGroupName, _resource);
            _apmKey = await _appServiceApmBuilder.Build(WebApp, _resource);
            AddRequiredAdditionalConfiguration();

            var knownSecrets = await BuildKnownSecretsCollection();

            return new SpeedwayWebAppResourceOutputMetadata(
                WebApp!.Name,
                GetPrimaryRootUri(),
                WebApp.SystemAssignedManagedServiceIdentityPrincipalId,
                await GetExistingConfiguration(),
                knownSecrets,
                _resource.ResourceMetadata.RequiredSecretNames ?? new HashSet<string>());
        }

        private async Task<Dictionary<string, string>> GetExistingConfiguration()
        {
            var existingConfiguration = (await WebApp!.GetAppSettingsAsync())
                .ToDictionary(x => x.Value.Key, x => x.Value.Value);
            return existingConfiguration;
        }

        private async Task<Dictionary<string, string>> BuildKnownSecretsCollection()
        {
            var knownSecrets = new Dictionary<string, string>();
            await AddApiManagementKeyToSecrets(knownSecrets);
            return knownSecrets;
        }

        private async Task AddApiManagementKeyToSecrets(Dictionary<string, string> knownSecrets)
        {
            if (_resource.ResourceMetadata.RequiresApiManagementKey)
            {
                var apiManagement = await _wellKnownPlatformComponentProvider.GetApiManagement();
                knownSecrets.Add("ApiManagementKey", await apiManagement.GetKey());
            }
        }

        private string GetPrimaryRootUri()
        {
            return $"https://{WebApp!.HostNames.First()}/";
        }

        /// <summary>
        /// Add to metadata implicit configuration settings which the web-app needs.
        /// </summary>
        /// <returns></returns>
        private void AddRequiredAdditionalConfiguration()
        {
            _resource.Configuration["ASPNETCORE_ENVIRONMENT"] = _resource.Context.Environment;
            _resource.Configuration["SPEEDWAY_NAME"] = _resource.Name;
            AddAppInsightsKey();
            CheckForNonStandardPortForContainers();
        }

        private void AddAppInsightsKey()
        {
            if (_apmKey != null)
            {
                _logger.LogDebug("Ensuring APPLICATIONINSIGHTS_CONNECTION_STRING on webapp {Webapp} configuration",
                    _resource.Name);
                _resource.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"] = _apmKey;
            }
        }

        private void CheckForNonStandardPortForContainers()
        {
            if (_resource.ResourceMetadata.WebAppDeploymentType == WebAppDeploymentType.Container)
            {
                if (_resource.ResourceMetadata.Container?.Port != null)
                {
                    _resource.Configuration["WEBSITES_PORT"] =
                        _resource.ResourceMetadata.Container?.Port?.ToString() ?? string.Empty;
                }
            }
        }

        public virtual async Task Deploy(ZipArchive zipArchive)
        {
            if (_resource.ResourceMetadata.ActualWebAppDeploymentType == WebAppDeploymentType.Binaries)
            {
                await _appServiceWebAppArtifactDeployer.DeployBinariesToWebApp(WebApp!, zipArchive, _resource);
            }
        }

        public async Task ReflectAppSettings(Dictionary<string, string> newAndUpdatedSettings)
        {
            var duplicateKeys = newAndUpdatedSettings.Keys.Select(x => x.ToLowerInvariant()).GroupBy(x => x)
                .Where(x => x.Count() > 1).ToArray();
            if (duplicateKeys.Any())
            {
                var duplicateKeyEntries = string.Join(", ", duplicateKeys.Select(x => x.Key));
                _logger.LogWarning("Found duplicate configuration setting keys for App {App}: ", duplicateKeyEntries);
                throw new ArgumentException(
                    $"Found the following duplicate app-setting keys which would cause a conflict: {duplicateKeyEntries}");
            }

            _logger.LogInformation("Setting webapp {App} app settings", WebApp!.Name);
            if (WebApp!.OperatingSystem == OperatingSystem.Linux)
            {
                //https://stackoverflow.com/questions/51480085/configuring-appsettings-with-asp-net-core-on-azure-web-app-for-containers-whith
                newAndUpdatedSettings = newAndUpdatedSettings
                    .ToDictionary(x => x.Key.Replace(":", "__"), x => x.Value);
            }

            await WebApp!.Update().WithAppSettings(newAndUpdatedSettings).ApplyAsync();
        }
        
        public Task<ServicePrincipal> ServicePrincipal => _graphClient
            .ServicePrincipals[WebApp!.SystemAssignedManagedServiceIdentityPrincipalId].Request().GetAsync();

    }
}