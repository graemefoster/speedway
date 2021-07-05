using System;
using System.Collections.Generic;
using System.Linq;

namespace Speedway.Core.Resources
{
    [SpeedwayResource("storage")]
    public record SpeedwayStorageResourceMetadata(string Name,
        IList<SpeedwayStorageResourceContainer> Containers, 
        List<SpeedwayResourceLinkMetadata>? Links) : SpeedwayResourceMetadata(Name, "Storage", Links ?? new List<SpeedwayResourceLinkMetadata>())
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
            nodeVisitor.VisitStorage(this);
        }
    }
}