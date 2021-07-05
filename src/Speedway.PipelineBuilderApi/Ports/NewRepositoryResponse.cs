using System;

namespace Speedway.PipelineBuilderApi.Ports
{
    public record NewRepositoryResponse(Guid Id, string Name, string Uri, string GitUri);
}