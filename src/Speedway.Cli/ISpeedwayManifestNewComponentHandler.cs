using System.Threading.Tasks;

namespace Speedway.Cli
{
    public interface ISpeedwayManifestNewComponentHandler
    {
        Task CreateComponent(string speedwayRootPath);
    }
}