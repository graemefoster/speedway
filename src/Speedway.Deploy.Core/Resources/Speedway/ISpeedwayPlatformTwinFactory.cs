namespace Speedway.Deploy.Core.Resources.Speedway
{
    /// <summary>
    /// Builds objects that will represent Speedway Resources on a native platform
    /// </summary>
    public interface ISpeedwayPlatformTwinFactory
    {
        public ISpeedwayPlatformTwin Build<T>(T speedwayResource) where T : ISpeedwayResource;
    }
}