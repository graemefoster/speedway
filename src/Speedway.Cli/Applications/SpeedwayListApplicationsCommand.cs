using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;

namespace Speedway.Cli.Applications
{
    class SpeedwayListApplicationsCommand: ISpeedwayCommand
    {
        
        private Task Execute()
        {
            Console.WriteLine("Apps in current manifest:");
            Console.WriteLine("-------------------------");

            var speedwayManifestWrapper = SpeedwayManifestWrapper.Find();
            foreach (var application in speedwayManifestWrapper.Applications)
            {
                Console.WriteLine(application);
            }
            Console.WriteLine();
            Console.WriteLine("-------------------------");

            return Task.CompletedTask;
        }

        public void BuildCommandHandler()
        {
            TopLevelCommands.Applications.AddCommand(new Command("list", "Shows applications in speedway manifest file")
            {
                Handler = CommandHandler.Create(Execute)
            });
        }
    }
}