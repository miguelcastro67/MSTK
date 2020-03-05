using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using MSTK.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Timers;

namespace MSTK.Monitor
{
    public class Hub
    {
        const string HubHostAddress = "http://localhost:8086";

        public Hub()
        {
            _MonitorHubHost = StartHost();
        }

        public static List<HostMetadata> Hosts = new List<HostMetadata>();

        IWebHost _MonitorHubHost = null;
        List<ExpectedHost> _ExpectedHosts = null;
        
        public void Start()
        {
            _MonitorHubHost.Start();
            LoadRegistrations();
            CheckHosts();
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

        void LoadRegistrations()
        {
            IConfigurationBuilder configBuilder = new ConfigurationBuilder();
            configBuilder.AddJsonFile("hosts.json");
            var configuration = configBuilder.Build();
            
            _ExpectedHosts = configuration.GetSection("hosts").GetChildren()
                .Select(hostConfig => new ExpectedHost
                {
                    HostName = hostConfig["hostName"],
                    MinInstances = int.Parse(hostConfig["minInstances"]),
                    MaxInstances = int.Parse(hostConfig["maxInstances"])
                }).ToList();
        }

        void CheckHosts()
        {
            Timer checkTimer = new Timer(5000);

            checkTimer.Elapsed += (s, e) =>
            {
                checkTimer.Stop();

                foreach (var expectedHost in _ExpectedHosts)
                {
                    var hosts = Hosts.Where(item => item.HostName.ToLower() == expectedHost.HostName.ToLower());
                    if (hosts == null || hosts.Count() < expectedHost.MinInstances)
                    {
                        int instancesFound = 0;
                        if (hosts != null)
                            instancesFound = hosts.Count();

                        Console.WriteLine("Expected {0} instances of host '{1}'. {2} instances found.", expectedHost.MinInstances, expectedHost.HostName, instancesFound);
                        expectedHost.ReqMet = false;
                    }
                    else
                    {
                        expectedHost.InstancesFound = hosts.Count();
                        if (!expectedHost.ReqMet)
                        {
                            Console.WriteLine("All instances of host '{0}' found.", expectedHost.HostName);
                            expectedHost.ReqMet = true;
                        }
                    }
                }

                checkTimer.Start();
            };

            checkTimer.Start();
        }
    }
}
