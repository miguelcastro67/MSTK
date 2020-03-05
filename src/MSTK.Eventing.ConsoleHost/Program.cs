using MSTK.Core;
using MSTK.Core.UI;
using System;

namespace MSTK.Eventing.ConsoleHost
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting eventing hub...");

            Hub hub = new Hub();
            hub.Start();

            Console.WriteLine("Eventing hub running.");

            ConsoleHelper consoleHelper = new ConsoleHelper();
            consoleHelper.ShowMenu(new MenuItem[]
            {
                new MenuItem("List event subscribers",
                () =>
                {
                    Console.WriteLine();
                    foreach (SubscriptionInfo host in Hub.Subscriptions)
                        Console.WriteLine(host.EventName + " - " + host.HostName + "/" + host.Instance + " : " + host.CallbackAddress);
                })
            });
        }
    }
}
