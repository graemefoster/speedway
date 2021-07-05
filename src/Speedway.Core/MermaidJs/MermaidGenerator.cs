using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Speedway.Core.MermaidJs
{
    public class MermaidGenerator
    {
        public MermaidGenerator()
        {
        }

        public string Generate(SpeedwayManifest speedwayManifest)
        {
            var connectors = new List<MermaidConnector>();

            var sb = new StringBuilder();
            sb.AppendLine($"```mermaid");
            sb.AppendLine($"graph LR");

            // var webappLike = speedwayManifest.Resources.OfType<SpeedwayWebAppResourceMetadata>().ToArray();
            // var reordered = webappLike.Concat(speedwayManifest.Resources.Except(webappLike)).ToArray();
            var nodeVisitor = new NodeVisitor(sb, connectors);
            foreach (var resource in speedwayManifest.Resources.OrderBy(x => x.Name))
            {
                resource.Accept(nodeVisitor);
            }

            foreach (var resourceMetadata in speedwayManifest.Resources.OrderBy(x => x.Name))
            {
                var visitor = new LinkVisitor(resourceMetadata, sb, connectors);
                visitor.VisitLinks();
            }

            foreach (var indexedConnector in connectors.Select((connector, idx) => new {connector, idx}))
            {
                indexedConnector.connector.Generate(sb, indexedConnector.idx);
            }
            
            sb.AppendLine($"```");
            sb.AppendLine();

            return sb.ToString();
        }
    }
}