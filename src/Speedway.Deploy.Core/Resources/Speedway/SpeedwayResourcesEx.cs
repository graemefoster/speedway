using System.Collections.Generic;
using Speedway.Deploy.Core.Resources.Speedway.ExistingOAuthClient;
using Speedway.Deploy.Core.Resources.Speedway.OAuthClient;

namespace Speedway.Deploy.Core.Resources.Speedway
{
    public static class SpeedwayResourcesEx
    {
        public static SpeedwayOAuthClientResource? FindSpeedwayOAuthClient(this IEnumerable<ISpeedwayResource> resources, string name)
        {
            foreach (var resource in resources)
            {
                var isThisYou = resource.Find<SpeedwayOAuthClientResource>(name);
                if (isThisYou != null) return isThisYou;
            }

            return null;
        }        
        
        public static SpeedwayExistingOAuthClientResource? FindSpeedwayExistingOAuthClient(this IEnumerable<ISpeedwayResource> resources, string name)
        {
            foreach (var resource in resources)
            {
                var isThisYou = resource.Find<SpeedwayExistingOAuthClientResource>(name);
                if (isThisYou != null) return isThisYou;
            }

            return null;
        }
    }
}