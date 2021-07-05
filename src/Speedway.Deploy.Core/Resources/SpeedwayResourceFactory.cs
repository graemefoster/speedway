using System;
using Microsoft.Extensions.DependencyInjection;
using Speedway.Core;
using Speedway.Core.Resources;
using Speedway.Deploy.Core.Resources.Speedway;
using Speedway.Deploy.Core.Resources.Speedway.Context;
using Speedway.Deploy.Core.Resources.Speedway.ExistingOAuthClient;
using Speedway.Deploy.Core.Resources.Speedway.NoSql;
using Speedway.Deploy.Core.Resources.Speedway.OAuthClient;
using Speedway.Deploy.Core.Resources.Speedway.SecretStore;
using Speedway.Deploy.Core.Resources.Speedway.Storage;
using Speedway.Deploy.Core.Resources.Speedway.WebApi;
using Speedway.Deploy.Core.Resources.Speedway.WebApp;

namespace Speedway.Deploy.Core.Resources
{
    internal class SpeedwayResourceFactory : ISpeedwayResourceFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public SpeedwayResourceFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public ISpeedwayResource Build(SpeedwayContextResource context, SpeedwayResourceMetadata metadata)
        {
            return metadata switch
            {
                SpeedwayOAuthClientResourceMetadata typedMetadata => BuildResource<SpeedwayOAuthClientResource, SpeedwayOAuthClientResourceMetadata>(context, typedMetadata),
                SpeedwayExistingOAuthClientResourceMetadata typedMetadata => BuildResource<SpeedwayExistingOAuthClientResource, SpeedwayExistingOAuthClientResourceMetadata>(context, typedMetadata),
                SpeedwayWebApiResourceMetadata typedMetadata => BuildResource<SpeedwayWebApiResource, SpeedwayWebApiResourceMetadata>(context, typedMetadata),
                SpeedwayWebAppResourceMetadata typedMetadata => BuildResource<SpeedwayWebAppResource, SpeedwayWebAppResourceMetadata>(context, typedMetadata),
                SpeedwayStorageResourceMetadata typedMetadata => BuildResource<SpeedwayStorageResource, SpeedwayStorageResourceMetadata>(context, typedMetadata),
                SpeedwaySecretContainerResourceMetadata typedMetadata => BuildResource<SpeedwaySecretContainerResource, SpeedwaySecretContainerResourceMetadata>(context, typedMetadata),
                SpeedwayNoSqlResourceMetadata typedMetadata => BuildResource<SpeedwayNoSqlResource, SpeedwayNoSqlResourceMetadata>(context, typedMetadata),
                _ => throw new ArgumentException($"Unknown metadata type: {metadata.GetType().FullName}", nameof(metadata))
            };
        }

        private ISpeedwayResource BuildResource<T, TMetadata>(SpeedwayContextResource context, TMetadata resourceMetadata) where T: SpeedwayResource<TMetadata> where TMetadata : SpeedwayResourceMetadata
        {
            return ActivatorUtilities.CreateInstance<T>(
                _serviceProvider,
                context,
                resourceMetadata);
        }

        public SpeedwayContextResource BuildContext(SpeedwayManifest manifest, string environment)
        {
            return ActivatorUtilities.CreateInstance<SpeedwayContextResource>(_serviceProvider, manifest, environment);
        }
    }
}