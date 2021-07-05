using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Speedway.Api.Extensions
{
    public static class WebApiRegistrationEx
    {
        public static void RegisterValidationServices(this Assembly assembly, IServiceCollection services)
        {
            AssemblyScanner.FindValidatorsInAssembly(assembly)
                .ForEach(item => services.AddScoped(item.InterfaceType, item.ValidatorType));

            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(MediatrPipelineValidationBehavior<,>));
        }

        public static IServiceCollection ExposeSwagger(this IServiceCollection services)
        {
            services.AddSwaggerGen();
            return services;
        }
    }
}