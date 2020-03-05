using System;

namespace MSTK.Hosting
{
    public class CallInfoEventArgs : EventArgs
    {
        public CallInfoEventArgs(string method, string requestUri, DateTime timeStamp)
        {
            Method = method;
            RequestUri = requestUri;
            TimeStamp = timeStamp;
        }

        public string Method { get; set; }
        public string RequestUri { get; set; }
        public DateTime TimeStamp { get; set; }
    }
}
