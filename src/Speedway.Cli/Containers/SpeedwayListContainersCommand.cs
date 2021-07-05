using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Speedway.Cli.Containers
{
    class SpeedwayListContainersCommand : ISpeedwayCommand
    {
        private readonly SpeedwayApiClient _speedwayApiClient;

        public SpeedwayListContainersCommand(
            ILogger<SpeedwayListContainersCommand> logger,
            SpeedwayApiClient speedwayApiClient)
        {
            _speedwayApiClient = speedwayApiClient;
        }

        private async Task Execute()
        {
            Console.WriteLine("Fetching containers");
            var containers =
                await (await _speedwayApiClient.Send(new HttpRequestMessage(HttpMethod.Get, "container"), "Containers"))
                    .Content.ReadAsStringAsync();
            Console.WriteLine(containers);
            Console.WriteLine("-------------------------");
        }

        public void BuildCommandHandler()
        {
            var cmd = new Command("list");
            cmd.Handler = CommandHandler.Create(Execute);
            TopLevelCommands.Containers.AddCommand(cmd);
        }

    }
}