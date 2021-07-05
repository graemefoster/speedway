using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace Speedway.Deploy.Core.Resources.Speedway.WebApp
{
    /// <summary>
    /// Ensures that the platform reflects a given speedway resource
    /// </summary>
    public interface ISpeedwayWebAppPlatformTwin : ISpeedwayPlatformTwin
    {
        Task Deploy(ZipArchive binaries);
        Task ReflectAppSettings(Dictionary<string, string> newAndUpdatedSettings);
    }
}