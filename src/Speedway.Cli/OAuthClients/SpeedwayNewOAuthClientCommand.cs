using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Threading.Tasks;
using Speedway.Core.Resources;

namespace Speedway.Cli.OAuthClients
{
    // ReSharper disable once ClassNeverInstantiated.Global
    internal class SpeedwayNewOAuthClientCommand : ISpeedwayCommand
    {

        private Task Execute(string name, SpeedwayClientType clientType, string signOnUri, string[] replyUris)
        {
            var speedwayManifestWrapper = SpeedwayManifestWrapper.Find();

            if (speedwayManifestWrapper.OAuthClients.Any(x => x == name))
            {
                throw new InvalidOperationException($"An OAuth Client already exists with the name {name}");
            }

            speedwayManifestWrapper.AddResource(new SpeedwayOAuthClientResourceMetadata(
                name,
                clientType,
                signOnUri,
                replyUris,
                new List<string>(),
                new string[0],
                new List<SpeedwayOAuthRole>(), 
                new List<SpeedwayOAuthScope>(), 
                new List<SpeedwayResourceLinkMetadata>()));

            speedwayManifestWrapper.Save();

            return Task.CompletedTask;
        }

        public void BuildCommandHandler()
        {
            var cmd = new Command("new", "Create a new OAuth Clients in the speedway manifest file");

            var nameOption = new Option(new[] {"--name", "-n"}) {Argument = new Argument<string>(), IsRequired = true };
            var typeOption = new Option(new[] {"--type", "-t"}) {Argument = new Argument<SpeedwayClientType>(), IsRequired = true };
            var signOnUriOption = new Option(new[] {"--sign-on-uri", "-s"}) {Argument = new Argument<string>(), IsRequired = true };
            var replyUrisOption = new Option(new[] {"--reply-uris", "-r"}) {Argument = new Argument<string>(), IsRequired = true, AllowMultipleArgumentsPerToken = true };

            cmd.AddOption(nameOption);
            cmd.AddOption(typeOption);
            cmd.AddOption(signOnUriOption);
            cmd.AddOption(replyUrisOption);

            cmd.Handler = CommandHandler.Create<string, SpeedwayClientType, string, string[]>(Execute);
            TopLevelCommands.OAuthClients.AddCommand(cmd);
        }
    }
}