using System.Collections.Generic;

namespace Speedway.Deploy.Core.Resources.Speedway.WebApp
{
    public record SpeedwayWebAppResourceOutputMetadata(
        string PlatformName,
        string Uri,
        string EphemeralIdentityId,
        Dictionary<string, string> Configuration,
        Dictionary<string, string> KnownSecrets,
        HashSet<string> RequiredSecrets) :
        SpeedwayResourceOutputMetadata(PlatformName, KnownSecrets, RequiredSecrets),
        ISpeedwayResourceOutputMetadataWithConfiguration;
    
}