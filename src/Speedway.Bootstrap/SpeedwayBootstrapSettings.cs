using Speedway.Deploy.Core;

namespace Speedway.Bootstrap
{
    public record SpeedwayBootstrapSettings : AzureSpeedwaySettings
    {
        public string Environment { get; init; } = "";
        public string ApimPublisherEmail { get; set; } = "";
    }
}