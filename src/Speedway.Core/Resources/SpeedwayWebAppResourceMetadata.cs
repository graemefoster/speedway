using System;
using System.Collections.Generic;

namespace Speedway.Core.Resources
{
    [SpeedwayResource("webapp")]
    public record SpeedwayWebAppResourceMetadata(
        string Name,
        WebAppDeploymentType? WebAppDeploymentType,
        ContainerMetadata? Container,
        IDictionary<string, string>? Configuration,
        HashSet<string>? RequiredSecretNames,
        bool RequiresApiManagementKey,
        string FriendlyType = null!
    ) :
        SpeedwayResourceMetadata(Name, FriendlyType ?? "WebApp", new List<SpeedwayResourceLinkMetadata>())
    {
        public override void Accept(INodeVisitor nodeVisitor)
        {
            nodeVisitor.VisitWebApp(this);
        }

        public WebAppDeploymentType ActualWebAppDeploymentType =>
            WebAppDeploymentType ?? Resources.WebAppDeploymentType.Binaries;
    }

    public record ContainerMetadata(string ImageUri, string? Run, int? Port);
}
