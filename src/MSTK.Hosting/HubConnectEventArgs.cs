using System;
using System.Linq;

namespace MSTK.Hosting
{
    public class HubConnectEventArgs : EventArgs
    {
        public HubConnectEventArgs(bool connected)
        {
            Connected = connected;
        }

        public bool Connected { get; set; }
    }
}
