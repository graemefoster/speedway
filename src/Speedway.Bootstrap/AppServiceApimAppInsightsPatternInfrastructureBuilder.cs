using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.ResourceManager.Fluent.Models;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Speedway.Deploy.Provider.AzureAppService;
using OperatingSystem = Microsoft.Azure.Management.AppService.Fluent.OperatingSystem;
using SpeedwayManifest = Speedway.Core.SpeedwayManifest;

namespace Speedway.Bootstrap
{
    /// <summary>
    /// Provides a new platform with:
    ///  - a single ASP to host all the apps in
    ///  - Log Analytics for app-insights to report into
    ///  - API Management for exposing APIs
    /// </summary>
    internal class AppServiceApimAppInsightsPatternInfrastructureBuilder : IWellKnownPlatformComponentProvider
    {
        private readonly IAzure _azure;
        private readonly SpeedwayManifest _manifest;
        private readonly IOptions<SpeedwayBootstrapSettings> _settings;
        private IAppServicePlan? _cachedAsp;
        private string? _cachedAnalyticsWorkspaceId;

        public AppServiceApimAppInsightsPatternInfrastructureBuilder(
            IAzure azure,
            SpeedwayManifest manifest,
            IOptions<SpeedwayBootstrapSettings> settings)
        {
            _azure = azure;
            _manifest = manifest;
            _settings = settings;
        }

        public async Task<IAppServicePlan> GetDefaultAppServicePlan()
        {
            if (_cachedAsp != null) return _cachedAsp;

            var rg = _azure.ResourceGroups.ListByTag("speedway-manifest-id", _manifest.Id.ToString()).Single();
            var aspName = $"{rg.Name}-asp";

            var existing = _azure.AppServices.AppServicePlans.ListByResourceGroup(rg.Name).ToArray();
            if (!existing.Any())
            {
                _cachedAsp = await _azure.AppServices.AppServicePlans.Define(aspName)
                    .WithRegion(rg.Region)
                    .WithExistingResourceGroup(rg)
                    .WithPricingTier(PricingTier.BasicB1)
                    .WithOperatingSystem(OperatingSystem.Linux)
                    .CreateAsync();
            }
            else
            {
                _cachedAsp = existing.First();
            }

            await CreateLogAnalyticsIfDoesNotExist(rg.Name, rg.Region);
            await CreateApimIfDoesNotExist(rg.Name);

            return _cachedAsp;
        }

        public Task<IAzureApiManagement> GetApiManagement()
        {
            throw new NotSupportedException();
        }

        public Task<string> GetLogAnalyticsWorkspaceResourceId()
        {
            return Task.FromResult(_cachedAnalyticsWorkspaceId!);
        }

        private async Task CreateLogAnalyticsIfDoesNotExist(string rgName, Region region)
        {
            var resourceName = $"{rgName}-analytics";

            var workspace = await GetResourceId(rgName, resourceName);
            if (workspace != null)
            {
                _cachedAnalyticsWorkspaceId = workspace.Id;
                return;
            }

            using var reader = new StreamReader(Assembly.GetExecutingAssembly()
                                                    .GetManifestResourceStream(
                                                        "Speedway.Bootstrap.Resources.log-analytics.json") ??
                                                throw new InvalidOperationException(
                                                    "Failed to find resource template"));
            var template = await reader.ReadToEndAsync();

            var deployment = await _azure.Deployments
                .Define($"speedway-loganalytics-{DateTime.Now.ToOADate()}")
                .WithExistingResourceGroup(rgName)
                .WithTemplate(template)
                .WithParameters(JsonConvert.SerializeObject(new
                {
                    workspaceName = new {value = resourceName},
                    sku = new {value = "Free"},
                    location = new {value = region.Name},
                    resourcePermissions = new {value = true}
                })) //system.json doesn't like anonymous objects?
                .WithMode(DeploymentMode.Incremental)
                .CreateAsync();

            _cachedAnalyticsWorkspaceId = (await GetResourceId(rgName, resourceName))!.Id;
        }

        private async Task CreateApimIfDoesNotExist(string rgName)
        {
            var resourceName = $"{rgName}-apim";

            var resource = await GetResourceId(rgName, resourceName);
            if (resource != null)
            {
                return;
            }

            using var reader = new StreamReader(Assembly.GetExecutingAssembly()
                                                    .GetManifestResourceStream(
                                                        "Speedway.Bootstrap.Resources.apim.json") ??
                                                throw new InvalidOperationException(
                                                    "Failed to find APIM resource template"));
            var template = await reader.ReadToEndAsync();

            var deployment = await _azure.Deployments
                .Define($"speedway-apim-{DateTime.Now.ToOADate()}")
                .WithExistingResourceGroup(rgName)
                .WithTemplate(template)
                .WithParameters(JsonConvert.SerializeObject(new
                {
                    publisherEmail = new {value = _settings.Value.ApimPublisherEmail},
                    publisherName = new {value = "Speedway"},
                    sku = new {value = "Developer"},
                    skuCount = new {value = 1},
                    apiManagementServiceName = new {value = resourceName}
                })) 
                .WithMode(DeploymentMode.Incremental)
                .CreateAsync();

            await GetResourceId(rgName, resourceName);
        }

        private async Task<IGenericResource?> GetResourceId(string rgName, string resourceName)
        {
            var existing = await _azure.GenericResources.ListByResourceGroupAsync(rgName, loadAllPages: true);
            if (existing.Any(x => x.Name == resourceName))
            {
                return existing.Single(x => x.Name == resourceName);
            }

            return null;
        }
    }
}