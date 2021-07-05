using System.Collections.Generic;
using System.Text.Json;

namespace Speedway.PipelineBuilderApi.Ports.Adapters.DevOpsTypes
{
    internal record ServiceEndpointAuthorization(Dictionary<string, JsonElement> Parameters);
}