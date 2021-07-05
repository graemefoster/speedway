using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Speedway.Deploy.Core;
using Speedway.Deploy.Core.Resources.Speedway;
using Speedway.Deploy.Core.Resources.Speedway.Context;

namespace Speedway.Deploy.Provider.AzureAppService.Context
{
    // ReSharper disable once ClassNeverInstantiated.Global
    internal class ResourceGroupSpeedwayPlatformContextTwin : ISpeedwayContextPlatformTwin
    {
        private readonly ILogger<ResourceGroupSpeedwayPlatformContextTwin> _logger;
        private readonly IAzure _az;
        private readonly SpeedwayContextResource _context;
        private readonly IOptions<AzureSpeedwaySettings> _settings;
        private IResourceGroup? _resourceGroup;

        public ResourceGroupSpeedwayPlatformContextTwin(
            ILogger<ResourceGroupSpeedwayPlatformContextTwin> logger,
            IAzure az,
            SpeedwayContextResource context,
            IOptions<AzureSpeedwaySettings> settings)
        {
            _logger = logger;
            _az = az;
            _context = context;
            _settings = settings;
        }

        public string Id => _resourceGroup!.Id;
        public string Name => _resourceGroup!.Name;

        public async Task<SpeedwayResourceOutputMetadata> Reflect()
        {
            var rgName = $"sw-{_context.Manifest.Slug}-{_context.GetEnvironmentShort()}";
            _resourceGroup = _az.ResourceGroups.FindSpeedwayResource(_context).SingleOrDefault();
            if (_resourceGroup == null)
            {
                _resourceGroup = await _az.ResourceGroups
                    .Define(rgName)
                    .WithRegion(Microsoft.Azure.Management.ResourceManager.Fluent.Core.Region.Create(_settings.Value.Region))
                    .WithSpeedwayTag(_context.Manifest.Id, _context.Manifest.Slug, _context.Environment).CreateAsync();

                _logger.LogInformation(
                    "Created Resource Group {ResourceGroupName} matching ProjectId {Id} for environment {Env}",
                    _resourceGroup.Name, _context.Manifest.Id, _context.Environment);
            }
            else
            {
                _logger.LogInformation(
                    "Found Resource Group {ResourceGroupName} matching ProjectId {Id} for environment {Env}",
                    _resourceGroup.Name, _context.Manifest.Id, _context.Environment);
            }

            return new SpeedwayResourceOutputMetadata(_resourceGroup!.Name, new Dictionary<string, string>(), new HashSet<string>());
        }

        public string Region()
        {
            return _resourceGroup!.Region.Name;
        }
        
    }
}