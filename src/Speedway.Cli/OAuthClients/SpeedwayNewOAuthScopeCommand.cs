using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using Speedway.Core.Resources;

namespace Speedway.Cli.OAuthClients
{
    // ReSharper disable once ClassNeverInstantiated.Global
    internal class DefaultValueAttributeSpeedwayNewOAuthScopeCommand : ISpeedwayCommand
    {
        public void BuildCommandHandler()
        {
            var parentCmd = new Command("scope");
            var cmd = new Command("new");
            parentCmd.AddCommand(cmd);

            var nameOption = new Option(new[] {"--name", "-n"}) {Argument = new Argument<string>(), IsRequired = true};
            var scopeNameOption = new Option(new[] {"--scope", "-s"}) {Argument = new Argument<string>(), IsRequired = true};

            cmd.AddOption(nameOption);
            cmd.AddOption(scopeNameOption);

            cmd.Handler = CommandHandler.Create<string, string>(Execute);
            TopLevelCommands.OAuthClients.AddCommand(parentCmd);
        }

        private void Execute(string name, string scope)
        {
            var speedwayManifestWrapper = SpeedwayManifestWrapper.Find();
            var toResource = speedwayManifestWrapper.FindResource<SpeedwayOAuthClientResourceMetadata>(name) ??
                             throw new ArgumentException($"Cannot find oauth client named {name}");

            if (toResource.Scopes == null)
            {
                speedwayManifestWrapper.ReplaceResource(toResource, toResource with {Scopes = new List<SpeedwayOAuthScope>()});
            }

            if (toResource.Scopes!.All(x => x.Name != scope))
            {
                toResource.Scopes.Add(new SpeedwayOAuthScope(scope));
            }

            speedwayManifestWrapper.Save();
            
            Console.WriteLine($"Added scope {scope} to oauth client {name}");
        }
    }
}