using System;
using System.Linq;
using System.Collections.Generic;

namespace Speedway.Core.Resources
{
    [SpeedwayResource("secrets")]
    public record SpeedwaySecretContainerResourceMetadata(
        string Name, 
        List<SpeedwayResourceLinkMetadata>? Links) 
        : SpeedwayResourceMetadata(Name, "Secrets", Links ?? new List<SpeedwayResourceLinkMetadata>())
    {
        private static readonly Type[] CanLinkToTypes = new Type[]
        {
            typeof(SpeedwayOAuthClientResourceMetadata),
            typeof(SpeedwayWebApiResourceMetadata),
            typeof(SpeedwayWebAppResourceMetadata),
        };

        public bool CanLinkTo(SpeedwayResourceMetadata fromResource)
        {
            return CanLinkToTypes.Contains(fromResource.GetType());
        }

        public override void Accept(INodeVisitor nodeVisitor)
        {
            nodeVisitor.VisitSecrets(this);
        }
    }
}