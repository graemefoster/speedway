using System.Threading.Tasks;
using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Extensions.Logging;
using Speedway.Core.Resources;
using Speedway.Deploy.Core;
using Speedway.Deploy.Core.Resources.Speedway.WebApp;

namespace Speedway.Deploy.Provider.AzureAppService.WebApp
{
    internal class AppServiceWebAppBuilder<TMetadata> where TMetadata : SpeedwayWebAppResourceMetadata
    {
        private readonly IAzure _azure;
        private readonly ILogger<AppServiceWebAppBuilder<TMetadata>> _logger;
        private readonly IWellKnownPlatformComponentProvider _wellKnownPlatformComponentProvider;

        public AppServiceWebAppBuilder(
            IAzure azure,
            ILogger<AppServiceWebAppBuilder<TMetadata>> logger,
            IWellKnownPlatformComponentProvider wellKnownPlatformComponentProvider)
        {
            _azure = azure;
            _logger = logger;
            _wellKnownPlatformComponentProvider = wellKnownPlatformComponentProvider;
        }
        
        public async Task<IWebApp> Build(string resourceGroupName, SpeedwayWebLikeResource<TMetadata> resource)
        {
            if (_azure.WebApps.ListWebAppBasicByResourceGroup(resourceGroupName)
                .SpeedwayResourceExists(resource, resource.Context.Environment, out var webApp))
            {
                _logger.LogInformation("WebApp {Name} already exists with name {Id}", resource.Name, webApp!.Name);
                await _wellKnownPlatformComponentProvider
                    .GetDefaultAppServicePlan(); //ensure we push any changes required into the app-service plan.
                var platformResource = await _azure.WebApps.GetByIdAsync(webApp.Id);
                await UpdateWebAppTopLevelSettings(platformResource, resource);
                return platformResource;
            }
            else
            {
                var randomName = AzureEx.SuggestResourceName(resource.Context, resource.Name);
                var platformResource = await CreateWebApp(resourceGroupName, randomName, resource);

                _logger.LogInformation(
                    "Created new app {Id} to represent {ResourceGroup}/{Name}",
                    resourceGroupName,
                    resource.Name,
                    platformResource.Name);

                return platformResource;
            }
        }

        private async Task<IWebApp> CreateWebApp(string resourceGroupName, string randomName, SpeedwayWebLikeResource<TMetadata> resource)
        {
            var initialDefinition = _azure.WebApps.Define(randomName)
                .WithExistingLinuxPlan(await _wellKnownPlatformComponentProvider.GetDefaultAppServicePlan())
                .WithExistingResourceGroup(resourceGroupName);

            if (resource.ResourceMetadata.ActualWebAppDeploymentType == WebAppDeploymentType.Binaries)
            {
                return await initialDefinition.WithBuiltInImage(new RuntimeStack("DOTNETCORE", "5.0"))
                    .WithNetFrameworkVersion(ExpandableStringEnum<NetFrameworkVersion>.Parse("v5"))
                    .WithSpeedwayTag(resource.Context.Manifest.Id, resource.Name, resource.Context.Environment)
                    .WithHttpsOnly(true)
                    .WithoutPhp()
                    .WithSystemAssignedManagedServiceIdentity()
                    .CreateAsync();
            }

            return await initialDefinition
                .WithPublicDockerHubImage(resource.ResourceMetadata.Container!.ImageUri)
                .WithStartUpCommand(resource.ResourceMetadata.Container!.Run)
                .WithNetFrameworkVersion(ExpandableStringEnum<NetFrameworkVersion>.Parse("v5"))
                .WithSpeedwayTag(resource.Context.Manifest.Id, resource.Name, resource.Context.Environment)
                .WithHttpsOnly(true)
                .WithoutPhp()
                .WithSystemAssignedManagedServiceIdentity()
                .CreateAsync();
        }    
        
        /// <summary>
        /// Switch the app to / from containers if necessary
        /// </summary>
        /// <returns></returns>
        private async Task UpdateWebAppTopLevelSettings(IWebApp webApp, SpeedwayWebLikeResource<TMetadata> resource)
        {
            if (resource.ResourceMetadata.ActualWebAppDeploymentType == WebAppDeploymentType.Binaries)
            {
                await webApp
                    .Update()
                    .WithBuiltInImage(new RuntimeStack("DOTNETCORE", "5.0"))
                    .ApplyAsync();
            }
            else
            {
                await webApp
                    .Update()
                    .WithPublicDockerHubImage(resource.ResourceMetadata.Container!.ImageUri)
                    .WithStartUpCommand(resource.ResourceMetadata.Container!.Run)
                    .ApplyAsync();
            }
        }

    }
}