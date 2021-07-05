using System.Collections.Generic;

namespace Speedway.Core.Resources
{
    [SpeedwayResource("nosql")]
    public record SpeedwayNoSqlResourceMetadata
        (string Name, string[]? Containers) : SpeedwayResourceMetadata(Name, "NoSql", new List<SpeedwayResourceLinkMetadata>())
    {
        public override void Accept(INodeVisitor nodeVisitor)
        {
            nodeVisitor.VisitNoSql(this);
        }
    }
}