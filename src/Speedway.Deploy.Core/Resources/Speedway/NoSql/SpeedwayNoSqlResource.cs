using Speedway.Core.Resources;
using Speedway.Deploy.Core.Resources.Speedway.Context;

namespace Speedway.Deploy.Core.Resources.Speedway.NoSql
{
    /// <summary>
    /// Provides support for storage - blobs, queues
    /// </summary>
    // ReSharper disable once ClassNeverInstantiated.Global
    public class SpeedwayNoSqlResource : SpeedwayResource<SpeedwayNoSqlResourceMetadata>
    {
        public SpeedwayNoSqlResource(SpeedwayContextResource context, SpeedwayNoSqlResourceMetadata metadata,
            ISpeedwayPlatformTwinFactory platformTwinFactory) : base(context, metadata, platformTwinFactory)
        {
        }

    }
}
