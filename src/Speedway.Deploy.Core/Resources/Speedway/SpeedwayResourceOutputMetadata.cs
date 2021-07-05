using System.Collections.Generic;

namespace Speedway.Deploy.Core.Resources.Speedway
{
    public record SpeedwayResourceOutputMetadata(
        string PlatformResourceName, 
        Dictionary<string, string> KnownSecrets,
        HashSet<string> RequiredSecrets);
}