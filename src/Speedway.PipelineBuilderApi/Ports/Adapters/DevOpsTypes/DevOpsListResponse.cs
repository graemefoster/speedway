namespace Speedway.PipelineBuilderApi.Ports.Adapters.DevOpsTypes
{
    public record DevOpsListResponse<T>(int Count, T[] Value);
}