using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Speedway.Core.Resources;
using Speedway.Deploy.Core.Resources.Speedway.Context;
using Speedway.Deploy.Core.Resources.Speedway.OAuthClient;
using Speedway.Deploy.Core.Resources.Speedway.WebApp;

namespace Speedway.Deploy.Core.Resources.Speedway.WebApi
{
    public class SpeedwayWebApiResource : SpeedwayWebLikeResource<SpeedwayWebApiResourceMetadata>
    {
        public SpeedwayWebApiResource(ILogger<SpeedwayWebApiResource> logger, SpeedwayContextResource context, SpeedwayWebApiResourceMetadata metadata, ISpeedwayPlatformTwinFactory twinFactory) : base(logger, context, metadata, twinFactory)
        {
        }

        public override Task PostProcess(IDictionary<ISpeedwayResource, SpeedwayResourceOutputMetadata> metadata)
        {
            var mappedOAuthClient = ResourceMetadata.OAuthClientName;
            GetPlatformTwin<ISpeedwayWebApiPlatformTwin>().SetOAuthClient(metadata.Keys.OfType<SpeedwayOAuthClientResource>().Single(x => x.Name == mappedOAuthClient));
            return base.PostProcess(metadata);
        }
    }
}