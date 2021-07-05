using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Threading.Tasks;
using Speedway.Core;
using Speedway.Deploy.Core.Resources.Speedway;

namespace Speedway.Deploy.Core
{
    public interface IManifestDeployer
    {
        public Task Deploy(
            string environment, 
            SpeedwayManifest manifest, 
            ZipArchive? zipArchive,
            Func<IDictionary<ISpeedwayResource, SpeedwayResourceOutputMetadata>, Task>? postLinkCallback);
    }
}