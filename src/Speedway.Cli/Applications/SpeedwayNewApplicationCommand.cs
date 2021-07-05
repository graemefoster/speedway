using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Threading.Tasks;
using Speedway.Core.Resources;

namespace Speedway.Cli.Applications
{
    // ReSharper disable once ClassNeverInstantiated.Global
    internal class SpeedwayNewApplicationCommand : ISpeedwayCommand
    {
        private static readonly AppType[] DotNetApps = new[]
            {AppType.DotNetApi, AppType.DotNetMvc, AppType.DotNetWebApp};

        private async Task Execute(string name, AppType type)
        {
            var speedwayManifestWrapper = SpeedwayManifestWrapper.Find();

            if (speedwayManifestWrapper.Applications.Any(x => x == name))
            {
                throw new InvalidOperationException($"A webapp already exists with the name {name}");
            }

            if (DotNetApps.Contains(type))
            {
                var dotnetSubType = type switch
                {
                    AppType.DotNetApi => "webapi",
                    AppType.DotNetMvc => "mvc",
                    AppType.DotNetWebApp => "webapp",
                    _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown App Type")
                };
                var newAppDotNetCliHandler =
                    new NewAppDotNetCliHandler(name, dotnetSubType);
                await speedwayManifestWrapper.NewComponent(newAppDotNetCliHandler);
            }

            var defaultSecrets = speedwayManifestWrapper.FindResource<SpeedwaySecretContainerResourceMetadata>("Default");
            if (defaultSecrets == null)
            {
                defaultSecrets = new SpeedwaySecretContainerResourceMetadata("Default", null);
                speedwayManifestWrapper.AddResource(defaultSecrets);
            }

            var coreMetadata = new SpeedwayWebAppResourceMetadata(
                name,
                WebAppDeploymentType.Binaries,
                null,
                new Dictionary<string, string>()
                {
                    {"ConfigurationExample", "Something"}
                },
                new HashSet<string>(),
                false);

            if (type == AppType.DotNetApi)
            {
                var apiResource = new SpeedwayWebApiResourceMetadata(
                    coreMetadata.Name,
                    WebAppDeploymentType.Binaries,
                    null,
                    coreMetadata.Configuration,
                    coreMetadata.RequiredSecretNames,
                    true,
                    "<oauth-client-name-here>",
                    "/swagger/v1/swagger.json");

                speedwayManifestWrapper.AddResource(apiResource);
                defaultSecrets.Links.Add(new SecretsLink(coreMetadata.Name, LinkAccess.Read));
                
            } else
            {
                speedwayManifestWrapper.AddResource(coreMetadata);
                defaultSecrets.Links.Add(new SecretsLink(coreMetadata.Name, LinkAccess.Read));
            }

            speedwayManifestWrapper.Save();
            
            Console.WriteLine($"Added new application {name} of type {type}");
        }

        public void BuildCommandHandler()
        {
            var cmd = new Command("new", "Create a new api in the speedway manifest file");

            var nameOption = new Option(new[] {"--name", "-n"}) {Argument = new Argument<string>(), IsRequired = true};
            var typeOption = new Option(new[] {"--type", "-t"}) {Argument = new Argument<AppType>(), IsRequired = true};

            cmd.AddOption(nameOption);
            cmd.AddOption(typeOption);

            cmd.Handler = CommandHandler.Create<string, AppType>(Execute);
            TopLevelCommands.Applications.AddCommand(cmd);
        }
    }
}