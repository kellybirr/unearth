using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Unearth.WebApi;

namespace ServiceResolver.UnitTests
{
    [TestClass]
    public class WebApiTests
    {
        [TestMethod]
        public async Task Api_1_Test()
        {
            var locator = new WebApiLocator(); // {ServiceDomain = "dev-test.rnd.ipzo.net"};
            Console.WriteLine("Testing in {0}", locator.ServiceDomain);

            WebApiService svc = await locator.Locate("api1");
            Assert.IsNotNull(svc);

            svc.Decryptor.KeyPhrase = "dev";

            Uri[] uriArray = svc.Uris.ToArray();
            Assert.IsTrue(uriArray.Length > 0);

            foreach (Uri u in uriArray)
                Console.WriteLine(u.ToString());

            Assert.AreEqual(
                "https://justaguy@secure.nunya.biz/api",
                uriArray[0].ToString()
            );

            Assert.IsTrue(svc.Headers.Count > 0);
            Assert.AreEqual("Banana", svc.Headers["X-Custom-Header"]);
        }

        [TestMethod]
        public async Task Api_RR_Test()
        {
            var locator = new WebApiLocator(); // {ServiceDomain = "dev-test.rnd.ipzo.net"};
            Console.WriteLine("Testing in {0}", locator.ServiceDomain);

            WebApiService svc = await locator.Locate("rr-api");
            Assert.IsNotNull(svc);

            Uri[] uriArray = svc.Uris.ToArray();
            Assert.IsTrue(uriArray.Length == 3);

            foreach (Uri u in uriArray)
                Console.WriteLine(u.ToString());
        }


        [TestMethod]
        public async Task Public_Web_Test()
        {
            var locator = new WebApiLocator();
            Console.WriteLine("Testing in {0}", locator.ServiceDomain);

            WebApiService svc = await locator.Locate("@dialer-ivr-pub".Substring(1));
            Assert.IsNotNull(svc);

            Uri[] uriArray = svc.Uris.ToArray();
            Assert.IsTrue(uriArray.Length == 1);

            foreach (Uri u in uriArray)
                Console.WriteLine(u.ToString());
        }

        [TestMethod]
        public void Sync_MyServer_Test()
        {
            DateTime until = DateTime.Now.AddHours(1);
            while (DateTime.Now < until)
            {
                DateTime b4 = DateTime.UtcNow;
                string uri = HtmlExtensions.MyServer;
                double ms = DateTime.UtcNow.Subtract(b4).TotalMilliseconds;

                Console.WriteLine($"{uri} ({ms})");

                Thread.Sleep(10_000);
            }
        }

    }

    public static class HtmlExtensions
    {
        private static readonly WebApiLocator _locator = new WebApiLocator {ServiceDomain = "apps.ipzhost.net"};

        public static string MyServer
        {
            get
            {
                string location = "@dialer-ivr-pub";
                if (!location.StartsWith("@")) return location;

                // this has to be sync
                WebApiService webService = _locator.Locate(location.Substring(1)).Result;
                return webService.Uris.First().ToString().TrimEnd('/');
            }
        }
    }
}
