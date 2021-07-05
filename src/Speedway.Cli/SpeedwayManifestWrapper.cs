using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Speedway.Core;
using Speedway.Core.Resources;

namespace Speedway.Cli
{
    public class SpeedwayManifestWrapper
    {
        private readonly string _manifestPath;
        private readonly string _speedwayRoot;
        private readonly SpeedwayManifest _file;

        private SpeedwayManifestWrapper(string path)
        {
            _manifestPath = path;
            _speedwayRoot = Directory.GetParent(path)!.Parent!.FullName;
            _file = SpeedwayManifest.DeserializeFromJson(File.ReadAllText(path));
        }

        public string[] Applications
        {
            get
            {
                return _file.Resources.Where(x => x is SpeedwayWebAppResourceMetadata)
                    .Select(x => x.Name).ToArray();
            }
        }

        public string[] Storage
        {
            get
            {
                return _file.Resources.OfType<SpeedwayStorageResourceMetadata>()
                    .Select(x => x.Name).ToArray();
            }
        }

        public IEnumerable<string> OAuthClients         {
            get
            {
                return _file.Resources.Where(x => x is SpeedwayOAuthClientResourceMetadata)
                    .Select(x => x.Name).ToArray();
            }
        }

        public SpeedwayManifest RawManifest => _file;

        public static SpeedwayManifestWrapper Find()
        {
            var currentDir = new DirectoryInfo(Directory.GetCurrentDirectory());
            do
            {
                var speedwayManifestFolder = Path.Combine(currentDir.FullName, ".speedway");
                if (Directory.Exists(speedwayManifestFolder))
                {
                    var speedwayManifestFile = Path.Combine(speedwayManifestFolder, "manifest.json");
                    if (File.Exists(speedwayManifestFile))
                    {
                        return new SpeedwayManifestWrapper(Path.Combine(currentDir.FullName, ".speedway",
                            "manifest.json"));
                    }

                    throw new InvalidOperationException("Cannot find a manifest file inside the speedway folder");
                }

                currentDir = currentDir.Parent;
            } while (currentDir != null);

            throw new InvalidOperationException("Cannot find a manifest file inside the current folder hierarchy");
        }

        public Task NewComponent(ISpeedwayManifestNewComponentHandler handler)
        {
            return handler.CreateComponent(_speedwayRoot);
        }

        public T AddResource<T>(T resource) where T: SpeedwayResourceMetadata
        {
            _file.Resources.Add(resource);
            return resource;
        }

        public void Save()
        {
            File.WriteAllText(_manifestPath,  _file.SerializeToJson());
        }

        public T? FindResource<T>(string name) where T: SpeedwayResourceMetadata
        {
            return _file.Resources.OfType<T>().SingleOrDefault(x => x.Name == name);
        }

        public T ReplaceResource<T>(T resource, T replacementResource) where T:SpeedwayResourceMetadata
        {
            var index = _file.Resources.IndexOf(resource);
            _file.Resources.Remove(resource);
            _file.Resources.Insert(index, replacementResource);
            return replacementResource;
        }

        /// <summary>
        /// Writes a mermaid readme.md file to the repository
        /// </summary>
        /// <param name="mermaid"></param>
        public void WriteMermaid(string mermaid)
        {
            File.WriteAllText(Path.Combine(_speedwayRoot, "mermaid.md"), mermaid);
        }
    }
}