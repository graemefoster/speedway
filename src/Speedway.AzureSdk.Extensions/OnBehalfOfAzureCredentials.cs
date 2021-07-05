using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Identity.Web;
using Microsoft.Rest;
using Microsoft.Rest.Azure.Authentication;

namespace Speedway.AzureSdk.Extensions
{    
    public class OnBehalfOfAzureCredentials : AzureCredentials
    {
        private readonly ITokenAcquisition _tokenAcquisition;

        private readonly IDictionary<Uri, ServiceClientCredentials> _credentialsCache =
            new Dictionary<Uri, ServiceClientCredentials>();

        public OnBehalfOfAzureCredentials(
            ITokenAcquisition tokenAcquisition,
            ServicePrincipalLoginInformation servicePrincipalLoginInformation,
            string tenantId,
            AzureEnvironment environment) : base(servicePrincipalLoginInformation, tenantId, environment)
        {
            _tokenAcquisition = tokenAcquisition;
        }

        public override async Task ProcessHttpRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri == null) throw new InvalidOperationException("Null Uri on request");
            
            var adSettings = new ActiveDirectoryServiceSettings
            {
                AuthenticationEndpoint = new Uri(Environment.AuthenticationEndpoint),
                TokenAudience = new Uri(Environment.ManagementEndpoint),
                ValidateAuthority = true
            };
            var url = request.RequestUri.ToString();

            var isGraph = false;
            if (url.StartsWith(Environment.GraphEndpoint, StringComparison.OrdinalIgnoreCase))
            {
                isGraph = true;
                adSettings.TokenAudience = new Uri(Environment.GraphEndpoint);
            }

            if (!_credentialsCache.ContainsKey(adSettings.TokenAudience))
            {
                var token =  await _tokenAcquisition.GetAccessTokenForUserAsync(
                    new[]
                    {
                        isGraph ? "https://graph.windows.net/.default" : AadScopes.AzureResourceManagerImpersonation,
                    });

                _credentialsCache.Add(adSettings.TokenAudience, new TokenCredentials(token));
            }

            await _credentialsCache[adSettings.TokenAudience].ProcessHttpRequestAsync(request, cancellationToken);
        }
    }
}