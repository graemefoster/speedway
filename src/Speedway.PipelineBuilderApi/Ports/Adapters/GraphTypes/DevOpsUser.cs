using System;

namespace Speedway.PipelineBuilderApi.Ports.Adapters.GraphTypes
{
    public record DevOpsUsers(UserEntitlement[] Items, UserEntitlement[] Members);
    public record UserEntitlement(Guid Id, DevOpsUser User);
    public record DevOpsUser(Guid Id, string PrincipalName);
}