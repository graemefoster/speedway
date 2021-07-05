using System.Collections.Generic;

namespace Speedway.Deploy.Core.Resources.Speedway
{
    public interface ISpeedwayResourceOutputMetadataWithConfiguration
    {
        Dictionary<string, string> Configuration { get; }
    }
}