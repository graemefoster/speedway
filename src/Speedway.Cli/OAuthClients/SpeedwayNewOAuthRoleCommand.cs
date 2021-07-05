using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using Speedway.Core.Resources;

namespace Speedway.Cli.OAuthClients
{
    // ReSharper disable once ClassNeverInstantiated.Global
    internal class SpeedwayNewOAuthRoleCommand : ISpeedwayCommand
    {
        public void BuildCommandHandler()
        {
            var parentCmd = new Command("role");
            var cmd = new Command("new");
            parentCmd.AddCommand(cmd);

            var nameOption = new Option(new[] {"--name", "-n"}) {Argument = new Argument<string>(), IsRequired = true};
            var roleNameOption = new Option(new[] {"--role", "-r"}) {Argument = new Argument<string>(), IsRequired = true};
            var allowedTypesOption = new Option(new[] {"--allowedTypes", "-a"}) {Argument = new Argument<OAuthRoleAllowedType[]>(), IsRequired = true};

            cmd.AddOption(nameOption);
            cmd.AddOption(roleNameOption);
            cmd.AddOption(allowedTypesOption);

            cmd.Handler = CommandHandler.Create<string, string, OAuthRoleAllowedType[]>(Execute);
            TopLevelCommands.OAuthClients.AddCommand(parentCmd);
        }

        private void Execute(string name, string role, OAuthRoleAllowedType[] allowedTypes)
        {
            var speedwayManifestWrapper = SpeedwayManifestWrapper.Find();
            var toResource = speedwayManifestWrapper.FindResource<SpeedwayOAuthClientResourceMetadata>(name) ??
                             throw new ArgumentException($"Cannot find oauth client named {name}");

            if (toResource.Roles == null)
            {
                toResource = speedwayManifestWrapper.ReplaceResource(toResource, toResource with {Roles = new List<SpeedwayOAuthRole>()});
            }

            var existingRole = toResource.Roles!.SingleOrDefault(x => x.Name == role);
            if (existingRole != null)
            {
                toResource.Roles.Remove(existingRole);
            }

            toResource.Roles.Add(new SpeedwayOAuthRole(role, allowedTypes.Distinct().OrderBy(x => x.ToString()).ToArray()));

            speedwayManifestWrapper.Save();
            
            Console.WriteLine($"Added role {role} to oauth client {name} with allowedTypes {string.Join(", ", allowedTypes)}");
        }
    }
}