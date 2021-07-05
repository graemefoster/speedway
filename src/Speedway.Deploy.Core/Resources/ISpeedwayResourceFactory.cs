using Speedway.Core;
using Speedway.Core.Resources;
using Speedway.Deploy.Core.Resources.Speedway;
using Speedway.Deploy.Core.Resources.Speedway.Context;

namespace Speedway.Deploy.Core.Resources
{
    /// <summary>
    /// Builds objects that will represent Speedway Resources
    /// </summary>
    public interface ISpeedwayResourceFactory
    {
        public SpeedwayContextResource BuildContext(SpeedwayManifest manifest, string environment);
        public ISpeedwayResource Build(SpeedwayContextResource context, SpeedwayResourceMetadata resourceMetadata);
    }
}