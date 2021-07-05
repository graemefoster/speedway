using System;

namespace Speedway.Core.Resources
{
    public class SpeedwayResourceAttribute : Attribute
    {
        public SpeedwayResourceAttribute(string type)
        {
            Type = type;
        }

        public string Type { get; }
    }

}