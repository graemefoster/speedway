using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.AppService.Fluent.Models;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Speedway.Core.Resources;
using Speedway.Deploy.Core;
using Speedway.Deploy.Core.Resources.Speedway;
using Speedway.Deploy.Core.Resources.Speedway.OAuthClient;
using Speedway.Deploy.Core.Resources.Speedway.WebApi;
using Speedway.Deploy.Core.Resources.Speedway.WebApp;
using Speedway.Deploy.Provider.AzureAppService.WebApp;

namespace Speedway.Deploy.Provider.AzureAppService.WebApi
{
    /// <summary>
    /// Azure App Service representation of a SpeedwayWebAppResource that uses AppInsights for APM
    /// </summary>
    // ReSharper disable once ClassNeverInstantiated.Global
    internal class AppServiceSpeedwayPlatformWebApiTwin :
        AppServiceSpeedwayPlatformWebAppTwin<SpeedwayWebApiResourceMetadata>, ISpeedwayWebApiPlatformTwin
    {
        private readonly ILogger<AppServiceSpeedwayPlatformWebApiTwin> _logger;
        private readonly IAzure _azure;
        private readonly IGraphServiceClient _graphClient;
        private readonly IWellKnownPlatformComponentProvider _wellKnownPlatformComponentProvider;
        private readonly SpeedwayWebApiResource _resource;
        private IHaveAnAzureServicePrincipal? _aadApplication;
        private SpeedwayResourceOutputMetadata? _response;

        public AppServiceSpeedwayPlatformWebApiTwin(
            ILogger<AppServiceSpeedwayPlatformWebApiTwin> logger,
            IAzure azure,
            IGraphServiceClient graphClient,
            IWellKnownPlatformComponentProvider wellKnownPlatformComponentProvider,
            SpeedwayWebApiResource resource,
            AppServiceWebAppBuilder<SpeedwayWebApiResourceMetadata> appServiceWebAppBuilder,
            AppServiceApmBuilder<SpeedwayWebApiResourceMetadata> appServiceApmBuilder,
            AppServiceWebAppArtifactDeployer<SpeedwayWebApiResourceMetadata> appServiceWebAppArtifactDeployer) 
            : base(
                logger, 
                graphClient,
                wellKnownPlatformComponentProvider, 
                resource, 
                appServiceWebAppBuilder, 
                appServiceApmBuilder,
                appServiceWebAppArtifactDeployer)
        {
            _logger = logger;
            _azure = azure;
            _graphClient = graphClient;
            _wellKnownPlatformComponentProvider = wellKnownPlatformComponentProvider;
            _resource = resource;
        }

        public override async Task<SpeedwayResourceOutputMetadata> Reflect()
        {
            _response = await base.Reflect();
            await RegisterApiInApiManagement();
            var apim = await _wellKnownPlatformComponentProvider.GetApiManagement();
            var gatewayUri = (string)((dynamic)apim.ApimResource.Properties).gatewayUrl;
            return (_response as SpeedwayWebAppResourceOutputMetadata)! with {Uri = $"{gatewayUri}/{_resource.Name}/" };
        }

        private async Task RegisterApiInApiManagement()
        {
            var apiM = await _wellKnownPlatformComponentProvider.GetApiManagement();
            await apiM.CreateApi(WebApp!);
            await WhiteListApiManagementInWebApp(apiM);
        }

        private async Task WhiteListApiManagementInWebApp(IAzureApiManagement apiM)
        {
            var apimIpAddress = (string) ((dynamic) apiM.ApimResource.Properties).publicIPAddresses[0];
            using var websiteManagementClient = new WebSiteManagementClient(_azure.AppServices.RestClient)
            {
                SubscriptionId = _azure.SubscriptionId
            };
            var siteConfig = await websiteManagementClient.WebApps.GetConfigurationAsync(WebApp!.ResourceGroupName, WebApp!.Name);
            siteConfig.IpSecurityRestrictions ??= new List<IpSecurityRestriction>();
            var cidrIp = $"{apimIpAddress}/32";

            RemoveAllowAnyFromIpRestrictions(siteConfig);
            AddAllowApimToIpRestrictions(siteConfig, cidrIp);
            AddDenyAllToIpRestrictions(siteConfig);

            await websiteManagementClient.WebApps.UpdateConfigurationAsync(
                WebApp!.ResourceGroupName,
                WebApp!.Name,
                siteConfig);

            WebApp = await WebApp!.RefreshAsync();
            _logger.LogInformation("Restricted IP Addresses on App {Name} to APIM", WebApp!.Name);
        }

        private void AddDenyAllToIpRestrictions(SiteConfigResourceInner siteConfig)
        {
            if (siteConfig.IpSecurityRestrictions.All(x => x.IpAddress != "Any"))
            {
                siteConfig.IpSecurityRestrictions.Add(new IpSecurityRestriction("Any", action: "Deny")
                {
                    Priority = 100
                });
            }
        }

        private void AddAllowApimToIpRestrictions(SiteConfigResourceInner siteConfig, string cidrIp)
        {
            if (siteConfig.IpSecurityRestrictions.All(x => x.IpAddress != cidrIp))
            {
                siteConfig.IpSecurityRestrictions.Add(new IpSecurityRestriction(cidrIp, action: "Allow")
                {
                    Name = "Allow Speedway APIm Gateway",
                    Priority = 10
                });
            }
        }

        private static void RemoveAllowAnyFromIpRestrictions(SiteConfigResourceInner siteConfig)
        {
            var allowAll = siteConfig.IpSecurityRestrictions.Where(x => x.IpAddress == "Any" && x.Action == "Allow").ToArray();
            if (allowAll.Any())
            {
                foreach (var allowAllRestriction in allowAll)
                {
                    siteConfig.IpSecurityRestrictions.Remove(allowAllRestriction);
                }
            }
        }


        public override async Task Deploy(ZipArchive binaries)
        {
            await base.Deploy(binaries);
            await RegisterApi();
        }

        /// <summary>
        /// If this is an api and has swagger configured then bring it in to apim.
        /// </summary>
        /// <returns></returns>
        private async Task RegisterApi()
        {
            var apiManagement = await _wellKnownPlatformComponentProvider.GetApiManagement();
            var azureAdApplication = (await _graphClient.Applications.Request().Filter($"appId eq '{(await _aadApplication!.ServicePrincipal).AppId}'").GetAsync()).Single();
            await apiManagement.ImportApi(WebApp!, _resource.ResourceMetadata, azureAdApplication);
        }

        /// <summary>
        /// When an api is mapped against an oauth client we get given it. We need this for sorting out API Management.
        /// </summary>
        /// <param name="speedwayOAuthClientResource"></param>
        public void SetOAuthClient(SpeedwayOAuthClientResource speedwayOAuthClientResource)
        {
            _aadApplication = speedwayOAuthClientResource.GetPlatformTwin<IHaveAnAzureServicePrincipal>();
        }
    }
}