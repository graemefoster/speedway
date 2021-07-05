using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Speedway.Core;
using Speedway.Core.Resources;
using Speedway.Deploy.Core.Resources;
using Speedway.Deploy.Core.Resources.Speedway;
using Speedway.Deploy.Core.Resources.Speedway.Context;
using Speedway.Deploy.Core.Resources.Speedway.SecretStore;

namespace Speedway.Deploy.Core
{
    internal class ManifestDeployer : IManifestDeployer
    {
        private readonly ISpeedwayResourceFactory _speedwayResourceFactory;
        private readonly ILogger<ManifestDeployer> _logger;

        public ManifestDeployer(
            ISpeedwayResourceFactory speedwayResourceFactory,
            ILogger<ManifestDeployer> logger)
        {
            _speedwayResourceFactory = speedwayResourceFactory;
            _logger = logger;
        }

        public async Task Deploy(
            string environment,
            SpeedwayManifest manifest,
            ZipArchive? zipArchive,
            Func<IDictionary<ISpeedwayResource, SpeedwayResourceOutputMetadata>, Task>? postLinkCallback)
        {
            var context = _speedwayResourceFactory.BuildContext(manifest, environment);

            var allComponents = BuildAllComponentsIncludingDefaultKeyVault(manifest, context);
            _logger.LogInformation("Beginning Manifest deployment");
            var outputMetadata = await ReflectAllComponentsOnPlatform(context, allComponents);
            await LinkComponents(allComponents);
            if (postLinkCallback != null) await postLinkCallback!(outputMetadata);
            await PostProcessComponents(outputMetadata);

            await DeployArtifacts(allComponents, zipArchive);
        }

        /// <summary>
        /// Build metadata for a new secret store called Default. This is a special one that all APIs in the group can read from.
        /// </summary>
        /// <param name="manifest"></param>
        /// <returns></returns>
        private static SpeedwaySecretContainerResourceMetadata FindOrBuildDefaultSecretStoreMetadata(
            SpeedwayManifest manifest)
        {
            var preDefinedSecrets = manifest.Resources.SingleOrDefault(x =>
                x is SpeedwaySecretContainerResourceMetadata &&
                x.Name == "Default");

            return (SpeedwaySecretContainerResourceMetadata) 
                (preDefinedSecrets 
                 ?? new SpeedwaySecretContainerResourceMetadata(SpeedwaySecretContainerResource.DefaultName, null));
        }

        private async Task DeployArtifacts(ISpeedwayResource[] allComponents, ZipArchive? zipArchive)
        {
            if (zipArchive == null)
            {
                _logger.LogWarning("No zip archive was passed. No components will be deployed");
                return;
            }

            foreach (var resource in allComponents.OfType<ISupportDeployingArtifacts>())
            {
                await resource.Deploy(zipArchive!);
            }
        }

        private ISpeedwayResource[] BuildAllComponentsIncludingDefaultKeyVault(SpeedwayManifest manifest,
            SpeedwayContextResource context)
        {
            var defaultSecretStoreComponent = FindOrBuildDefaultSecretStoreMetadata(manifest);
            
            var defaultSecretStore = (SpeedwaySecretContainerResource) _speedwayResourceFactory.Build(context, defaultSecretStoreComponent);
            
            var requiredComponents =
                manifest.Resources
                    .Except(new [] {defaultSecretStoreComponent})
                    .Select(x => _speedwayResourceFactory.Build(context, x))
                    .Union(new[] {defaultSecretStore}).ToArray();
            
            return requiredComponents;
        }

        private async Task PostProcessComponents(
            IDictionary<ISpeedwayResource, SpeedwayResourceOutputMetadata> requiredComponents)
        {
            var defaultKv = await EnsureAllSecretsAreSetupBeforePostProcessing(requiredComponents);
            foreach (var component in requiredComponents.Keys.Except(new[] {defaultKv}))
            {
                await component.PostProcess(requiredComponents);
            }
        }

        /// <summary>
        /// Ensure any secrets are setup first. Then we can get the URI's to reference them in configuration
        /// </summary>
        /// <param name="requiredComponents"></param>
        /// <returns></returns>
        private static async Task<ISpeedwayResource> EnsureAllSecretsAreSetupBeforePostProcessing(
            IDictionary<ISpeedwayResource, SpeedwayResourceOutputMetadata> requiredComponents)
        {
            var defaultKv = requiredComponents.Keys.Single(x =>
                x is SpeedwaySecretContainerResource && x.Name == SpeedwaySecretContainerResource.DefaultName);
            await defaultKv.PostProcess(requiredComponents);
            return defaultKv;
        }

        private async Task LinkComponents(ISpeedwayResource[] allComponents)
        {
            foreach (var componentThatCanBeLinkedTo in allComponents.OfType<ISpeedwayResourceThatSupportsLinkingTo>()
                .Where(x => x.Links.Any()))
            {
                _logger.LogInformation("Linking components to {Component}", componentThatCanBeLinkedTo.Name);
                await ConfigureAccessBetweenPlatformComponents(allComponents, componentThatCanBeLinkedTo);
                _logger.LogInformation("Linked components to {Component}", componentThatCanBeLinkedTo.Name);
            }
        }

        private async Task ConfigureAccessBetweenPlatformComponents(ISpeedwayResource[] allComponents,
            ISpeedwayResourceThatSupportsLinkingTo componentThatSupportsLinkingTo)
        {
            foreach (var link in componentThatSupportsLinkingTo.Links)
            {
                _logger.LogInformation("Assigning access to {Component} type {Type}", link.Name, link.GetType().Name);
                var toLinkTo = allComponents.SingleOrDefault(x => x.Name == link.Name);
                if (toLinkTo == null)
                {
                    _logger.LogWarning("Could not find component to link to - {Component} type {Type}", link.Name,
                        link.GetType().Name);
                }
                else
                {
                    await componentThatSupportsLinkingTo.GrantAccessTo(toLinkTo, link);
                }
            }
        }

        private static async Task<IDictionary<ISpeedwayResource, SpeedwayResourceOutputMetadata>>
            ReflectAllComponentsOnPlatform(SpeedwayContextResource context, ISpeedwayResource[] requiredComponents)
        {
            var outputMetadata = new Dictionary<ISpeedwayResource, SpeedwayResourceOutputMetadata>()
            {
                {context, await context.ReflectOnPlatform()}
            };
            foreach (var component in requiredComponents)
            {
                outputMetadata[component] = await component.ReflectOnPlatform();
            }

            return outputMetadata;
        }
    }
}
