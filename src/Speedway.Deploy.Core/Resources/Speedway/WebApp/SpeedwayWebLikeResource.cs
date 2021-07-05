using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Speedway.Core.Resources;
using Speedway.Deploy.Core.Resources.Speedway.Configuration;
using Speedway.Deploy.Core.Resources.Speedway.Context;

namespace Speedway.Deploy.Core.Resources.Speedway.WebApp
{
    /// <summary>
    /// Represents a component which runs as an App / API.
    /// </summary>
    // ReSharper disable once ClassNeverInstantiated.Global
    public class SpeedwayWebLikeResource<T> :
        SpeedwayResource<T>,
        ISupportDeployingArtifacts,
        ISpeedwayResourceWithConfiguration
        where T: SpeedwayWebAppResourceMetadata
    {
        private readonly ConfigurationMerger _configurationMerger;

        // ReSharper disable once MemberCanBeProtected.Global
        public SpeedwayWebLikeResource(
            ILogger<SpeedwayWebLikeResource<T>> logger,
            SpeedwayContextResource context,
            T metadata,
            ISpeedwayPlatformTwinFactory twinFactory) : base(context, metadata, twinFactory)
        {
            Configuration = ResourceMetadata.Configuration ?? new Dictionary<string, string>();
            _configurationMerger = new ConfigurationMerger(logger, this);
        }

        /// <summary>
        /// Applies new configuration based on the manifest file.
        /// </summary>
        /// <param name="metadata"></param>
        /// <returns></returns>
        public override async Task PostProcess(IDictionary<ISpeedwayResource, SpeedwayResourceOutputMetadata> metadata)
        {
            var updatedConfiguration = _configurationMerger.CalculateNewAppConfiguration(metadata);
            var speedwayWebAppPlatformTwin = base.GetPlatformTwin<ISpeedwayWebAppPlatformTwin>();
            await speedwayWebAppPlatformTwin.ReflectAppSettings(updatedConfiguration);
            var originalConfiguration = ((SpeedwayWebAppResourceOutputMetadata) metadata[this]).Configuration;
            ReflectUpdatedSettingsOnResourceOutput(updatedConfiguration, originalConfiguration);
            await base.PostProcess(metadata);
        }

        private static void ReflectUpdatedSettingsOnResourceOutput(Dictionary<string, string> updatedConfiguration, Dictionary<string, string> originalConfiguration)
        {
            foreach (var updatedEntry in updatedConfiguration)
            {
                originalConfiguration[updatedEntry.Key] = updatedEntry.Value;
            }
        }

        /// <summary>
        /// Deploys a package to the platform twin.
        /// </summary>
        /// <param name="zipArchive"></param>
        /// <returns></returns>
        public Task Deploy(ZipArchive zipArchive)
        {
            return base.GetPlatformTwin<ISpeedwayWebAppPlatformTwin>().Deploy(zipArchive); //await _deploymentHelper.CreateZipForArtifact(zipArchive, Name));
        }
        
        public IDictionary<string, string> Configuration { get; }
    }
}