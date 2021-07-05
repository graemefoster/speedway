using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Speedway.PipelineBuilderApi.Ports.Adapters.DevOpsTypes
{
    internal record ServiceEndpoint(Guid Id, string Name, ServiceEndpointAuthorization Authorization,
        Dictionary<string, JsonElement> Data);
}