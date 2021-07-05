using System;
using System.Threading.Tasks;
using Speedway.Core.Resources;
using Speedway.Deploy.Core.Resources.Speedway.Context;

namespace Speedway.Deploy.Core.Resources.Speedway.Storage
{
    /// <summary>
    /// Provides support for storage - blobs, queues
    /// </summary>
    // ReSharper disable once ClassNeverInstantiated.Global
    public class SpeedwayStorageResource : SpeedwayResource<SpeedwayStorageResourceMetadata>, ISpeedwayResourceThatSupportsLinkingTo
    {
        public SpeedwayStorageResource(SpeedwayContextResource context, SpeedwayStorageResourceMetadata metadata,
            ISpeedwayPlatformTwinFactory platformTwinFactory) : base(context, metadata, platformTwinFactory)
        {
        }

        public Task GrantAccessTo(ISpeedwayResource resource, SpeedwayResourceLinkMetadata link)
        {
            var storageLink = link as StorageLink;
            if (storageLink == null)
                throw new InvalidOperationException($"Attempt to link a storage resource using a {link.GetType().Name}. Storage Resource only support StorageLink");
            return GetPlatformTwin<IStoragePlatformTwin>().GrantAccessTo(resource, storageLink);
        }
    }
}