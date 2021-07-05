using System.Threading.Tasks;
using Microsoft.Azure.Management.AppService.Fluent;

namespace Speedway.Deploy.Provider.AzureAppService
{
    public interface IWellKnownPlatformComponentProvider
    {
        Task<IAppServicePlan> GetDefaultAppServicePlan();
        Task<IAzureApiManagement> GetApiManagement();
        Task<string> GetLogAnalyticsWorkspaceResourceId();
    }
}