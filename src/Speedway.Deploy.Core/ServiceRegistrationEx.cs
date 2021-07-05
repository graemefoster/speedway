using Microsoft.Extensions.DependencyInjection;
using Speedway.Deploy.Core.Resources;

namespace Speedway.Deploy.Core
{
    public static class ServiceRegistrationEx
    {
        public static IServiceCollection RegisterSpeedway(
            this IServiceCollection services)
        {
            services.AddScoped<ISpeedwayResourceFactory, SpeedwayResourceFactory>();
            services.AddScoped<IManifestDeployer, ManifestDeployer>();
            services.AddScoped<DeploymentHelper>();

            
            return services;
        }
    }
}