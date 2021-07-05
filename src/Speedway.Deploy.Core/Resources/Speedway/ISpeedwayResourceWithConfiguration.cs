using System.Collections.Generic;

namespace Speedway.Deploy.Core.Resources.Speedway
{
    public interface ISpeedwayResourceWithConfiguration : ISpeedwayResource
    {
        IDictionary<string, string> Configuration { get; }
    }
}