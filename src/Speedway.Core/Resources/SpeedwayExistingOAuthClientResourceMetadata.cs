using System;
using System.Collections.Generic;
using System.Linq;

namespace Speedway.Core.Resources
{
    [SpeedwayResource("preExistingOAuthClient")]
    // ReSharper disable once ClassNeverInstantiated.Global
    public record SpeedwayExistingOAuthClientResourceMetadata(string Name, string ApplicationId) :
        SpeedwayResourceMetadata(Name, "ExistingOAuth",
            new List<SpeedwayResourceLinkMetadata>()), IOAuthClientMetadata 
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
            nodeVisitor.VisitExistingOAuthClient(this);
        }
    }
}