using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Speedway.Core.Resources;
using Speedway.Deploy.Core.Resources.Speedway.Context;

namespace Speedway.Deploy.Core.Resources.Speedway.SecretStore
{
    /// <summary>
    /// Provides secret storage and access 
    /// </summary>
    // ReSharper disable once ClassNeverInstantiated.Global
    public class SpeedwaySecretContainerResource : SpeedwayResource<SpeedwaySecretContainerResourceMetadata>, ISpeedwayResourceThatSupportsLinkingTo
    {
        public static string DefaultName = "Default";
        
        public SpeedwaySecretContainerResource(
            SpeedwayContextResource context,
            SpeedwaySecretContainerResourceMetadata metadata, 
            ISpeedwayPlatformTwinFactory platformTwinFactory): 
            base(context, metadata, platformTwinFactory)
        {
        }

        /// <summary>
        /// Get all the secrets asked for by the resources and store them
        /// </summary>
        /// <param name="metadata"></param>
        /// <returns></returns>
        public override async Task PostProcess(IDictionary<ISpeedwayResource, SpeedwayResourceOutputMetadata> metadata)
        {
            var secrets = metadata.SelectMany(x =>
                    x.Value.KnownSecrets.Select(s =>
                        new KeyValuePair<string, string>(BuildSecretName(x.Key, s.Key), s.Value)))
                .ToDictionary(x => x.Key, x => x.Value);

            var externallySetRequiredSecrets = new HashSet<string>(
                metadata.SelectMany(x =>
                    x.Value.RequiredSecrets.Select(s => BuildSecretName(x.Key, s))));
            
            await base.GetPlatformTwin<ISecretStorePlatformTwin>().StoreKnownSecrets(secrets, externallySetRequiredSecrets);
            await base.PostProcess(metadata);
        }

        private static string BuildSecretName(ISpeedwayResource resource, string secretName)
        {
            return $"{resource.Name}-{secretName}";
        }

        public string GetSecretUri(ISpeedwayResource declaringResource, string secretName)
        {
            return GetPlatformTwin<ISecretStorePlatformTwin>().GetSecretUri(BuildSecretName(declaringResource, secretName));
        }

        public string GetPredefinedSecretUri(string secretName)
        {
            return GetPlatformTwin<ISecretStorePlatformTwin>().GetSecretUri(secretName);
        }

        public Task GrantAccessTo(ISpeedwayResource resource, SpeedwayResourceLinkMetadata link)
        {
            var secretsLink = link as SecretsLink;
            if (secretsLink == null)
                throw new InvalidOperationException($"Attempt to link a secret resource using a {link.GetType().Name}. Secret Resources only support SecretsLink");
            return GetPlatformTwin<ISecretStorePlatformTwin>().GrantAccessTo(resource, secretsLink);
        }
    }
}