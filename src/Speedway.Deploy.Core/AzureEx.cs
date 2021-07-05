using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core.CollectionActions;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core.Resource.Definition;
using Speedway.Deploy.Core.Resources.Speedway;
using Speedway.Deploy.Core.Resources.Speedway.Context;

namespace Speedway.Deploy.Core
{
    public static class AzureEx
    {
        public static IEnumerable<T> FindSpeedwayResource<T>(this ISupportsListingByTag<T> items,
            SpeedwayContextResource context) where T : IResource
        {
            return items.ListByTag("speedwayId", context.Manifest.Id.ToString())
                .Where(x => x.Tags.ContainsKey("speedwayEnvironment") &&
                            x.Tags["speedwayEnvironment"] == context.Environment);
        }

        public static string SuggestResourceName(SpeedwayContextResource context, string name, int characterLimit = 40, bool allowHyphens = true)
        {
            var namePart = name.Length > characterLimit / 2 ? name.Substring(0, characterLimit / 2) : name;
            var suggestedName = $"{namePart}-{context.GetEnvironmentShort()}-{Guid.NewGuid().ToString()}";
            if (!allowHyphens) suggestedName = suggestedName.Replace("-", "");
            return suggestedName.Length > characterLimit ? suggestedName.Substring(0, characterLimit) : suggestedName;
        }
        
        
        public static T WithSpeedwayTag<T>(this IDefinitionWithTags<T> resource, Guid manifestId, string slug, string environment)
        {
            return resource.WithTags(BuildSpeedwayTags(manifestId, slug, environment));
        }

        public static Dictionary<string, string> BuildSpeedwayTags(Guid manifestId, string slug, string environment)
        {
            return new()
            {
                {"speedway-manifest-id", manifestId.ToString()},
                {"speedway-name", slug},
                {"speedway-environment", environment}
            };
        }

        public static bool SpeedwayResourceExists<T>(
            this IEnumerable<T> resources, 
            ISpeedwayResource metadata, 
            string environment, 
            out T? existing) where T:IResource
        {
            existing = resources.FirstOrDefault(x => x.Tags.ContainsKey("speedway-name") && x.Tags["speedway-name"] == metadata.Name && x.Tags.ContainsKey("speedway-environment") && x.Tags["speedway-environment"] == environment);
            return existing != null;
        }

    }
}