using System;
using System.Threading.Tasks;
using Speedway.Core.Resources;
using Speedway.Deploy.Core.Resources.Speedway.Context;

namespace Speedway.Deploy.Core.Resources.Speedway
{
    // /// <summary>
    // /// Represents a resource which can be linked to. For example, a storage account can be accessed by a webapp so is a linkable resource
    // /// </summary>
    // /// <typeparam name="TResourceMetadata"></typeparam>
    // public abstract class LinkableSpeedwayResource<TResourceMetadata : SpeedwayResource<TResourceMetadata>, ILinkableSpeedwayResource where TResourceMetadata : SpeedwayResourceMetadata
    // {
    //
    //     protected LinkableSpeedwayResource(SpeedwayContextResource context, TResourceMetadata metadata, ISpeedwayPlatformTwinFactory platformTwinFactory) : base(context, metadata with { Links = metadata.Links ?? Array.Empty<SpeedwayResourceLinkMetadata>()}, platformTwinFactory)
    //     {
    //     }
    //
    //     public Task GrantAccessTo(ISpeedwayResourceWithIdentity resource, SpeedwayResourceLinkMetadata linkInformation)
    //     {
    //         return GetPlatformTwin<ISpeedwayPlatformTwinThatSupportsRuntimeAccess>().GrantAccessTo(resource, linkInformation);
    //     }
    // }
}