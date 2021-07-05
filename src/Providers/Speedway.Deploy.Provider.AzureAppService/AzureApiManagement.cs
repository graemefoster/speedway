using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Speedway.Core.Resources;

namespace Speedway.Deploy.Provider.AzureAppService
{
    internal class AzureApiManagement : IAzureApiManagement
    {
        private readonly IAzure _azure;
        public IGenericResource ApimResource { get; }

        private readonly ILogger<AzureApiManagement> _logger;

        public AzureApiManagement(
            IAzure azure, 
            IGenericResource apimResource, 
            ILogger<AzureApiManagement> logger)
        {
            _azure = azure;
            ApimResource = apimResource;
            _logger = logger;
        }
        
        public async Task CreateApi(IWebApp webApp)
        {
            await CreateBackendToRepresentAppService(webApp);
        }

        private async Task CreateBackendToRepresentAppService(IWebApp webApp)
        {
            var restCall = new
            {
                properties = new
                {
                    description = $"Backend representing {webApp.Name}",
                    url = $"https://{webApp.HostNames.First()}",
                    protocol = "http",
                    tls = new
                    {
                        validateCertificateChain = true,
                        validateCertificateName = true
                    }
                }
            };

            var response = await CallApimManagementEndpoint($"backends/{webApp.Name}", restCall);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Created backend representing {AppName}",  webApp.Name);
            }
        }

        public async Task ImportApi(IWebApp webApp, SpeedwayWebApiResourceMetadata metadata, Application azureAdApplicationRepresentingApi)
        {
            await CreateOrUpdateApiFromSwagger(metadata.Name, new Uri($"https://{webApp.HostNames.First()}"), metadata.Swagger);
            await AttachPolicyToApi(metadata.Name, webApp.Name, $"api://{azureAdApplicationRepresentingApi.AppId}");
        }

        /// <summary>
        /// Assert incoming jwt against expected audience for the web-app
        /// </summary>
        /// <param name="webApp"></param>
        /// <param name="apiName"></param>
        /// <param name="backendName"></param>
        /// <returns></returns>
        private async Task AttachPolicyToApi(string apiName, string backendName, string expectedAudienceClaim)
        {
            var restCall = new
            {
                properties = new
                {
                    format = "xml",
                    value = @$"
<policies>
    <inbound>
        <set-backend-service backend-id=""{backendName}"" />
        <validate-jwt header-name=""Authorization"" failed-validation-httpcode=""401"" failed-validation-error-message=""Unauthorized. Access token is missing or invalid."">
            <openid-config url=""https://login.microsoftonline.com/{_azure.GetCurrentSubscription().Inner.TenantId}/.well-known/openid-configuration"" />
            <required-claims>
                <claim name=""aud"">
                    <value>{expectedAudienceClaim}</value>
                </claim>
            </required-claims>
        </validate-jwt>
    </inbound>
    <backend>    
        <forward-request />  
    </backend>
    <outbound />
</policies>"
                }
            };

            var response = await CallApimManagementEndpoint($"apis/{apiName}/policies/policy", restCall);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Imported API representing {AppName} at path {Path}", apiName, apiName);
            }
        }

        private async Task CreateOrUpdateApiFromSwagger(string apiName, Uri baseUri, string swaggerUri)
        {
            var restCall = new
            {
                properties = new
                {
                    format = "openapi-link",
                    value = new Uri(baseUri + swaggerUri),
                    path = apiName,
                }
            };

            var response = await CallApimManagementEndpoint($"apis/{apiName}", restCall);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Imported API representing {AppName}", apiName);
            }
        }
        
        public async Task<string> GetKey()
        {
            var response = await CallApimManagementEndpoint($"subscriptions/master/listSecrets", new {}, HttpMethod.Post);
            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<JObject>(await response.Content.ReadAsStringAsync()).Value<string>("primaryKey");
            }

            throw new InvalidOperationException("Failed to retrieve api-management secrets");
        }

        private async Task<HttpResponseMessage> CallApimManagementEndpoint(string relativeManagementPath, object restCall, HttpMethod? method = null)
        {
            var managementUri = ApimResource.Manager.Inner.BaseUri.AbsoluteUri + ApimResource.Id;
            var httpRequest =
                new HttpRequestMessage(method ?? HttpMethod.Put, $"{managementUri}/{relativeManagementPath}?api-version=2020-06-01-preview")
                {
                    Content = new StringContent(JsonConvert.SerializeObject(restCall), Encoding.UTF8,
                        "application/json")
                };

            await _azure.GenericResources.Manager.Inner.Credentials.ProcessHttpRequestAsync(httpRequest,
                CancellationToken.None);
            var client = new HttpClient();
            return await client.SendAsync(httpRequest);
        }
    }
}