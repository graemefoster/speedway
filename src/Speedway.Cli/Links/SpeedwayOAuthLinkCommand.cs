using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using Speedway.Core.Resources;

namespace Speedway.Cli.Links
{
    // ReSharper disable once ClassNeverInstantiated.Global
    internal class SpeedwayNewOAuthLinkCommand : ISpeedwayCommand
    {
        public void BuildCommandHandler()
        {
            var cmd = new Command("link");

            var fromOption = new Option(new[] {"--from", "-f"}) {Argument = new Argument<string>(), IsRequired = true};
            var toOption = new Option(new[] {"--to", "-t"}) {Argument = new Argument<string>(), IsRequired = true};
            var scopesOption = new Option(new[] {"--scopes", "-s"})
                {Argument = new Argument<string[]>(), IsRequired = false};
            var rolesOption = new Option(new[] {"--roles", "-r"})
                {Argument = new Argument<string[]>(), IsRequired = false};

            cmd.AddOption(fromOption);
            cmd.AddOption(toOption);
            cmd.AddOption(scopesOption);
            cmd.AddOption(rolesOption);

            cmd.Handler = CommandHandler.Create<string, string, string[], string[]>(Execute);
            TopLevelCommands.OAuthClients.AddCommand(cmd);
        }

        private void Execute(string from, string to, string[]? scopes, string[]? roles)
        {
            var speedwayManifestWrapper = SpeedwayManifestWrapper.Find();
            var toResource =
                speedwayManifestWrapper.FindResource<SpeedwayOAuthClientResourceMetadata>(to) as IOAuthClientMetadata ??
                speedwayManifestWrapper.FindResource<SpeedwayExistingOAuthClientResourceMetadata>(to) ??
                throw new ArgumentException($"Cannot find oauth client named {to}");

            var fromResource = speedwayManifestWrapper.FindResource<SpeedwayResourceMetadata>(from) ??
                               throw new ArgumentException($"Cannot find resource named {from}");

            if (scopes != null)
            {
                if (!toResource.CanLinkTo(fromResource, typeof(OAuthScopeLink)))
                    throw new ArgumentException($"Cannot link from oauth-client to {fromResource.GetType().Name}");

                var scopesLink = toResource.Links.OfType<OAuthScopeLink>().Where(x => x.Name == from).ToArray();
                if (scopesLink.Any())
                {
                    foreach (var existing in scopesLink)
                    {
                        toResource.Links.Remove(existing);
                        scopes = scopes.Union(existing.Scopes).ToArray();
                    }
                }
                if (scopes.Any())
                {
                    toResource.Links.Add(new OAuthScopeLink(from, scopes));
                }
            }

            if (roles != null)
            {
                if (!toResource.CanLinkTo(fromResource, typeof(OAuthRoleLink)))
                    throw new ArgumentException($"Cannot link from oauth-client to {fromResource.GetType().Name}");

                var rolesLink = toResource.Links.OfType<OAuthRoleLink>().Where(x => x.Name == from).ToArray();
                if (rolesLink.Any())
                {
                    foreach (var existing in rolesLink)
                    {
                        toResource.Links.Remove(existing);
                        roles = roles.Union(existing.Roles).ToArray();
                    }
                }
                if (roles.Any())
                {
                    toResource.Links.Add(new OAuthRoleLink(from, roles));
                }
            }

            speedwayManifestWrapper.Save();

            Console.WriteLine(
                $"Linked oauth application {to} to resource {from} with scopes {string.Join(", ", scopes ?? new string [0])} and roles {string.Join(", ", roles ?? new string [0])}");
        }
    }
}