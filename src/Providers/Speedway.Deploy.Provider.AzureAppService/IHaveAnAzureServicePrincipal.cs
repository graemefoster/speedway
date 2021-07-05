using System.Threading.Tasks;
using Microsoft.Graph;

namespace Speedway.Deploy.Provider.AzureAppService
{
    internal interface IHaveAnAzureServicePrincipal
    {
        Task<ServicePrincipal> ServicePrincipal { get; }
    }
}