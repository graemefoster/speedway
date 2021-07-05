using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Speedway.Deploy.Core;

namespace Speedway.Deploy.Provider.AzureAppService
{
    /// <summary>
    /// Provides a new platform with:
    ///  - a single ASP to host all the apps in
    ///  - Log Analytics for app-insights to report into
    ///  - API Management for exposing APIs
    /// </summary>
    internal class AppServiceApimAppInsightsPattern : IWellKnownPlatformComponentProvider
    {
        private readonly IAzure _azure;
        private Task<IAppServicePlan>? _cached;
        private string? _logAnalyticsId;
        private readonly IOptions<AzureSpeedwaySettings> _settings;
        private readonly ILoggerFactory _loggerFactory;
        private IGenericResource? _apim;

        public AppServiceApimAppInsightsPattern(IAzure azure, IOptions<AzureSpeedwaySettings> settings,
            ILoggerFactory loggerFactory)
        {
            _azure = azure;
            _settings = settings;
            _loggerFactory = loggerFactory;
        }

        public Task<IAppServicePlan> GetDefaultAppServicePlan()
        {
            return _cached ??= _azure
                .AppServices
                .AppServicePlans
                .GetByResourceGroupAsync(
                    _settings.Value.DefaultAppServicePlanResourceGroup,
                    _settings.Value.DefaultAppServicePlanName);
        }

        public async Task<IAzureApiManagement> GetApiManagement()
        {
            var appServicePlan = await GetDefaultAppServicePlan();
            if (_apim == null)
            {
                var resourceName = $"{appServicePlan.ResourceGroupName}-apim";
                
                var existing = await _azure.GenericResources.GetAsync(
                    appServicePlan.ResourceGroupName,
                    "Microsoft.ApiManagement",
                    null,
                    "service",
                    resourceName);
                
                _apim = existing;
            }
            return new AzureApiManagement(_azure, _apim, _loggerFactory.CreateLogger<AzureApiManagement>());
        }

        public async Task<string> GetLogAnalyticsWorkspaceResourceId()
        {
            var asp = await GetDefaultAppServicePlan();
            return await GetLogAnalyticsWorkspaceId(asp.ResourceGroupName, $"{asp.ResourceGroupName}-analytics");
        }

        private async Task<string> GetLogAnalyticsWorkspaceId(string rgName, string resourceName)
        {
            if (_logAnalyticsId != null) return _logAnalyticsId;
            var existing = await _azure.GenericResources.ListByResourceGroupAsync(rgName, loadAllPages: true);
            _logAnalyticsId = existing.Single(x => x.Name == resourceName).Id!;
            return _logAnalyticsId!;
        }
    }
}