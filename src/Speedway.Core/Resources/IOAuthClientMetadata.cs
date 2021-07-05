using System;
using System.Collections.Generic;

namespace Speedway.Core.Resources
{
    public interface IOAuthClientMetadata
    {
        bool CanLinkTo(SpeedwayResourceMetadata fromResource, Type link);
        List<SpeedwayResourceLinkMetadata> Links { get; }
    }
}