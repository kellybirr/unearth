using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StackExchange.Redis;
using Unearth.Redis;

namespace ServiceResolver.UnitTests
{
    [TestClass]
    public class RedisTests
    {
        [TestMethod]
        public async Task Redis_Test()
        {
            var locator = new RedisLocator();
            var service = await locator.Locate("dark-rnd");

            IConnectionMultiplexer connection = await service.Connect();
            IDatabase cache = connection.GetDatabase();
            
            TimeSpan ts = await cache.PingAsync();
            Console.WriteLine(ts.ToString());

            var g = Guid.NewGuid().ToString();
            await cache.StringSetAsync("Message", g);

            string s = await cache.StringGetAsync("Message");
            Console.WriteLine(s);

            Assert.AreEqual(g, s);
        }
    }
}
