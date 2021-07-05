namespace Speedway.Core.Resources
{
    public record SpeedwayOAuthRole(string Name, OAuthRoleAllowedType[] AllowedTypes);

    public enum OAuthRoleAllowedType
    {
        User,
        Application
    }

    public record SpeedwayOAuthScope(string Name);
    
    [SpeedwayResource("oauthRoleLink")]
    public record OAuthRoleLink(string Name, string[] Roles) : SpeedwayResourceLinkMetadata(Name)
    {
        public override void Accept(ILinkVisitor linkVisitor)
        {
            linkVisitor.VisitOAuthRoleLink(this);
        }
    }

    [SpeedwayResource("oauthScopeLink")]
    public record OAuthScopeLink(string Name, string[] Scopes) : SpeedwayResourceLinkMetadata(Name)
    {
        public override void Accept(ILinkVisitor linkVisitor)
        {
            linkVisitor.VisitOAuthScopeLink(this);
        }
    }
}