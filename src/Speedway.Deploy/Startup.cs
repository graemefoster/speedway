using System;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Azure.Identity;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authentication.AzureAD.UI;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Logging;
using Speedway.Api.Extensions;
using Speedway.Core;
using Speedway.Deploy.Core;
using Speedway.Deploy.Provider.AzureAppService;

namespace Speedway.Deploy
{
    public class Startup
    {
        private readonly IHostEnvironment _hostEnvironment;

        public Startup(IConfiguration configuration, IHostEnvironment hostEnvironment)
        {
            _hostEnvironment = hostEnvironment;
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddApplicationInsightsTelemetry();
            
            services
                .AddMicrosoftIdentityWebApiAuthentication(Configuration);

            services.AddControllers();

            var intermediateAzureAdOptions = new AzureADOptions();
            Configuration.GetSection("AzureAd").Bind(intermediateAzureAdOptions);
            services.Configure<AzureADOptions>(Configuration.GetSection("AzureAd"));
            var runningLocally = _hostEnvironment.IsDevelopment() && (Environment.GetEnvironmentVariable("APPSETTING_WEBSITE_SITE_NAME") == null);
            if (runningLocally)
            {
                IdentityModelEventSource.ShowPII = true;
                services.AddSingleton<IAzureCredentialProvider, LocalAzureCredentialProvider>();
                var app = ConfidentialClientApplicationBuilder.Create(intermediateAzureAdOptions.ClientId)
                    .WithClientSecret(intermediateAzureAdOptions.ClientSecret)
                    .WithTenantId(intermediateAzureAdOptions.TenantId)
                    .Build();
                services.AddSingleton(app);
            }
            else
            {
                services.AddSingleton<IAzureCredentialProvider, MsiAzureCredentialProvider>();
            }

            var speedwaySettingsConfigurationSection = Configuration.GetSection("SpeedwaySettings");
            var intermediateSpeedwaySettings = speedwaySettingsConfigurationSection.Get<AzureSpeedwaySettings>();
            speedwaySettingsConfigurationSection.Bind(intermediateSpeedwaySettings);

            services.Configure<AzureSpeedwaySettings>(speedwaySettingsConfigurationSection);
            services.Configure<AzureSpeedwaySettings>(options =>
            {
                options.DeployApiAzureApplicationId = intermediateAzureAdOptions.ClientId;
            });
            services.AddMediatR(typeof(Startup).Assembly);

            RegisterValidationServices(services);

            services.AddAzureClients(clients =>
            {
                clients.UseCredential(sp =>
                    {
                        if (runningLocally)
                        {
                            var settings = sp.GetRequiredService<IOptions<AzureADOptions>>();
                            return new ClientSecretCredential(settings.Value.TenantId, settings.Value.ClientId,
                                settings.Value.ClientSecret);
                        }

                        return new ManagedIdentityCredential();
                    })
                    .AddBlobServiceClient(new Uri($"https://{intermediateSpeedwaySettings.ArtifactStorageName}.blob.core.windows.net/{intermediateSpeedwaySettings.ArtifactBlobContainerName}"));
            });

            services.AddScoped(sp => Microsoft.Azure.Management.Fluent.Azure
                .Configure()
                .Authenticate(sp.GetRequiredService<IAzureCredentialProvider>().Provide())
                .WithSubscription(intermediateSpeedwaySettings.SubscriptionId));

            services.AddScoped(sp =>
                Microsoft.Azure.Management.Storage.Fluent.StorageManager.Configure()
                    .Authenticate(sp.GetRequiredService<IAzureCredentialProvider>().Provide(), intermediateSpeedwaySettings.SubscriptionId)
            );
            
            services.AddScoped<IGraphServiceClient>(sp => GetGraphApiClient(runningLocally, sp.GetService<IConfidentialClientApplication>()).Result);
            
            services.AddMvcCore(options => { options.Filters.Add<SpeedwayExceptionFilter>(); });

            services.RegisterSpeedway();
            services.RegisterAzureAppServiceProvider();
        }

        private void RegisterValidationServices(IServiceCollection services)
        {
            typeof(Startup).Assembly.RegisterValidationServices(services);
            typeof(SpeedwayManifest).Assembly.RegisterValidationServices(services);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
        
        private static async Task<GraphServiceClient> GetGraphApiClient(bool isLocal, IConfidentialClientApplication? confidentialClient)
        {
            if (isLocal)
            {
                return new GraphServiceClient(new DelegateAuthenticationProvider(r =>
                {
                    var token = confidentialClient!.AcquireTokenForClient(new[] {"https://graph.microsoft.com/.default"}).ExecuteAsync().Result;
                    r.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
                    return Task.CompletedTask;
                }));
            }

            var azureServiceTokenProvider = new AzureServiceTokenProvider();
            string accessToken = await azureServiceTokenProvider
                .GetAccessTokenAsync("https://graph.microsoft.com/");

            var graphServiceClient = new GraphServiceClient(
                new DelegateAuthenticationProvider((requestMessage) =>
                {
                    requestMessage
                        .Headers
                        .Authorization = new AuthenticationHeaderValue("bearer", accessToken);

                    return Task.CompletedTask;
                }));

            return graphServiceClient;
        }
        
    }
}