using System;

namespace Speedway.PipelineBuilderApi.Ports.Adapters.DevOpsTypes
{
    public record Operation(Guid Id, string Status, string Url);
}