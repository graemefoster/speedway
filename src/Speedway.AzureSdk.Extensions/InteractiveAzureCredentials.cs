using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Identity.Client;
using Microsoft.Rest;
using Microsoft.Rest.Azure.Authentication;

namespace Speedway.AzureSdk.Extensions
{
    public class InteractiveAzureCredentials : AzureCredentials
    {
        public JwtSecurityToken IdToken { get; set; }

        private readonly IPublicClientApplication _publicClientApplication;

        private readonly IDictionary<Uri, ServiceClientCredentials> _credentialsCache =
            new Dictionary<Uri, ServiceClientCredentials>();

        public InteractiveAzureCredentials(
            IPublicClientApplication publicClientApplication, string tenantId) : base(
            new DeviceCredentialInformation(),
            tenantId,
            AzureEnvironment.AzureGlobalCloud)
        {
            _publicClientApplication = publicClientApplication;
        }

        public override async Task ProcessHttpRequestAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
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
            var isKv = false;
            string host = request.RequestUri.Host;
            if (url.StartsWith(Environment.GraphEndpoint, StringComparison.OrdinalIgnoreCase))
            {
                isGraph = true;
                adSettings.TokenAudience = new Uri(Environment.GraphEndpoint);
            }
            else if (host.EndsWith(Environment.KeyVaultSuffix, StringComparison.OrdinalIgnoreCase))
            {
                isKv = true;
                adSettings.TokenAudience = new Uri($"https://{Environment.KeyVaultSuffix}");
            }

            if (!_credentialsCache.ContainsKey(adSettings.TokenAudience))
            {
                var token = await GetToken(
                    new[]
                    {
                        isGraph ? "https://graph.windows.net/.default" :
                        isKv ? AadScopes.AzureKeyVaultImpersonation : AadScopes.AzureResourceManagerImpersonation,
                    });

                _credentialsCache.Add(adSettings.TokenAudience, new TokenCredentials(token));
            }

            await _credentialsCache[adSettings.TokenAudience].ProcessHttpRequestAsync(request, cancellationToken);
        }

        public async Task<string> GetToken(string[] scopes)
        {
            var accounts = _publicClientApplication.GetAccountsAsync().Result;
            AuthenticationResult result;
            try
            {
                result = await _publicClientApplication
                    .AcquireTokenSilent(scopes, accounts.FirstOrDefault())
                    .ExecuteAsync();
            }
            catch (MsalUiRequiredException)
            {
                result = await _publicClientApplication.AcquireTokenInteractive(scopes)
                    .ExecuteAsync();
            }

            IdToken = new JwtSecurityTokenHandler().ReadJwtToken(result.IdToken);
            return result.AccessToken;
        }
    }
}