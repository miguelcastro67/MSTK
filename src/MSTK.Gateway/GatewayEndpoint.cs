using System;
using System.Linq;

namespace MSTK.Gateway
{
    public class GatewayEndpoint
    {
        public string Address { get; set; }
        public string Method { get; set; }
        public string Route { get; set; }
        public bool IsActive { get; set; }
        public DateTime LastCall { get; set; }
    }
}
