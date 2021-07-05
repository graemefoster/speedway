using System.Threading.Tasks;

namespace Speedway.Core.Resources
{
    public abstract record SpeedwayResourceLinkMetadata(string Name)
    {
        public abstract void Accept(ILinkVisitor linkVisitor);
    }

    public interface ILinkVisitor
    {
        void VisitOAuthRoleLink(OAuthRoleLink link);
        void VisitOAuthScopeLink(OAuthScopeLink link);
        void VisitStorageLink(StorageLink link);
        void VisitSecretsLink(SecretsLink link);
        void VisitNoSqlLink(NoSqlLink link);
    }
    public interface INodeVisitor
    {
        void VisitWebApp(SpeedwayWebAppResourceMetadata node);
        void VisitWebApi(SpeedwayWebApiResourceMetadata node);
        void VisitSecrets(SpeedwaySecretContainerResourceMetadata node);
        void VisitStorage(SpeedwayStorageResourceMetadata node);
        void VisitExistingOAuthClient(SpeedwayExistingOAuthClientResourceMetadata node);
        void VisitOAuthClient(SpeedwayOAuthClientResourceMetadata node);
        void VisitNoSql(SpeedwayNoSqlResourceMetadata node);
    }

    [SpeedwayResource("secretsLink")]
    public record SecretsLink(string Name, LinkAccess Access) : SpeedwayResourceLinkMetadata(Name)
    {
        public override void Accept(ILinkVisitor linkVisitor)
        {
            linkVisitor.VisitSecretsLink(this);
        }
    }

    [SpeedwayResource("storageLink")]
    public record StorageLink(string Name, LinkAccess Access) : SpeedwayResourceLinkMetadata(Name)
    {
        public override void Accept(ILinkVisitor linkVisitor)
        {
            linkVisitor.VisitStorageLink(this);
        }
    }

    [SpeedwayResource("nosqlLink")]
    public record NoSqlLink(string Name) : SpeedwayResourceLinkMetadata(Name)
    {
        public override void Accept(ILinkVisitor linkVisitor)
        {
            linkVisitor.VisitNoSqlLink(this);
        }
    }

    public enum LinkAccess
    {
        Read,
        ReadWrite
    }
}