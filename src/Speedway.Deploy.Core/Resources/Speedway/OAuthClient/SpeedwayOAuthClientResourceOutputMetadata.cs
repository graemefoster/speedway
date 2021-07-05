using System.Collections.Generic;

namespace Speedway.Deploy.Core.Resources.Speedway.OAuthClient
{
    public record SpeedwayOAuthClientResourceOutputMetadata(string PlatformName, string ClientId, Dictionary<string, string> KnownSecrets) : SpeedwayResourceOutputMetadata(PlatformName, KnownSecrets, new HashSet<string>());
}