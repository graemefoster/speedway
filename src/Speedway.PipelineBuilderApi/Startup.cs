using MediatR;
using Microsoft.AspNetCore.Authentication.AzureAD.UI;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Logging;
using Speedway.Api.Extensions;
using Speedway.AzureSdk.Extensions;
using Speedway.Core;
using Speedway.PipelineBuilderApi.Features.Containers;
using Speedway.PipelineBuilderApi.Ports;
using Speedway.PipelineBuilderApi.Ports.Adapters;

namespace Speedway.PipelineBuilderApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddApplicationInsightsTelemetry();

            var section = Configuration.GetSection("PipelineBuilderSettings");
            var settings = section.Get<PipelineBuilderSettings>();
            services.Configure<PipelineBuilderSettings>(section);

            services.Configure<AzureADOptions>(Configuration.GetSection("AzureAd"));

            services
                .AddMicrosoftIdentityWebApiAuthentication(Configuration)
                .EnableTokenAcquisitionToCallDownstreamApi()
                .AddDownstreamWebApi("DevOps", options =>
                {
                    options.BaseUrl = settings.DevOpsUri;
                    options.Scopes = AadScopes.AzureDevOpsImpersonation;
                })
                .AddDownstreamWebApi("DevOpsEx", options =>
                {
                    options.BaseUrl = settings.DevOpsExUri;
                    options.Scopes = AadScopes.AzureDevOpsImpersonation;
                })
                .AddDownstreamWebApi("arm", options =>
                {
                    options.BaseUrl = $"https://management.azure.com/subscriptions/{settings.SubscriptionId}/";
                    options.Scopes = AadScopes.AzureResourceManagerImpersonation;
                })
                .AddDownstreamWebApi("graph", options =>
                {
                    options.BaseUrl = "https://graph.microsoft.com/v1.0";
                    options.Scopes = "https://graph.microsoft.com/.default";
                })
                .AddMicrosoftGraph()
                .AddInMemoryTokenCaches();

            services.AddScoped(sp =>
            {
                var aadOptions = sp.GetRequiredService<IOptions<AzureADOptions>>().Value;

                return Microsoft.Azure.Management.Fluent.Azure.Authenticate(
                        RestClient.Configure().WithEnvironment(AzureEnvironment.AzureGlobalCloud)
                            .WithBaseUri($"https://management.azure.com/")
                            .WithCredentials(new OnBehalfOfAzureCredentials(
                                sp.GetRequiredService<ITokenAcquisition>(),
                                new ServicePrincipalLoginInformation
                                {
                                    ClientId = aadOptions.ClientId,
                                    ClientSecret = aadOptions.ClientSecret,
                                }, aadOptions.TenantId, AzureEnvironment.AzureGlobalCloud))
                            .Build(),
                        aadOptions.TenantId)
                    .WithSubscription(settings.SubscriptionId);
            });


            services.AddControllers();

            typeof(Startup).Assembly.RegisterValidationServices(services);
            typeof(SpeedwayManifest).Assembly.RegisterValidationServices(services);

            services.AddScoped<ISourceControlRepository, AzureDevOpsSourceControlRepository>();
            services.AddScoped<NewContainerContext>();

            services.AddMediatR(typeof(Startup).Assembly);
            // services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UserNamePipelineDecorator<,>));

            services.AddMvcCore(options => { options.Filters.Add<SpeedwayExceptionFilter>(); });

            services.ExposeSwagger();
        }

        
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                IdentityModelEventSource.ShowPII = true;
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();


            app.UseSwagger();
            app.UseSwaggerUI(options =>
                {
                    options.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
                }
            );
            
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints => { 
                endpoints.MapControllers();
            });
        }
    }
}