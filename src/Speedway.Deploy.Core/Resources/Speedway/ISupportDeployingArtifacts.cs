using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace Speedway.Deploy.Core.Resources.Speedway
{
    public interface ISupportDeployingArtifacts
    {
        Task Deploy(ZipArchive zipArchive);
    }
}