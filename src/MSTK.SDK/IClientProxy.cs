using System;

namespace MSTK.SDK
{
    public interface IClientProxy
    {
        void PublishEvent(string eventName, object payload);
    }
}
