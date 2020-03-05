using Microsoft.AspNetCore.Hosting;
using MSTK.Core;
using System;
using System.Collections.Generic;
using System.IO;

namespace MSTK.Eventing
{
    public class Hub
    {
        const string HubHostAddress = "http://localhost:8085";

        public Hub()
        {
            _EventHubHost = StartHost();
        }

        public static List<SubscriptionInfo> Subscriptions = new List<SubscriptionInfo>();

        IWebHost _EventHubHost = null;

        public void Start()
        {
            _EventHubHost.Start();
        }

        public void LoadSubscriptions()
        {
            // TODO: Read a JSON file with info about event subscriptions.
        }

        IWebHost StartHost()
        {
            IWebHost host = new WebHostBuilder()
                .UseKestrel()
                .UseUrls(new string[] { HubHostAddress })
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<Startup>()
                .Build();

            return host;
        }
    }
}
