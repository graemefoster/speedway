using System;
using System.IO;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Serilog;
using Serilog.Events;
using Speedway.AzureSdk.Extensions;
using Speedway.Core.MermaidJs;
using Speedway.Deploy.Core;
using Speedway.Deploy.Provider.AzureAppService;
using SpeedwayManifest = Speedway.Core.SpeedwayManifest;

namespace Speedway.Bootstrap
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Speedway Bootstrap");

            if (args.Length == 1 && args[0] == "mermaid")
            {
                Console.WriteLine(new MermaidGenerator().Generate(LoadSpeedwayManifest()));
                return;
            }

            var hostBuilder = Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hbdlr, cfg) =>
                {
                    if (hbdlr.HostingEnvironment.IsDevelopment())
                    {
                        cfg.AddUserSecrets(typeof(Program).Assembly);
                    }
                })
                .ConfigureLogging(lb =>
                {
                    lb.ClearProviders();
                    lb.AddSerilog(new LoggerConfiguration()
                        .WriteTo
                        .File(@"c:\\logs\\speedway-.log", LogEventLevel.Information,
                            rollingInterval: RollingInterval.Hour)
                        .WriteTo.Console()
                        .CreateLogger());
                })
                .ConfigureServices((hbdlr, services) =>
                {
                    services.AddScoped<Bootstrapper>();

                    services.AddSingleton(LoadSpeedwayManifest());
                    

                    var azureAdSettings = hbdlr.Configuration.GetSection("AzureAd");
                    services.RegisterSpeedway();
                    services.RegisterAzureAppServiceProvider();
                    
                    //override the well known builder, with the bootstrap version.
                    services.AddScoped<IWellKnownPlatformComponentProvider, AppServiceApimAppInsightsPatternInfrastructureBuilder>();

                    var tenantId = azureAdSettings.GetValue<string>("TenantId");

                    var clientId = azureAdSettings.GetValue<string>("ClientId");
                    var publicClientApplication = PublicClientApplicationBuilder
                        .Create(clientId)
                        .WithTenantId(tenantId)
                        .WithRedirectUri("http://localhost")
                        .Build();

                    services.Configure<SpeedwayBootstrapSettings>(hbdlr.Configuration.GetSection("SpeedwayBootstrap"));
                    services.Configure<AzureSpeedwaySettings>(hbdlr.Configuration.GetSection("SpeedwayBootstrap"));
                    services.Configure<AzureSpeedwaySettings>(options =>
                    {
                        options.DeployApiAzureApplicationId = clientId;
                    });


                    services.AddSingleton(new InteractiveAzureCredentials(
                        publicClientApplication,
                        tenantId));

                    services.AddScoped(sp =>
                        Microsoft.Azure.Management.Fluent.Azure.Configure()
                            .Authenticate(sp.GetRequiredService<InteractiveAzureCredentials>())
                            .WithSubscription(sp.GetRequiredService<IOptions<SpeedwayBootstrapSettings>>().Value
                                .SubscriptionId));

                    services.AddScoped(sp =>
                        Microsoft.Azure.Management.Storage.Fluent.StorageManager.Configure()
                            .Authenticate(sp.GetRequiredService<InteractiveAzureCredentials>(),
                                sp.GetRequiredService<IOptions<SpeedwayBootstrapSettings>>().Value.SubscriptionId)
                    );

                    services.AddScoped<IGraphServiceClient>(sp =>
                        new GraphServiceClient(new DelegateAuthenticationProvider(async (requestMessage) =>
                        {
                            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer",
                                await sp.GetRequiredService<InteractiveAzureCredentials>()
                                    .GetToken(new[] {"https://graph.microsoft.com/.default"}));
                        })));
                });

            using var host = hostBuilder.Build();
            using var scope = host.Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<Bootstrapper>().Execute();
        }

        private static SpeedwayManifest LoadSpeedwayManifest()
        {
            using (var manifestResourceStream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("Speedway.Bootstrap.SpeedwayManifest.json")!)
            using (var reader = new StreamReader(manifestResourceStream))
            {
                var manifest = SpeedwayManifest.DeserializeFromJson(reader.ReadToEnd());
                return manifest;
            }
        }
    }
}