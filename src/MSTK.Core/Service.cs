using System;
using System.Collections.Generic;

namespace MSTK.Core
{
    public class Service
    { 
        public string Name { get; set; }
        public string DiscoveryName { get; set; }
        public string Dependency { get; set; }
        public IEnumerable<Operation> Operations { get; set; }
    }
}
