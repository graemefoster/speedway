using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Speedway.Core;
using Speedway.Deploy.Core;

namespace Speedway.Deploy.Features
{
    public class ManifestDeployment
    {
        public record DeployRequest(Guid SpeedwayProjectId, string BuildId, string Environment) : IRequest<DeployResponse>;
        public record DeployResponse(Guid ManifestId);

        public class Validator : AbstractValidator<DeployRequest>
        {
            public Validator(IValidator<SpeedwayManifest> manifestValidator)
            {
            }
        }

        /// <summary>
        /// This is where it's at :)
        /// </summary>
        class AzureSpeedwayManifestResourceFactory : IRequestHandler<DeployRequest, DeployResponse>
        {
            private readonly BlobServiceClient _blobServiceClient;
            private readonly IOptions<AzureSpeedwaySettings> _settings;
            private readonly ILogger<AzureSpeedwayManifestResourceFactory> _logger;
            private readonly IManifestDeployer _manifestDeployer;

            public AzureSpeedwayManifestResourceFactory(
                BlobServiceClient blobServiceClient, //can't see an interface :(
                IOptions<AzureSpeedwaySettings> settings,
                ILogger<AzureSpeedwayManifestResourceFactory> logger,
                IManifestDeployer manifestDeployer)
            {
                _blobServiceClient = blobServiceClient;
                _settings = settings;
                _logger = logger;
                _manifestDeployer = manifestDeployer;
            }

            public async Task<DeployResponse> Handle(DeployRequest request, CancellationToken cancellationToken)
            {
                _logger.LogInformation("Deployer is downloading blob for project {project}, build: {build}",
                    request.SpeedwayProjectId, request.BuildId);

                var container = _blobServiceClient.GetBlobContainerClient(_settings.Value.ArtifactBlobContainerName)
                    .GetBlobClient($"{request.SpeedwayProjectId}/{request.BuildId}/build.zip");

                var archive = container.Download(cancellationToken);

                using var zipStream = new ZipArchive(archive.Value.Content);
                var entry = zipStream.GetEntry(".speedway/manifest.json")!;

                await using var contentStream = entry.Open();
                using var contents = new StreamReader(contentStream);

                var manifest = SpeedwayManifest.DeserializeFromJson(await contents.ReadToEndAsync());

                await _manifestDeployer.Deploy(request.Environment, manifest, zipStream, null);

                return new DeployResponse(manifest.Id);
            }
        }
    }
}