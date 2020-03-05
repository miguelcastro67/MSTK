using System;
using System.Linq;
using System.Reflection;

namespace MSTK.Hosting
{
    public class HostHelper
    {
        public static Hoster Hoster = null;

        public static void StartHost()
        {
            Console.WriteLine("Starting host...");

            Hoster = new Hoster();// "http://localhost:8082");

            Hoster.DiscoveryHubConnect += (s, e) =>
            {
                if (e.Connected)
                    Console.WriteLine("Connected to Discovery Hub.");
                else
                    Console.WriteLine("Discovery Hub not found.");
            };

            Hoster.EventHubSubscribed += (s, e) =>
            {
                if (e.Connected)
                    Console.WriteLine("Subscribed to Event Hub.");
                else
                    Console.WriteLine("Event Hub not found.");
            };

            Hoster.MonitorHubRegistered += (s, e) =>
            {
                if (e.Connected)
                    Console.WriteLine("Registered with Monitor Hub.");
                else
                    Console.WriteLine("Monitor Hub not found.");
            };

            Hoster.Start();

            Console.WriteLine("Host '{0}/{1}' running. Press [Enter] to exit.", Hoster.HostName, Hoster.Instance);
            Console.ReadLine();

            Hoster.Stop();
        }
    }
}
