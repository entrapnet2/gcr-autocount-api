using System;
using System.Threading;

namespace GCR_autocount_api
{
    class Program
    {
        static void Main(string[] args)
        {
            string username = args.Length > 0 ? args[0] : "KENNY";
            string password = args.Length > 1 ? args[1] : "1111";

            var service = new MyService();
            service.Start(username, password);

            Console.WriteLine("Server running. Press Ctrl+C to stop...");
            
            // Keep running until Ctrl+C
            var exitEvent = new ManualResetEvent(false);
            Console.CancelKeyPress += (sender, e) => { e.Cancel = true; exitEvent.Set(); };
            exitEvent.WaitOne();

            service.Stop();
        }
    }
}
