using System.Threading.Tasks;
using Speedway.Core.Resources;

namespace Speedway.Deploy.Core.Resources.Speedway
{
    public interface ISpeedwayResourceThatSupportsLinkingTo 
    {
        SpeedwayResourceLinkMetadata[] Links { get; }
        string Name { get; }
        Task GrantAccessTo(ISpeedwayResource wantsAccess, SpeedwayResourceLinkMetadata linkInformation);
    }
}