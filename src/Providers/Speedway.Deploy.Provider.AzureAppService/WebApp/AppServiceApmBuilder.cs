using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Rest.Azure;
using Newtonsoft.Json;
using Speedway.Core.Resources;
using Speedway.Deploy.Core.Resources.Speedway.WebApp;

namespace Speedway.Deploy.Provider.AzureAppService.WebApp
{
    internal class AppServiceApmBuilder<TMetadata> where TMetadata : SpeedwayWebAppResourceMetadata
    {
        private readonly IAzure _azure;
        private readonly ILogger<AppServiceApmBuilder<TMetadata>> _logger;
        private readonly IWellKnownPlatformComponentProvider _wellKnownPlatformComponentProvider;

        public AppServiceApmBuilder(
            IAzure azure, 
            ILogger<AppServiceApmBuilder<TMetadata>> logger, 
            IWellKnownPlatformComponentProvider wellKnownPlatformComponentProvider)
        {
            _azure = azure;
            _logger = logger;
            _wellKnownPlatformComponentProvider = wellKnownPlatformComponentProvider;
        }

        /// <summary>
        /// Wires up App Insights. Customise this to your APM tool of choice :)
        /// </summary>
        /// <returns></returns>
        internal async Task<string> Build(IWebApp webApp, SpeedwayWebLikeResource<TMetadata> resource)
        {
            //shortcut if it already exists - very slow to do this bit.
            var appInsightsName = $"{webApp!.Name}-insights";
            try
            {
                var platformResource = await _azure.GenericResources.GetAsync(
                    webApp!.ResourceGroupName,
                    "microsoft.insights",
                    null, "components", appInsightsName);

                _logger.LogInformation("Found existing app-insights for app: {App}", resource.Name);

                //how to get the api key?
                return (string) ((dynamic) platformResource.Properties).ConnectionString;
            }
            catch (CloudException ce) when (ce.Response.StatusCode == HttpStatusCode.NotFound)
            {
            }

            using var reader = new StreamReader(
                typeof(AppServiceSpeedwayPlatformWebAppTwin<>).Assembly.GetManifestResourceStream(
                    $"{typeof(AppServiceSpeedwayPlatformWebAppTwin<>).Namespace}.ArmTemplates.app-insights.json")
                ?? throw new InvalidOperationException("Failed to find resource template"));

            var template = await reader.ReadToEndAsync();

            _logger.LogInformation("Creating app-insights for app: {App}", resource.Name);
            var deploymentName = $"appinsights-{DateTime.Now.ToOADate()}";
            await _azure.Deployments.Define(deploymentName)
                .WithExistingResourceGroup(webApp!.ResourceGroupName)
                .WithTemplate(template)
                .WithParameters(JsonConvert.SerializeObject(new
                {
                    name = new {value = appInsightsName},
                    type = new {value = "web"},
                    regionId = new {value = webApp.RegionName},
                    tagsArray = new {value = webApp.Tags},
                    requestSource = new {value = "rest"},
                    workspaceResourceId = new
                        {value = await _wellKnownPlatformComponentProvider.GetLogAnalyticsWorkspaceResourceId()}
                }))
                .WithMode(DeploymentMode.Incremental)
                .CreateAsync();

            _logger.LogDebug("Submitted app-insights for app: {App}. Waiting for completion", resource.Name);

            //now make sure the instrumentation key is set as a parameter:
            var deployment =
                await _azure.Deployments.GetByResourceGroupAsync(webApp.ResourceGroupName,
                    deploymentName); //https://github.com/Azure/azure-libraries-for-net/issues/822

            var instrumentationKey = ((dynamic) deployment.Outputs).appInsightsInstrumentationKey.value;
            _logger.LogInformation("Created app-insights for app: {App}", resource.Name);
            return $"InstrumentationKey={instrumentationKey}";
        }

    }
}