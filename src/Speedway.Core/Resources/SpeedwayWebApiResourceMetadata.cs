using System.Collections.Generic;

namespace Speedway.Core.Resources
{
    [SpeedwayResource("webapi")]
    public record SpeedwayWebApiResourceMetadata(
        string Name,
        WebAppDeploymentType? WebAppDeploymentType,
        ContainerMetadata? Container,
        IDictionary<string, string>? Configuration,
        HashSet<string>? RequiredSecretNames,
        bool RequiresApiManagementKey,
        string OAuthClientName,
        string Swagger
    ) :
        SpeedwayWebAppResourceMetadata(Name, WebAppDeploymentType, Container, Configuration, RequiredSecretNames, RequiresApiManagementKey, "Api")
    {
        public override void Accept(INodeVisitor nodeVisitor)
        {
            nodeVisitor.VisitWebApi(this);
        }
    }
}