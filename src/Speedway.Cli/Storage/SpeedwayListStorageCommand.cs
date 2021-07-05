using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;

namespace Speedway.Cli.Storage
{
    class SpeedwayListStorageCommand: ISpeedwayCommand
    {
        
        private Task Execute()
        {
            Console.WriteLine("Storage in current manifest:");
            Console.WriteLine("-------------------------");

            var speedwayManifestWrapper = SpeedwayManifestWrapper.Find();
            foreach (var application in speedwayManifestWrapper.Storage)
            {
                Console.WriteLine(application);
            }
            Console.WriteLine();
            Console.WriteLine("-------------------------");

            return Task.CompletedTask;
        }

        public void BuildCommandHandler()
        {
            TopLevelCommands.Storage.AddCommand(new Command("list", "Shows storage in speedway manifest file")
            {
                Handler = CommandHandler.Create(Execute)
            });
        }
    }
}