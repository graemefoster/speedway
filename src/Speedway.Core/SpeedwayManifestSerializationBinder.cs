using System;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Serialization;
using Speedway.Core.Resources;

namespace Speedway.Core
{
    public class SpeedwayManifestSerializationBinder : ISerializationBinder
    {
        private readonly Type[] _knownTypes = typeof(SpeedwayResourceMetadata).Assembly
            .GetTypes()
            .Where(x => x.GetCustomAttribute<SpeedwayResourceAttribute>() != null && !x.IsAbstract)
            .ToArray();

        public Type BindToType(string? assemblyName, string typeName)
        {
            return _knownTypes.SingleOrDefault(t => t.GetCustomAttribute<SpeedwayResourceAttribute>()!.Type == typeName)!;
        }

        public void BindToName(Type serializedType, out string? assemblyName, out string typeName)
        {
            assemblyName = null;
            typeName = serializedType.GetCustomAttribute<SpeedwayResourceAttribute>()!.Type;
        }    
    }
}