using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Identity.Web;

namespace Speedway.PipelineBuilderApi
{
    public static class HttpEx
    {

        public static async Task<T> GetForUserSafeAsync<T>(
            this IDownstreamWebApi downstreamWebApi,
            string serviceName,
            string relativePath,
            Action<DownstreamWebApiOptions>? downstreamWebApiOptionsOverride = null,
            ClaimsPrincipal? user = null) where T : class
        {
            var result = await downstreamWebApi.GetForUserAsync<T>(
                serviceName,
                relativePath,
                downstreamWebApiOptionsOverride,
                user);

            if (result == null) throw new InvalidOperationException("Failed to find projects in devops");
            return result;
        }
        
    }
}