
using System.Collections.Generic;

namespace Speedway.Core.Resources
{
    public abstract record SpeedwayResourceMetadata(string Name, string FriendlyType, List<SpeedwayResourceLinkMetadata> Links)
    {
        public abstract void Accept(INodeVisitor nodeVisitor);
    }
}