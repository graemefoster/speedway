using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Speedway.Cli
{
    public class SpeedwayApiClient
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly SpeedwayClientTokenProvider _tokenProvider;

        public SpeedwayApiClient(IHttpClientFactory httpClientFactory, SpeedwayClientTokenProvider tokenProvider)
        {
            _httpClientFactory = httpClientFactory;
            _tokenProvider = tokenProvider;
        }

        public async Task<HttpResponseMessage> Send(HttpRequestMessage request, params string[] requiredScopes)
        {
            var token = await _tokenProvider.GetToken();
            var client = _httpClientFactory.CreateClient("speedwayApi");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return await client.SendAsync(request);
        }
    }
}