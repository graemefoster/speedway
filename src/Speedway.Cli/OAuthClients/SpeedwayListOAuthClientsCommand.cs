using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;

namespace Speedway.Cli.OAuthClients
{
    class SpeedwayListOAuthClientsCommand: ISpeedwayCommand
    {
        
        private Task Execute()
        {
            Console.WriteLine("OAuth Clients in current manifest:");
            Console.WriteLine("-------------------------");

            var speedwayManifestWrapper = SpeedwayManifestWrapper.Find();
            foreach (var client in speedwayManifestWrapper.OAuthClients)
            {
                Console.WriteLine(client);
            }
            Console.WriteLine();
            Console.WriteLine("-------------------------");

            return Task.CompletedTask;
        }

        public void BuildCommandHandler()
        {
            TopLevelCommands.OAuthClients.AddCommand(new Command("list", "Shows OAuth Clients in speedway manifest file")
            {
                Handler = CommandHandler.Create(Execute)
            });
        }
    }
}