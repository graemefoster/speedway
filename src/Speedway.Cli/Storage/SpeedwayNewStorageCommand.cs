using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using Speedway.Core.Resources;

namespace Speedway.Cli.Storage
{
    // ReSharper disable once ClassNeverInstantiated.Global
    internal class SpeedwayNewStorageCommand : ISpeedwayCommand
    {
        public void BuildCommandHandler()
        {
            var cmd = new Command("new");

            var nameOption = new Option(new[] {"--name", "-n"}) {Argument = new Argument<string>(), IsRequired = true};
            var containerNameOption = new Option(new[] {"--containerName", "-c"})
            {
                Argument = new Argument<string>(), IsRequired = true,
                Description = "Name of the queue, or container within the storage resource"
            };
            var typeOption = new Option(new[] {"--type", "-t"}) {Argument = new Argument<SpeedwayStorageResourceContainerType>(), IsRequired = true};

            cmd.AddOption(nameOption);
            cmd.AddOption(containerNameOption);
            cmd.AddOption(typeOption);

            cmd.Handler = CommandHandler.Create<string, string, SpeedwayStorageResourceContainerType>(Execute);
            TopLevelCommands.Storage.AddCommand(cmd);
        }

        private void Execute(string name, string containerName, SpeedwayStorageResourceContainerType type)
        {
            var speedwayManifestWrapper = SpeedwayManifestWrapper.Find();
            var resource = speedwayManifestWrapper.FindResource<SpeedwayStorageResourceMetadata>(name) ??
                           speedwayManifestWrapper.AddResource(new SpeedwayStorageResourceMetadata(name, new List<SpeedwayStorageResourceContainer>(), null));

            if (resource.Containers.Any(x => x.Name == containerName))
            {
                throw new InvalidOperationException($"A Storage Resource already exists with this container {name} - {containerName}");
            }
            
            resource.Containers.Add(new SpeedwayStorageResourceContainer(type, containerName));

            speedwayManifestWrapper.Save();

            Console.WriteLine($"Created new storage container {name} with {type} - {containerName}");
        }

    }
}