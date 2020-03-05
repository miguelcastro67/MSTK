using System;

namespace MSTK.SDK
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class DiscoveryAttribute : Attribute
    {
        public DiscoveryAttribute()
        {
            Enabled = true;
        }

        public DiscoveryAttribute(string discoveryName)
        {
            DiscoveryName = discoveryName;
            Enabled = true;
        }

        public string DiscoveryName { get; set; }
        public bool Enabled { get; set; }
        public string Dependency { get; set; }
    }
}
