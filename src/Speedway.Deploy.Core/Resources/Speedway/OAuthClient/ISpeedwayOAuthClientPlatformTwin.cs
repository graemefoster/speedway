using System.Collections.Generic;
using System.Threading.Tasks;

namespace Speedway.Deploy.Core.Resources.Speedway.OAuthClient
{
    public interface ISpeedwayOAuthClientPlatformTwin : ISpeedwayPlatformTwin
    {
        Task RequestApplicationAccess(string[] roles, ISpeedwayResource allowedApplication);
        Task RequestDelegatedAccess(string[] scopes, ISpeedwayResource applicationThatWantsToDelegateThis);
        Task SetKnownApplications(ISpeedwayPlatformTwin knownApplication);
        Task AddRedirects(IEnumerable<string> redirectUris);
    }
}