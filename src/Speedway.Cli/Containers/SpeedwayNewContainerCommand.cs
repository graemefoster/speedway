using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Process = System.Diagnostics.Process;

namespace Speedway.Cli.Containers
{
    // ReSharper disable once ClassNeverInstantiated.Global
    internal class SpeedwayNewContainerCommand : ISpeedwayCommand
    {
        private readonly SpeedwayApiClient _speedwayApiClient;

        public SpeedwayNewContainerCommand(
            ILogger<SpeedwayNewContainerCommand> logger,
            SpeedwayApiClient speedwayApiClient)
        {
            _speedwayApiClient = speedwayApiClient;
        }

        public void BuildCommandHandler()
        {
            var cmd = new Command("new");

            var nameOption = new Option(new[] {"--slug", "-s"}) {Argument = new Argument<string>(), IsRequired = true};
            var initialUserOption = new Option(new[] {"--initial-user", "-u"}) {Argument = new Argument<string>(), IsRequired = true};
            var typeOption = new Option(new[] {"--display-name", "-d"}) {Argument = new Argument<string>(), IsRequired = true};

            cmd.AddOption(nameOption);
            cmd.AddOption(typeOption);
            cmd.AddOption(initialUserOption);

            cmd.Handler = CommandHandler.Create<string, string, string>(Execute);
            TopLevelCommands.Containers.AddCommand(cmd);
        }

        private async Task Execute(string slug, string displayName, string initialUser)
        {
            Validate();

            Console.WriteLine("Creating new Speedway container");
            var message = new HttpRequestMessage(HttpMethod.Post, "container")
            {
                Content = JsonContent.Create(new
                {
                    slug,
                    displayName,
                    initialDeveloper = initialUser
                })
            };
            var response = await _speedwayApiClient.Send(message, "Containers");
            var responseContent = (JObject?) JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync());
            if (responseContent == null)
                throw new InvalidOperationException("Unable to convert response from Speedway");
            if (response.IsSuccessStatusCode)
            {
                var repoUri = responseContent.Value<string>("gitUri");
                var process = "git.exe";
                var args = $"clone {repoUri}";
                await Process.Start(process, args).WaitForExitAsync();
                Console.WriteLine("Speedway has created new project, and cloned it.");
            }
            else
            {
                Console.WriteLine(responseContent.Value<string>("error"));
            }
        }

        private void Validate()
        {
            var currentPath = Directory.GetCurrentDirectory();
            if (Directory.Exists(Path.Combine(currentPath, ".speedway")))
                throw new InvalidOperationException(".speedway folder already exists in this location.");
        }
    }
}