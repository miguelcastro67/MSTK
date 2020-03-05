using System;

namespace MSTK.Hosting.ConsoleHost1
{
    class Program
    {
        static void Main(string[] args)
        {
            HostHelper.StartHost();

            //Console.WriteLine("Starting first host...");

            //Hoster hoster = new Hoster();// "http://localhost:8082");
            //hoster.HubConnect += (s, e) =>
            //{
            //    if (e.Connected)
            //        Console.WriteLine("Connected to Discovery Hub.");
            //    else
            //        Console.WriteLine("Discovery Hub not found.");
            //};
            //
            //hoster.Start();

            //Console.WriteLine("Host running. Press [Enter] to exit.");
            //Console.ReadLine();

            //hoster.Stop();
        }
    }
}
