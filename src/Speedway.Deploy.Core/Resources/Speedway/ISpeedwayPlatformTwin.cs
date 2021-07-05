using System.Threading.Tasks;

namespace Speedway.Deploy.Core.Resources.Speedway
{
    /// <summary>
    /// Ensures that the platform reflects a given speedway resource
    /// </summary>
    public interface ISpeedwayPlatformTwin
    {
        Task<SpeedwayResourceOutputMetadata> Reflect();
    }
}