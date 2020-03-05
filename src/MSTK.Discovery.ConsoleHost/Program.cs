using MSTK.Core;
using MSTK.Core.UI;
using System;

namespace MSTK.Discovery.ConsoleHost
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting discovery hub...");
            
            Hub hub = new Hub();
            hub.Start();

            Console.WriteLine("Discovery hub running.");

            ConsoleHelper consoleHelper = new ConsoleHelper();
            consoleHelper.ShowMenu(new MenuItem[]
            {
                new MenuItem("List connected hosters",
                () =>
                {
                    Console.WriteLine();
                    foreach (DiscoveryMetadata host in Hub.Hosts)
                        Console.WriteLine(host.HostName + "/" + host.Instance + " : " + host.HostAddress);
                })
            });
        }
    }
}
