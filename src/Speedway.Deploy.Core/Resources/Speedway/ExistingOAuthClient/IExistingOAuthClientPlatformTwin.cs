using System.Threading.Tasks;

namespace Speedway.Deploy.Core.Resources.Speedway.ExistingOAuthClient
{
    public interface IExistingOAuthClientPlatformTwin: ISpeedwayPlatformTwin
    {
        Task RequestApplicationAccess(string[] roles, ISpeedwayResource allowedApplication);
        Task RequestDelegatedAccess(string[] scopes, ISpeedwayResource applicationThatWantsToDelegateThis);
    }
}