using Microsoft.Extensions.DependencyInjection;
using Speedway.Deploy.Core.Resources.Speedway;
using Speedway.Deploy.Provider.AzureAppService.WebApp;

namespace Speedway.Deploy.Provider.AzureAppService
{
    public static class ServiceRegistrationEx
    {
        public static IServiceCollection RegisterAzureAppServiceProvider(this IServiceCollection services)
        {
            services.AddScoped<ISpeedwayPlatformTwinFactory, AzureTwinFactory>();
            services.AddScoped<IWellKnownPlatformComponentProvider, AppServiceApimAppInsightsPattern>();
            services.AddScoped(typeof(AppServiceWebAppArtifactDeployer<>));
            services.AddScoped(typeof(AppServiceWebAppBuilder<>));
            services.AddScoped(typeof(AppServiceApmBuilder<>));
            return services;
        }
    }
}