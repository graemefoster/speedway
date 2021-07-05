using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Speedway.Deploy.Core.Resources.Speedway.SecretStore;

namespace Speedway.Deploy.Core.Resources.Speedway.Configuration
{
    /// <summary>
    /// Works out the new configuration for a Speedway class
    /// </summary>
    public class ConfigurationMerger
    {
        private readonly ILogger _logger;
        private readonly ISpeedwayResourceWithConfiguration _resource;

        public ConfigurationMerger(ILogger logger, ISpeedwayResourceWithConfiguration resource)
        {
            _logger = logger;
            _resource = resource;
        }
 
        /// <summary>
        /// Works out what the new configuration will be by applying the manifest file to the existing config / output from other resources.
        /// </summary>
        /// <param name="speedwayResourceOutputMetadata"></param>
        /// <returns></returns>
        internal Dictionary<string, string> CalculateNewAppConfiguration(IDictionary<ISpeedwayResource, SpeedwayResourceOutputMetadata> speedwayResourceOutputMetadata)
        {
            var currentConfiguration = ((ISpeedwayResourceOutputMetadataWithConfiguration) speedwayResourceOutputMetadata[_resource]).Configuration;

            var newAppSettings = _resource.Configuration.Keys
                .Select(x =>
                {
                    var expectedSetting = GetExpectedAppSetting(x, speedwayResourceOutputMetadata);
                    if (currentConfiguration.TryGetValue(x, out var val))
                    {
                        return val != expectedSetting ? x : null;
                    }

                    return x;
                })
                .Where(x => x != null)
                .ToDictionary(x => x!, x => GetExpectedAppSetting(x!, speedwayResourceOutputMetadata));

            return newAppSettings;
        }

        /// <summary>
        /// Works out what the configuration setting should be. Currently allows:
        ///  - hardcoded values
        ///  - [secret] which will generate 2 guids as a random entry
        ///  - [&lt;resource-name&gt;.Configuration.Setting] which are . properties into another resources output.
        /// </summary>
        /// <param name="settingName"></param>
        /// <param name="speedwayResourceOutputMetadata"></param>
        /// <returns></returns>
        private string GetExpectedAppSetting(string settingName, IDictionary<ISpeedwayResource, SpeedwayResourceOutputMetadata> speedwayResourceOutputMetadata)
        {
            var val = _resource.Configuration![settingName!];
            if (val.StartsWith("[secret."))
            {
                var secretParts = val.Substring(1, val.Length - 2).Substring("secret-".Length).Split(".");
                var kv = (SpeedwaySecretContainerResource)speedwayResourceOutputMetadata.Keys.Single(x => x is SpeedwaySecretContainerResource && x.Name == SpeedwaySecretContainerResource.DefaultName);
                var expectedAppSetting = $"@Microsoft.KeyVault(SecretUri={kv.GetSecretUri(speedwayResourceOutputMetadata.Keys.Single(x => x.Name == secretParts[0]), secretParts[1])})";
                return expectedAppSetting;
            }

            if (val.StartsWith("[predefined-secret."))
            {
                var secretParts = val.Substring(1, val.Length - 2).Substring("predefined-secret-".Length).Split(".");
                var kv = (SpeedwaySecretContainerResource)speedwayResourceOutputMetadata.Keys.Single(x => x is SpeedwaySecretContainerResource && x.Name == SpeedwaySecretContainerResource.DefaultName);
                var expectedAppSetting = $"@Microsoft.KeyVault(SecretUri={kv.GetPredefinedSecretUri(secretParts.Single())})";
                return expectedAppSetting;
            }

            if (val.StartsWith("["))
            {
                var property = val.Substring(1, val.Length - 2).Split(".").ToArray();

                _logger.LogDebug("Taking configuration for resource:{Resource} for property path:{Path}", property[0],
                    val);
                var resource = speedwayResourceOutputMetadata.Single(r => r.Key.Name == property[0]);

                var referenceObject = (object) resource.Value;
                foreach (var item in property.Skip(1))
                {
                    if (item.ToLowerInvariant() == "secrets")
                    {
                        throw new NotSupportedException("Please use the [secrets.<resource>.secret-name] syntax to reference secrets");
                    }
                    
                    if (referenceObject is IDictionary dictionary)
                    {
                        referenceObject = dictionary[item];
                    }
                    else
                    {
                        var propertyInfo = referenceObject!.GetType()
                            .GetProperty(item, BindingFlags.Public | BindingFlags.Instance)!;

                        if (propertyInfo == null)
                        {
                            throw new ArgumentException(
                                $"Could not find property {item} on type {referenceObject!.GetType()}");
                        }

                        _logger.LogDebug("Property {Item} on type:{Type} ({Property})", 
                            property[0], 
                            referenceObject.GetType().Name, 
                            propertyInfo.Name);

                        referenceObject = propertyInfo!.GetValue(referenceObject)!;
                    }
                }

                _logger.LogInformation("Found value from property path:{Path}", property[0]);
                return Convert.ToString(referenceObject)!;
            }
            return _resource.Configuration[settingName!];
        }

        
    }
}