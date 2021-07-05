using Microsoft.AspNetCore.Authentication.AzureAD.UI;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Extensions.Options;

namespace Speedway.Api.Extensions
{
    /// <summary>
    /// TODO - use Keyvault to get these in Azure.
    /// </summary>
    public class LocalAzureCredentialProvider : IAzureCredentialProvider
    {
        private readonly IOptions<AzureADOptions> _settings;

        public LocalAzureCredentialProvider(IOptions<AzureADOptions> settings)
        {
            _settings = settings;
        }
        
        public AzureCredentials Provide()
        {
            return SdkContext.AzureCredentialsFactory.FromServicePrincipal(
                _settings.Value.ClientId,
                _settings.Value.ClientSecret,
                _settings.Value.TenantId,
                AzureEnvironment.AzureGlobalCloud);
        }
    }
}