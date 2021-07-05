using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using Speedway.Core.Resources;

namespace Speedway.Cli.Links
{
    // ReSharper disable once ClassNeverInstantiated.Global
    internal class SpeedwayNewSecretLinkCommand : ISpeedwayCommand
    {
        public void BuildCommandHandler()
        {
            var cmd = new Command("link");

            var fromOption = new Option(new[] {"--from", "-f"}) {Argument = new Argument<string>(), IsRequired = true};
            var toOption = new Option(new[] {"--to", "-t"}) {Argument = new Argument<string>(), IsRequired = true};
            var accessOption = new Option(new[] {"--access", "-a"})
                {Argument = new Argument<LinkAccess>(), IsRequired = true};

            cmd.AddOption(fromOption);
            cmd.AddOption(toOption);
            cmd.AddOption(accessOption);

            cmd.Handler = CommandHandler.Create<string, string, LinkAccess>(Execute);
            TopLevelCommands.Secrets.AddCommand(cmd);
        }

        private void Execute(string from, string to, LinkAccess access)
        {
            var speedwayManifestWrapper = SpeedwayManifestWrapper.Find();
            var toResource = speedwayManifestWrapper.FindResource<SpeedwaySecretContainerResourceMetadata>(to) ??
                             throw new ArgumentException($"Cannot find secret container named {to}");
            var fromResource = speedwayManifestWrapper.FindResource<SpeedwayResourceMetadata>(from) ??
                               throw new ArgumentException($"Cannot find resource named {from}");
            if (!toResource.CanLinkTo(fromResource))
                throw new ArgumentException($"Cannot link from storage to {fromResource.GetType().Name}");

            var link = toResource.Links.OfType<SecretsLink>().SingleOrDefault(x => x.Name == from);
            if (link != null)
            {
                toResource.Links.Remove(link);
            }

            toResource.Links.Add(new SecretsLink(from, access));

            speedwayManifestWrapper.Save();
            
            Console.WriteLine($"Linked secrets container {to} to resource {from} with access {access}");
        }
    }
}