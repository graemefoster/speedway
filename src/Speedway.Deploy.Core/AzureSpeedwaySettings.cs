namespace Speedway.Deploy.Core
{
    public record AzureSpeedwaySettings
    {
        public string SubscriptionId { get; init; } = "";
        public string Region { get; init; } = "";
        public string ArtifactBlobContainerName { get; init; } = "";
        public string ArtifactStorageName { get; init; } = "";
        public string DefaultAppServicePlanResourceGroup { get; init; } = "";
        public string DefaultAppServicePlanName { get; init; } = "";
        public string DeployApiAzureManagedIdentityId { get; init; } = "";
        public string DeployApiAzureApplicationId { get; set; } = "";
        public string ManifestStorageKey { get; set; } = "";
    }
}