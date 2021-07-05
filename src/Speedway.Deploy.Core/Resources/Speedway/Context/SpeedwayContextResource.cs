using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Speedway.Core;
using Speedway.Core.Resources;

namespace Speedway.Deploy.Core.Resources.Speedway.Context
{
    /// <summary>
    /// Represents a logical grouping of components that make a speedway app
    /// </summary>
    // ReSharper disable once ClassNeverInstantiated.Global
    public class SpeedwayContextResource: ISpeedwayResource
    {
        private readonly ISpeedwayPlatformTwin _platformTwin;

        public SpeedwayContextResource(SpeedwayManifest manifest, string environment, ISpeedwayPlatformTwinFactory platformTwin)
        {
            Manifest = manifest;
            Environment = environment;
            _platformTwin = platformTwin.Build(this);
        }

        public SpeedwayManifest Manifest { get; }
        public string Environment { get; }

        public string Name => Manifest.Slug;
        public string ResourceType => "context";
        public string Region => GetPlatformTwin<ISpeedwayContextPlatformTwin>().Region();

        public Task<SpeedwayResourceOutputMetadata> ReflectOnPlatform()
        {
            return _platformTwin.Reflect();
        }

        public SpeedwayResourceMetadata GetMetadata()
        {
            return new SpeedwayContextResourceMetadata(Manifest.DisplayName, "Context");
        }

        public T GetPlatformTwin<T>()
        {
            if (_platformTwin is T twin) return twin;
            throw new InvalidOperationException($"Platform twin of {GetType().Name} os {_platformTwin.GetType().Name} - unexpected ask for {nameof(T)}");
        }

        public Task PostProcess(IDictionary<ISpeedwayResource, SpeedwayResourceOutputMetadata> metadata)
        {
            return Task.CompletedTask;
        }

        public T? Find<T>(string name) where T: ISpeedwayResource
        {
            if (Name == name && typeof(T) == GetType()) return (T)(object)this;
            return default;
        }

        public string GetEnvironmentShort()
        {
            return Environment.ToLowerInvariant().Substring(0, Math.Min(Environment.Length, 3));
        }
    }
}