using System.Collections.Generic;
using System.Text;
using Speedway.Core.Resources;

namespace Speedway.Core.MermaidJs
{
    internal class LinkVisitor : ILinkVisitor
    {
        private readonly SpeedwayResourceMetadata _resourceMetadata;
        private readonly StringBuilder _stringBuilder;
        private readonly List<MermaidConnector> _mermaidConnectors;

        public LinkVisitor(
            SpeedwayResourceMetadata resourceMetadata, 
            StringBuilder stringBuilder,
            List<MermaidConnector> mermaidConnectors)
        {
            _resourceMetadata = resourceMetadata;
            _stringBuilder = stringBuilder;
            _mermaidConnectors = mermaidConnectors;
        }

        public void VisitExistingOAuthRoleLink(OAuthRoleLink link)
        {
            _mermaidConnectors.Add(new MermaidConnector(link.Name, _resourceMetadata.Name, MermaidConnectorType.AuthorisedBy, string.Join(",", link.Roles)));
        }

        public void VisitExistingOAuthScopeLink(OAuthScopeLink link)
        {
            _mermaidConnectors.Add(new MermaidConnector(link.Name, _resourceMetadata.Name, MermaidConnectorType.AuthorisedBy, string.Join(",", link.Scopes)));
        }
        public void VisitOAuthRoleLink(OAuthRoleLink link)
        {
            _mermaidConnectors.Add(new MermaidConnector(link.Name, _resourceMetadata.Name, MermaidConnectorType.AuthorisedBy, string.Join(",", link.Roles)));
        }

        public void VisitOAuthScopeLink(OAuthScopeLink link)
        {
            _mermaidConnectors.Add(new MermaidConnector(link.Name, _resourceMetadata.Name, MermaidConnectorType.AuthorisedBy, string.Join(",", link.Scopes)));
        }

        public void VisitStorageLink(StorageLink link)
        {
            _mermaidConnectors.Add(new MermaidConnector(link.Name, _resourceMetadata.Name, MermaidConnectorType.UsesCoreInfrastructure, string.Join(",", string.Join(",", link.Access))));
        }

        public void VisitSecretsLink(SecretsLink link)
        {
            _mermaidConnectors.Add(new MermaidConnector(link.Name, _resourceMetadata.Name, MermaidConnectorType.UsesCoreInfrastructure, string.Join(",", string.Join(",", link.Access))));
        }

        public void VisitNoSqlLink(NoSqlLink link)
        {
            _mermaidConnectors.Add(new MermaidConnector(link.Name, _resourceMetadata.Name, MermaidConnectorType.UsesCoreInfrastructure, "Uses"));
        }

        public void VisitLinks()
        {
            foreach (var link in _resourceMetadata.Links)
            {
                link.Accept(this);
            }
        }
    }
}