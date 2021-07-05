using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;
using Speedway.Core;
using Speedway.Core.MermaidJs;

namespace Speedway.Cli
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            Console.WriteLine(@"                             __                    
   _________  ___  ___  ____/ /      ______ ___  __
  / ___/ __ \/ _ \/ _ \/ __  / | /| / / __ `/ / / /
 (__  ) /_/ /  __/  __/ /_/ /| |/ |/ / /_/ / /_/ / 
/____/ .___/\___/\___/\__,_/ |__/|__/\__,_/\__, /  
    /_/                                   /____/   

");
            
            var currentDir = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory); //to locate settings in correct folder.

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
                        .File(
                            @"c:\\logs\\speedway-.log",
                            LogEventLevel.Information,
                            rollingInterval: RollingInterval.Hour
                        )
                        .CreateLogger());
                })
                .ConfigureServices((hbdlr, s) =>
                {
                    s.Configure<SpeedwayApiSettings>(hbdlr.Configuration.GetSection("SpeedwayApi"));
                    s.AddSingleton<SpeedwayClientTokenProvider>();
                    s.AddSingleton<SpeedwayApiClient>();

                    s.AddHttpClient("speedwayApi",
                        (sp, cfg) =>
                        {
                            cfg.BaseAddress = new Uri(sp.GetRequiredService<IOptions<SpeedwayApiSettings>>().Value.Uri);
                        });

                    var commands = typeof(Program)
                        .Assembly
                        .GetTypes()
                        .Where(x => typeof(ISpeedwayCommand).IsAssignableFrom(x))
                        .Where(x => !x.IsAbstract);

                    foreach (var command in commands)
                    {
                        s.AddSingleton(typeof(ISpeedwayCommand), command);
                    }
                });

            Directory.SetCurrentDirectory(currentDir);

            using var host = hostBuilder.Build();

            var logger = host.Services.GetRequiredService<ILogger<Program>>();

            host.Services.GetRequiredService<ILogger<Program>>().LogInformation("Speedway running");

            var allHandlers = host
                .Services
                .GetRequiredService<IEnumerable<ISpeedwayCommand>>()
                .ToArray();

            var rootCommandBuilder =
                new CommandLineBuilder(new RootCommand("Speedway - setup your projects at lightspeed"))
                    .UseMiddleware(async (ctx, next) =>
                    {
                        await next(ctx);
                        if (!ctx.ParseResult.Errors.Any())
                        {
                            try
                            {
                                var speedwayManifestWrapper = SpeedwayManifestWrapper.Find();
                                var speedwayManifest = speedwayManifestWrapper.RawManifest;
                                var mermaid = new MermaidGenerator().Generate(speedwayManifest);
                                speedwayManifestWrapper.WriteMermaid(mermaid);
                            }
                            catch (InvalidOperationException)
                            {
                                //no-op. probably a container create command so no manifest file in current location.
                            }
                        }
                    })
                    .UseDefaults()
                    .UseHelp()
                    .UseExceptionHandler((e, ctx) =>
                    {
                        logger.LogError(e, "Failed to execute speedway command");
                        Console.WriteLine(e.InnerException?.Message ?? e.Message);
                        Console.WriteLine(e.ToString());
                        ctx.ResultCode = -1;
                    });

            foreach (var handler in allHandlers)
            {
                handler.BuildCommandHandler();
            }

            rootCommandBuilder.AddCommand(TopLevelCommands.Applications);
            rootCommandBuilder.AddCommand(TopLevelCommands.Containers);
            rootCommandBuilder.AddCommand(TopLevelCommands.OAuthClients);
            rootCommandBuilder.AddCommand(TopLevelCommands.Storage);
            rootCommandBuilder.AddCommand(TopLevelCommands.Secrets);

            var rootCommand = rootCommandBuilder.Build();

            var exitCommand = await rootCommand.InvokeAsync(args, new SystemConsole());

            return exitCommand;
        }
    }
}