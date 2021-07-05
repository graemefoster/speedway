using System;
using System.Threading.Tasks;
using Speedway.Core.Resources;
using Speedway.Deploy.Core.Resources.Speedway.Context;

namespace Speedway.Deploy.Core.Resources.Speedway.ExistingOAuthClient
{
    /// <summary>
    /// Represents a component with identity that already exists on the platform.
    /// </summary>
    // ReSharper disable once ClassNeverInstantiated.Global
    public class SpeedwayExistingOAuthClientResource : SpeedwayResource<SpeedwayExistingOAuthClientResourceMetadata>, ISpeedwayResourceThatSupportsLinkingTo
    {
        public SpeedwayExistingOAuthClientResource(
            SpeedwayContextResource context, 
            SpeedwayExistingOAuthClientResourceMetadata metadata, 
            ISpeedwayPlatformTwinFactory twinFactory) : base(context, metadata, twinFactory)
        {
        }

        public Task GrantAccessTo(ISpeedwayResource wantsAccess, SpeedwayResourceLinkMetadata linkInformation)
        {
            var roleAccess = linkInformation as OAuthRoleLink;
            if (roleAccess != null)
            {
                return GetPlatformTwin<IExistingOAuthClientPlatformTwin>().RequestApplicationAccess(roleAccess.Roles, wantsAccess);
            }
            var scopeAccess = linkInformation as OAuthScopeLink;
            if (scopeAccess != null)
            {
                return GetPlatformTwin<IExistingOAuthClientPlatformTwin>().RequestDelegatedAccess(scopeAccess.Scopes, wantsAccess);
            }

            throw new NotSupportedException(
                $"Existing OAuth Client does not support links of type {linkInformation.GetType().Name}");        
        }
    }
}