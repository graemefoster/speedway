using System;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Extensions.Logging;
using Speedway.Core.Resources;
using Speedway.Deploy.Core;
using Speedway.Deploy.Core.Resources.Speedway.WebApp;

namespace Speedway.Deploy.Provider.AzureAppService.WebApp
{
    /// <summary>
    /// Uses the zipdeploy endpoint on an app-service to deploy.
    /// </summary>
    /// <typeparam name="TMetadata"></typeparam>
    internal class AppServiceWebAppArtifactDeployer<TMetadata> where TMetadata : SpeedwayWebAppResourceMetadata
    {
        private readonly ILogger<AppServiceWebAppArtifactDeployer<TMetadata>> _logger;
        private readonly DeploymentHelper _deploymentHelper;

        public AppServiceWebAppArtifactDeployer(ILogger<AppServiceWebAppArtifactDeployer<TMetadata>> logger, DeploymentHelper deploymentHelper)
        {
            _logger = logger;
            _deploymentHelper = deploymentHelper;
        }

        public async Task DeployBinariesToWebApp(IWebApp webApp, ZipArchive zipArchive, SpeedwayWebLikeResource<TMetadata> resource)
        {
            _logger.LogInformation("Deploying artifact to website {Website}", webApp!.Id);
            var publishProfile = await webApp.GetPublishingProfileAsync();
            var scm = webApp.EnabledHostNames.Single(x => x.Contains("scm"));

            // ReSharper disable once StringLiteralTypo
            var scmZipDeployUri = $"https://{scm}/api/zipdeploy";

            _logger.LogInformation("Beginning Zip deploy to scm Uri {Uri}", scmZipDeployUri);

            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(
                    Encoding.UTF8.GetBytes($"{publishProfile.GitUsername}:{publishProfile.GitPassword}")));

            await _deploymentHelper.CreateZipForArtifact(zipArchive, resource.Name,
                async binariesStream =>
                {
                    var response = await client.PostAsync(scmZipDeployUri, new StreamContent(binariesStream));

                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("Deployed zip to scm Uri {Uri}", scmZipDeployUri);
                    }
                    else
                    {
                        _logger.LogError("Failed to deploy zip to Uri {Uri}", scmZipDeployUri);
                    }
                });
        }
    }
}