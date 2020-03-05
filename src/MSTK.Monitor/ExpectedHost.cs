using System;
using System.Linq;

namespace MSTK.Monitor
{
    public class ExpectedHost
    {
        public string HostName { get; set; }
        public int MinInstances { get; set; }
        public int MaxInstances { get; set; }
        public int InstancesFound { get; set; }
        public bool ReqMet { get; set; }
    }
}
