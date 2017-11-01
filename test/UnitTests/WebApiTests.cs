using System;
using System.Linq;
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

    }
}
