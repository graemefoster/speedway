using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Speedway.AzureSdk.Extensions;
using Speedway.Core.Resources;
using Speedway.Deploy.Core.Resources.Speedway;
using Speedway.Deploy.Core.Resources.Speedway.ExistingOAuthClient;

namespace Speedway.Deploy.Provider.AzureAppService.ExistingOAuthClient
{
    // ReSharper disable once ClassNeverInstantiated.Global
    internal class ExistingAzureActiveDirectoryApplicationServicePrincipalPlatformTwin : IExistingOAuthClientPlatformTwin, IHaveAnAzureServicePrincipal
    {
        private readonly ILogger _logger;
        private readonly IGraphServiceClient _graphClient;
        private readonly SpeedwayExistingOAuthClientResource _resource;
        private ServicePrincipal? _servicePrincipal;

        public ExistingAzureActiveDirectoryApplicationServicePrincipalPlatformTwin(
            ILogger<ExistingAzureActiveDirectoryApplicationServicePrincipalPlatformTwin> logger,
            IGraphServiceClient graphClient,
            SpeedwayExistingOAuthClientResource resource)
        {
            _logger = logger;
            _graphClient = graphClient;
            _resource = resource;
        }

        public async Task<SpeedwayResourceOutputMetadata> Reflect()
        {
            _servicePrincipal = await _graphClient.GetApplicationServicePrincipalSafe(_resource.ResourceMetadata.ApplicationId);
            _logger.LogInformation("Found existing service principal {Id} with name {Name}", _servicePrincipal.Id, _servicePrincipal.DisplayName);
            return new SpeedwayResourceOutputMetadata(_servicePrincipal!.DisplayName, new Dictionary<string, string>(), new HashSet<string>());
        }

        public Task<ServicePrincipal> ServicePrincipal => Task.FromResult(_servicePrincipal!);
        
        public async Task RequestApplicationAccess(string[] roles, ISpeedwayResource allowedApplication)
        {
            var otherServicePrincipal = await allowedApplication.GetPlatformTwin<IHaveAnAzureServicePrincipal>().ServicePrincipal;
            await _graphClient.ApplyRoles(await ServicePrincipal, otherServicePrincipal, roles);
        }

        public async Task RequestDelegatedAccess(string[] scopes, ISpeedwayResource applicationThatWantsToDelegateThis)
        {
            var otherServicePrincipal = await applicationThatWantsToDelegateThis.GetPlatformTwin<IHaveAnAzureServicePrincipal>().ServicePrincipal;
            await _graphClient.ApplyScopes(
                await ServicePrincipal,
                otherServicePrincipal,
                scopes);
        }
    }
}