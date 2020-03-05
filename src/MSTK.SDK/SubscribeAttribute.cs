using System;

namespace MSTK.SDK
{
    [AttributeUsage(AttributeTargets.Method)]
    public class SubscribeAttribute : Attribute
    {
        public SubscribeAttribute()
        {
            Enabled = true;
        }

        public SubscribeAttribute(string eventName)
        {
            EventName = eventName;
            Enabled = true;
        }

        public string EventName { get; set; }
        public bool Enabled { get; set; }
    }
}
