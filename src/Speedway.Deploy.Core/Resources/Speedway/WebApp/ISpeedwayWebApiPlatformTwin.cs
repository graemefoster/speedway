using Speedway.Deploy.Core.Resources.Speedway.OAuthClient;

namespace Speedway.Deploy.Core.Resources.Speedway.WebApp
{
    public interface ISpeedwayWebApiPlatformTwin
    {
        void SetOAuthClient(SpeedwayOAuthClientResource speedwayOAuthClientResource);
    }
}