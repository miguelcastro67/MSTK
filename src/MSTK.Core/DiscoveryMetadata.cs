using System;
using System.Collections.Generic;

namespace MSTK.Core
{
    public class DiscoveryMetadata
    {
        public string HostName { get; set; }
        public string Instance { get; set; }
        public string HostAddress { get; set; }
        public IEnumerable<Service> Services { get; set; }
    }
}
