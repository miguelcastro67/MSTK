using System;

namespace MSTK.Core
{
    public class SubscriptionInfo
    {
        public string HostName { get; set; }
        public string Instance { get; set; }
        public string CallbackAddress { get; set; }
        public string EventName { get; set; }
    }
}
