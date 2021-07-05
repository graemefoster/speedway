using System;

namespace Speedway.PipelineBuilderApi
{
    public record PipelineBuilderSettings
    {
        public string DevOpsUri { get; init; } = "";
        public string DevOpsExUri { get; init; } = "";
        public string SubscriptionId { get; init; } = "";
        public string SubscriptionName { get; init; } = "";
        public string[] Environments { get; init; } = Array.Empty<string>();
        public string DevOpsPipelineAdApplicationId { get; init; } = "";
        public string DevOpsPipelineServicePrincipalSecret { get; init; } = "";
        public string DeployApiAzureAdApplicationId { get; init; } = "";
        public string ArtifactStorageName { get; init; } = "";
        public string DeployApiUri { get; init; } = "";
        public string PolicyApiUriBase { get; init; } = "";
        public string PolicyApiAuthorisationToken { get; init; } = "";
    }
}