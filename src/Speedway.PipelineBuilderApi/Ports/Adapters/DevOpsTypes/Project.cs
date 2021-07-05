using System;

namespace Speedway.PipelineBuilderApi.Ports.Adapters.DevOpsTypes
{
    public record Project(Guid Id, string Name, string Url);

    public record VariableGroup(int Id, bool IsShared);
}