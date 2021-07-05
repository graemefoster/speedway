using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;

namespace Speedway.Cli
{
    public class SpeedwayClientTokenProvider
    {
        private readonly IConfiguration _configuration;
        private readonly IOptions<SpeedwayApiSettings> _speedwayApiSettings;

        public SpeedwayClientTokenProvider(
            IConfiguration configuration,
            IOptions<SpeedwayApiSettings> speedwayApiSettings)
        {
            _configuration = configuration;
            _speedwayApiSettings = speedwayApiSettings;
        }

        public async Task<string> GetToken()
        {
            var scopes = new[] {$"api://{_speedwayApiSettings.Value.ClientId}/.default" };

            var app = PublicClientApplicationBuilder
                .Create(_configuration.GetValue<string>("AzureAd:ClientId"))
                .WithRedirectUri("http://localhost/")
                .WithTenantId(_configuration.GetValue<string>("AzureAd:TenantId"))
                .Build();

            var accounts = app.GetAccountsAsync().Result;
            AuthenticationResult result;
            try
            {
                result = await app
                    .AcquireTokenSilent(scopes, accounts.FirstOrDefault())
                    .ExecuteAsync();
            }
            catch (MsalUiRequiredException)
            {
                result = await app.AcquireTokenInteractive(scopes)
                    .ExecuteAsync();
            }

            return result.AccessToken;
        }
    }
}