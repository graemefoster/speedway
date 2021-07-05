using Microsoft.AspNetCore.Authentication.AzureAD.UI;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Extensions.Options;

namespace Speedway.Api.Extensions
{
    public class MsiAzureCredentialProvider : IAzureCredentialProvider
    {
        private readonly IOptions<AzureADOptions> _settings;

        public MsiAzureCredentialProvider(IOptions<AzureADOptions> settings)
        {
            _settings = settings;
        }
        
        public AzureCredentials Provide()
        {
            return SdkContext.AzureCredentialsFactory.FromSystemAssignedManagedServiceIdentity(
                MSIResourceType.AppService, AzureEnvironment.AzureGlobalCloud, _settings.Value.TenantId);
        }
    }
}