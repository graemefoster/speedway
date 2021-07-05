using System.Collections.Generic;
using System.Linq;
using System.Text;
using Speedway.Core.Resources;

namespace Speedway.Core.MermaidJs
{
    internal class NodeVisitor : INodeVisitor
    {
        private readonly StringBuilder _stringBuilder;
        private readonly List<MermaidConnector> _mermaidConnectors;
        private readonly List<string> _styledSubgraphs = new List<string>();

        public NodeVisitor(StringBuilder stringBuilder, List<MermaidConnector> mermaidConnectors)
        {
            _stringBuilder = stringBuilder;
            _mermaidConnectors = mermaidConnectors;
        }

        public void VisitWebApp(SpeedwayWebAppResourceMetadata node)
        {
            DefaultVisit(node);
        }

        public void VisitWebApi(SpeedwayWebApiResourceMetadata node)
        {
            DefaultVisit(node);
        }

        public void VisitSecrets(SpeedwaySecretContainerResourceMetadata node)
        {
            if (DefaultVisit(node))
            {
                _stringBuilder.AppendLine($"style {node.FriendlyType} fill:#300");
            }
        }

        public void VisitStorage(SpeedwayStorageResourceMetadata node)
        {
            if (DefaultVisit(node))
            {
                _stringBuilder.AppendLine($"style {node.FriendlyType} fill:#300");
            }
        }

        public void VisitExistingOAuthClient(SpeedwayExistingOAuthClientResourceMetadata node)
        {
            if (DefaultVisit(node))
            {
                _stringBuilder.AppendLine($"style {node.FriendlyType} fill:#030");
            }
        }

        public void VisitOAuthClient(SpeedwayOAuthClientResourceMetadata node)
        {
            if (DefaultVisit(node))
            {
                _stringBuilder.AppendLine($"style {node.FriendlyType} fill:#303");
            }
            foreach (var redirect in node.RedirectsFrom ?? Enumerable.Empty<string>())
            {
                _mermaidConnectors.Add(new MermaidConnector(node.Name, redirect, MermaidConnectorType.Authorises));
            }
        }

        public void VisitNoSql(SpeedwayNoSqlResourceMetadata node)
        {
            if (DefaultVisit(node))
            {
                _stringBuilder.AppendLine($"style {node.FriendlyType} fill:#030");
            }
        }

        /// <summary>
        /// Returns true if this is the first time this type of node has been visited. 
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private bool DefaultVisit(SpeedwayResourceMetadata node)
        {
            _stringBuilder.AppendLine($"    subgraph {node.FriendlyType}");
            _stringBuilder.AppendLine($"        {node.Name.Replace("-", "_")};");
            _stringBuilder.AppendLine($"    end");
            if (!_styledSubgraphs.Contains(node.FriendlyType))
            {
                _styledSubgraphs.Add(node.FriendlyType);
                return true;
            }

            return false;
        }
    }
}