using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Unearth.Database;

namespace ServiceResolver.UnitTests
{
    [TestClass]
    public class DatabaseTests
    {
        [TestMethod]
        public async Task SqlDatabase_Test()
        {
            var locator = new DatabaseLocator(); // {ServiceDomain = "kmb.home"};
            Console.WriteLine("Testing in {0}", locator.ServiceDomain);

            DatabaseService svc = await locator.Locate("fluf-workflow-db", DatabaseProtocol.Sql);
            Assert.IsNotNull(svc);

            string[] connArray = svc.ConnectionStrings.ToArray();
            Assert.IsTrue(connArray.Length == 1);

            foreach (string s in connArray)
                Console.WriteLine(s);

            Assert.AreEqual(
                "Server=tcp:vrdworkflow01.rnd.ipzo.net,1433;Database=WorkflowConfig;User ID=fluf_app;Password=Password1!;",
                //"Server=tcp:dilbert7.kmb.home,1433;Database=Onumbers;Integrated Security=SSPI;",
                //"Server=tcp:vrdlfadb01.rnd.ipzo.net,1433;Database=Onumbers;User ID=sa;Password=C4bb4g5;Applicaiton Name=kellyb;",
                connArray[0]
                );
        }

        [TestMethod]
        public async Task MongoDb_Test()
        {
            var locator = new DatabaseLocator(); // {ServiceDomain = "kmb.home"};
            Console.WriteLine("Testing in {0}", locator.ServiceDomain);

            DatabaseService svc = await locator.Locate("calltopark-db", DatabaseProtocol.MongoDb);
            Assert.IsNotNull(svc);

            string[] connArray = svc.ConnectionStrings.ToArray();
            Assert.IsTrue(connArray.Length == 1);

            foreach (string s in connArray)
                Console.WriteLine(s);

            Assert.AreEqual(
                "mongodb://vsropknosql01.ipzhost.net:27017,vsropknosql02.ipzhost.net:27017,vsropknosql03.ipzhost.net:27017/CallToPark?replicaSet=OmniPark-Mongo-1&serverSelectionTimeoutMS=7000",
                connArray[0]
            );
        }

        [TestMethod]
        public async Task RedisDb_Test()
        {
            var locator = new DatabaseLocator(); // {ServiceDomain = "kmb.home"};
            Console.WriteLine("Testing in {0}", locator.ServiceDomain);

            DatabaseService svc = await locator.Locate("dark-rnd", DatabaseProtocol.Redis);
            Assert.IsNotNull(svc);

            string[] connArray = svc.ConnectionStrings.ToArray();
            Assert.IsTrue(connArray.Length == 1);

            foreach (string s in connArray)
                Console.WriteLine(s);

            Assert.AreEqual(
                "dark-rnd.redis.cache.windows.net:6380,ssl=True,abortConnect=False,password=6GFHu3Ghwm03VXejZesHWlY/Fzv4Km+xcBCOuleLnGE=",
                connArray[0]
            );
        }

    }
}
