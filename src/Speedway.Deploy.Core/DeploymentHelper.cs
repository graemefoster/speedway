using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Speedway.Deploy.Core
{
    public class DeploymentHelper
    {
        private readonly ILogger<DeploymentHelper> _logger;

        public DeploymentHelper(ILogger<DeploymentHelper> logger)
        {
            _logger = logger;
        }
        
        public async Task CreateZipForArtifact(ZipArchive zipArchive, string resourceName, Func<Stream, Task> useStreamTask)
        {
            var wellKnownArtifactLocation = $"artifacts/{resourceName}/";
            _logger.LogInformation("Looking for {Artifact}", wellKnownArtifactLocation);
            if (zipArchive.Entries.Any(x => x.FullName.StartsWith(wellKnownArtifactLocation)))
            {
                _logger.LogInformation("Found {Artifact}. Proceeding to deploy", wellKnownArtifactLocation);

                //build a new zip up with these
                await using (var compressedFileStream = new MemoryStream())
                {
                    using (var zipFile = new ZipArchive(compressedFileStream, ZipArchiveMode.Create, true))
                    {
                        foreach (var fileToCompress in zipArchive.Entries.Where(x =>
                            x.FullName.StartsWith(wellKnownArtifactLocation) && !x.FullName.EndsWith("/")))
                        {
                            _logger.LogInformation("Adding file {File} to deployment", fileToCompress.Name);
                            await using (var fileStream = fileToCompress.Open())
                            {
                                var entry = zipFile.CreateEntry(
                                    fileToCompress.FullName.Substring(wellKnownArtifactLocation.Length));
                                await using (var entryStream = entry.Open())
                                {
                                    await fileStream.CopyToAsync(entryStream);
                                }
                            }
                        }
                    }

                    compressedFileStream.Position = 0;
                    await useStreamTask(compressedFileStream);
                }
            }
            else
            {
                _logger.LogWarning("Could not find {Artifact} in artifacts. Unable to proceed with deployment",
                    wellKnownArtifactLocation);
            }
        }
            
    }
}