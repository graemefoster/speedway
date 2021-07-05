using System.Threading.Tasks;
using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Graph;
using Speedway.Core.Resources;

namespace Speedway.Deploy.Provider.AzureAppService
{
    public interface IAzureApiManagement
    {
        Task CreateApi(IWebApp webApp);
        Task ImportApi(IWebApp webApp, SpeedwayWebApiResourceMetadata metadata, Application azureAdApplicationRepresentingApi);
        IGenericResource ApimResource { get; }

        /// <summary>
        /// This needs a lot of thought - at the moment it uses the primary key from the subscription.
        /// In the real world this is probably going to be managed outside of speedway 
        /// </summary>
        /// <returns></returns>
        Task<string> GetKey();
    }
}