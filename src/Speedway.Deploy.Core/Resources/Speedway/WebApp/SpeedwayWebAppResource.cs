using Microsoft.Extensions.Logging;
using Speedway.Core.Resources;
using Speedway.Deploy.Core.Resources.Speedway.Context;

namespace Speedway.Deploy.Core.Resources.Speedway.WebApp
{
    public class SpeedwayWebAppResource :
        SpeedwayWebLikeResource<SpeedwayWebAppResourceMetadata>
    {
        // ReSharper disable once MemberCanBeProtected.Global
        public SpeedwayWebAppResource(ILogger<SpeedwayWebAppResource> logger, SpeedwayContextResource context, SpeedwayWebAppResourceMetadata metadata, ISpeedwayPlatformTwinFactory twinFactory) : base(logger, context, metadata, twinFactory)
        {
        }
    }
}