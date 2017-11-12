using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unearth.WebApi;

namespace TestApp.Fx
{
    static class LongTest
    {
        public static void RunTest(string serviceName)
        {
            HtmlExtensions.Location = serviceName;

            try
            {
                int sleepTime = 0;
                DateTime until = DateTime.Now.AddHours(1);
                while (DateTime.Now < until)
                {
                    void DoIt()
                    {
                        DateTime b4 = DateTime.UtcNow;
                        string uri = HtmlExtensions.MyServer;
                        double ms = DateTime.UtcNow.Subtract(b4).TotalMilliseconds;

                        Console.WriteLine($"{uri} ({ms}/{sleepTime})");
                    }

                    Task[] tasks =
                    {
                        Task.Run(() => DoIt()),
                        Task.Run(() => DoIt()),
                        Task.Run(() => DoIt()),
                        Task.Run(() => DoIt()),
                        Task.Run(() => DoIt()),
                        Task.Run(() => DoIt()),
                        Task.Run(() => DoIt())
                    };

                    Task.WaitAll(tasks);

                    Thread.Sleep(sleepTime);
                    sleepTime += 200;
                }

                Console.WriteLine("OK");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            Console.ReadLine();
        }

        public static class HtmlExtensions
        {
            private static readonly WebApiLocator _locator = new WebApiLocator();

            public static string Location { get; set; }

            public static string MyServer
            {
                get
                {
                    Task<WebApiService> task = null;
                    WebApiService webService = null;
                    try
                    {
                        // this has to be sync
                        task = _locator.Locate(Location);
                        webService = task.Result;
                        if (webService == null)
                        {
                            Console.WriteLine("NULL");
                        }

                        return webService.Uris.First().ToString().TrimEnd('/');
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        throw;
                    }
                }
            }
        }

    }
}
