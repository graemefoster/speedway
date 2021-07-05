using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;

namespace Speedway.Core.Resources
{
    /// <summary>
    /// KnownApplications are applications that when consented to, implicitly consent to the scopes in this application. Use the .default scope to ensure that the user can see exactly what they are consenting to!
    /// </summary>
    [SpeedwayResource("oauthClient")]
    public record SpeedwayOAuthClientResourceMetadata(
        string Name,
        SpeedwayClientType ClientType,
        string SignOnUri,
        string[]? ReplyUrls,
        List<string>? RedirectsFrom,
        string[]? KnownApplications,
        List<SpeedwayOAuthRole>? Roles,
        List<SpeedwayOAuthScope>? Scopes,
        List<SpeedwayResourceLinkMetadata>? Links) : SpeedwayResourceMetadata(Name, "OAuth", Links ?? new List<SpeedwayResourceLinkMetadata>()), IOAuthClientMetadata
    {
        private static readonly Type[] CanLinkToScopeTypes = new Type[]
        {
            typeof(SpeedwayOAuthClientResourceMetadata),
        };
        private static readonly Type[] CanLinkToRoleTypes = new Type[]
        {
            typeof(SpeedwayOAuthClientResourceMetadata),
            typeof(SpeedwayWebApiResourceMetadata),
            typeof(SpeedwayWebAppResourceMetadata),
        };

        public bool CanLinkTo(SpeedwayResourceMetadata fromResource, Type link)
        {
            if (link == typeof(OAuthScopeLink)) return CanLinkToScopeTypes.Contains(fromResource.GetType());
            return CanLinkToRoleTypes.Contains(fromResource.GetType());
        }

        public override void Accept(INodeVisitor nodeVisitor)
        {
            nodeVisitor.VisitOAuthClient(this);
        }
    }
}