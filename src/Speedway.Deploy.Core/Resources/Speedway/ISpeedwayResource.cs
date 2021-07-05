using System.Collections.Generic;
using System.Threading.Tasks;
using Speedway.Core;
using Speedway.Core.Resources;

namespace Speedway.Deploy.Core.Resources.Speedway
{
    /// <summary>
    /// Can reflect a Speedway Resource on a platform. This might involve deploying more than one native resource
    /// </summary>
    public interface ISpeedwayResource
    {
        string Name { get; }
        string ResourceType { get; }
        Task<SpeedwayResourceOutputMetadata> ReflectOnPlatform();
        SpeedwayResourceMetadata GetMetadata();
        T GetPlatformTwin<T>();
        Task PostProcess(IDictionary<ISpeedwayResource, SpeedwayResourceOutputMetadata> metadata);
        T? Find<T>(string name) where T : ISpeedwayResource;
    }
}