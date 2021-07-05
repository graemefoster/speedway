using System;

namespace Speedway.Cli
{
    public record SpeedwayApiSettings
    {
        public SpeedwayApiSettings()
        {
        }
        
        public string Uri { get; init; } = "";
        public Guid ClientId { get; init; } = Guid.Empty;
    }
}