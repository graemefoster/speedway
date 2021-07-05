using System.CommandLine;

namespace Speedway.Cli
{
    internal static class TopLevelCommands
    {
        internal static readonly Command Containers = new Command("container");
        internal static readonly Command Applications = new Command("app");
        internal static readonly Command OAuthClients = new Command("oauth-client");
        internal static readonly Command Storage = new Command("storage");
        internal static readonly Command Secrets = new Command("secrets");
    }
}