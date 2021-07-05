using System.Collections.Generic;

namespace Speedway.Core.Resources
{
    public record SpeedwayContextResourceMetadata(string Name, string FriendlyType) : SpeedwayResourceMetadata(Name,
        FriendlyType, new List<SpeedwayResourceLinkMetadata>())
    {
        public override void Accept(INodeVisitor nodeVisitor)
        {
            //context isn't really a node. It's a holder for something like a resource-group
        }
    }
}