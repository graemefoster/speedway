using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Speedway.Core.Resources;
using Speedway.Deploy.Core.Resources.Speedway.Context;

namespace Speedway.Deploy.Core.Resources.Speedway
{
    public abstract class SpeedwayResource<TResourceMetadata> : ISpeedwayResource
        where TResourceMetadata : SpeedwayResourceMetadata
    {
        protected ISpeedwayPlatformTwin PlatformTwin { get; }
        public TResourceMetadata ResourceMetadata { get; }
        public SpeedwayContextResource Context { get; }

        protected SpeedwayResource(
            SpeedwayContextResource context,
            TResourceMetadata metadata,
            ISpeedwayPlatformTwinFactory platformTwinFactory)
        {
            ResourceMetadata = metadata;
            Context = context;
            PlatformTwin = platformTwinFactory.Build(this);
        }

        public string Name => ResourceMetadata.Name;
        public string ResourceType => ResourceMetadata.GetType().GetCustomAttribute<SpeedwayResourceAttribute>()!.Type;


        public virtual async Task<SpeedwayResourceOutputMetadata> ReflectOnPlatform()
        {
            return await PlatformTwin.Reflect();
        }

        public virtual T GetPlatformTwin<T>()
        {
            if (PlatformTwin is T twin) return twin;
            throw new InvalidOperationException(
                $"Platform twin of {GetType().Name} is {PlatformTwin.GetType().Name} - unexpected ask for {typeof(T).Name}");
        }

        public virtual Task PostProcess(IDictionary<ISpeedwayResource, SpeedwayResourceOutputMetadata> metadata)
        {
            return Task.CompletedTask;
        }

        public virtual T? Find<T>(string name) where T : ISpeedwayResource
        {
            if (Name == name && this is T) return (T) (object) this;
            return default;
        }

        public SpeedwayResourceMetadata GetMetadata()
        {
            return ResourceMetadata;
        }

        public SpeedwayResourceLinkMetadata[] Links => ResourceMetadata.Links.ToArray();

    }
}