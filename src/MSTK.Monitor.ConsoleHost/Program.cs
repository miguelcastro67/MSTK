using MSTK.Core;
using MSTK.Core.UI;
using MSTK.Monitor;
using System;

namespace MSTK.Monitor.ConsoleHost
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting monitor hub...");

            Hub hub = new Hub();
            hub.Start();

            Console.WriteLine("Monitor hub running.");

            ConsoleHelper consoleHelper = new ConsoleHelper();
            consoleHelper.ShowMenu(new MenuItem[]
            {
                new MenuItem("List connected hosters",
                () =>
                {
                    Console.WriteLine();
                    foreach (HostMetadata host in Hub.Hosts)
                        Console.WriteLine(host.HostName + "/" + host.Instance + " : " + host.HostAddress);
                })
            });
        }
    }
}
