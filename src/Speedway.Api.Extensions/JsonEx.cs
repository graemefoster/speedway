using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Speedway.Api.Extensions
{
    public static class JsonEx
    {
        public static T ToObject<T>(this JsonElement element)
        {
            var json = element.GetRawText();
            var response = JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            if (response == null) throw new ArgumentException($"Cannot deserialize json content into type {typeof(T).FullName}");
            return response;
        }

        public static T ToObject<T>(this JsonDocument document)
        {
            var json = document.RootElement.GetRawText();
            var response = JsonSerializer.Deserialize<T>(json);
            if (response == null) throw new ArgumentException($"Cannot deserialize json content into type {typeof(T).FullName}");
            return response;
        }
        
        public static async Task<T> ReadAsAsync<T>(this HttpContent content)
        {
            var responseContent = await content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(responseContent);
        }
    }
}