using Microsoft.AspNetCore.Hosting;
using MSTK.Core;
using System;
using System.Collections.Generic;
using System.IO;

namespace MSTK.Discovery
{
    public class Hub
    {
        const string HubHostAddress = "http://localhost:8084";

        public Hub()
        {
            _DiscoveryHubHost = StartHost();
        }
        
        public static List<DiscoveryMetadata> Hosts = new List<DiscoveryMetadata>();

        IWebHost _DiscoveryHubHost = null;

        public void Start()
        {
            _DiscoveryHubHost.Start();
        }

        public void LoadRegistrations()
        {
            // TODO: Read a JSON file with metadata about external services.
            //       Will need the address of service (with host),
            //                 the HTTP verb it will use,
            //                 and a discovery label for the operation.
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
