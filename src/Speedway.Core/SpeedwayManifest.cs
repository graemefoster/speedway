using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Speedway.Core.Resources;

namespace Speedway.Core
{
    public record SpeedwayManifest(Guid Id, string ApiVersion, string Slug, string DisplayName,
        List<SpeedwayResourceMetadata> Resources,
        string[] Developers, string[] Testers)
    {
        public string SerializeToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented, new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.Auto,
                SerializationBinder = new SpeedwayManifestSerializationBinder(),
                Converters = new JsonConverter[]
                {
                    new StringEnumConverter(),
                },
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });
        }

        public static SpeedwayManifest DeserializeFromJson(string content)
        {
            return JsonConvert.DeserializeObject<SpeedwayManifest>(content, new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.Auto,
                SerializationBinder = new SpeedwayManifestSerializationBinder(),
                Converters = new JsonConverter[]
                {
                    new StringEnumConverter()
                },
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            })!;
        }
    }
}