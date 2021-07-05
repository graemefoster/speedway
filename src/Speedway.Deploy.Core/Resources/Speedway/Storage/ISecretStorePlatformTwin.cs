using System.Threading.Tasks;
using Speedway.Core.Resources;

namespace Speedway.Deploy.Core.Resources.Speedway.Storage
{
    public interface IStoragePlatformTwin
    {
        Task GrantAccessTo(ISpeedwayResource resource, StorageLink access);
    }
}