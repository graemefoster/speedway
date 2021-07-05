using System;

namespace Speedway.PipelineBuilderApi.Ports.Adapters.DevOpsTypes
{
    public record SourceControlItem(Guid ObjectId, Guid CommitId);
}