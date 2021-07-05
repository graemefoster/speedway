using Newtonsoft.Json;

namespace Speedway.PipelineBuilderApi.Ports.Adapters.DevOpsTypes
{
    public record GraphListResponse<T>([JsonProperty("@odata.count")] int Count, T[] Value);
}