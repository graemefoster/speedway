using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Speedway.Cli.Applications
{
    public class NewAppDotNetCliHandler : ISpeedwayManifestNewComponentHandler
    {
        private readonly string _name;
        private readonly string _applicationType;
        public string? LocalRedirectPath { get; private set; }

        public NewAppDotNetCliHandler(string name, string applicationType)
        {
            _name = name;
            _applicationType = applicationType;
        }

        public async Task CreateComponent(string speedwayRootPath)
        {
            var path = Path.Combine(speedwayRootPath, "src", _name);
            var psi = new ProcessStartInfo("dotnet", $"new {_applicationType} -o {path}");
            var process = Process.Start(psi)!;
            await process.WaitForExitAsync();

            var launchSettings =
                (JObject) JsonConvert.DeserializeObject(
                    File.ReadAllText(Path.Combine(path, "Properties", "launchsettings.json")))!;
            
            LocalRedirectPath =
                $"https://localhost:{launchSettings["iisSettings"]!["iisExpress"]!.Value<int>("sslPort")}/signin-oidc";
        }
    }
}