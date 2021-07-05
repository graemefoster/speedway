using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using Azure.Security.KeyVault.Certificates;
using Speedway.Core.Resources;

namespace Speedway.Cli.OAuthClients
{
    // ReSharper disable once ClassNeverInstantiated.Global
    internal class SpeedwayNewOAuthRedirectorCommand : ISpeedwayCommand
    {
        public void BuildCommandHandler()
        {
            var parentCmd = new Command("redirect");
            var cmd = new Command("new");
            parentCmd.AddCommand(cmd);

            var nameOption = new Option(new[] {"--name", "-n"}) {Argument = new Argument<string>(), IsRequired = true};
            var redirectorOption = new Option(new[] {"--redirectFrom", "-r"}) {Argument = new Argument<string>(), IsRequired = true};

            cmd.AddOption(nameOption);
            cmd.AddOption(redirectorOption);

            cmd.Handler = CommandHandler.Create<string, string>(Execute);
            TopLevelCommands.OAuthClients.AddCommand(parentCmd);
        }

        private void Execute(string name, string redirectFromName)
        {
            var speedwayManifestWrapper = SpeedwayManifestWrapper.Find();
            var toResource = speedwayManifestWrapper.FindResource<SpeedwayOAuthClientResourceMetadata>(name) ??
                             throw new ArgumentException($"Cannot find oauth client named {name}");

            var _ = speedwayManifestWrapper.FindResource<SpeedwayWebAppResourceMetadata>(name) ??
                             throw new ArgumentException($"Cannot find oauth client named {name}");

            if (toResource.RedirectsFrom == null)
            {
                toResource = speedwayManifestWrapper.ReplaceResource(toResource, toResource with {RedirectsFrom = new List<string>()});
            }

            
            if (!toResource.RedirectsFrom!.Contains(redirectFromName))
            {
                toResource.RedirectsFrom.Add(redirectFromName);
            }

            speedwayManifestWrapper.Save();
            
            Console.WriteLine($"Added redirect from {redirectFromName} to oauth client {name}");
        }
    }
}