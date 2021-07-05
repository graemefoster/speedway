using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;

namespace Speedway.Api.Extensions
{
    public interface IAzureCredentialProvider
    {
        AzureCredentials Provide();
    }
}