using System;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Health.V1;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Unearth.Grpc;

namespace ServiceResolver.UnitTests
{
    [TestClass]
    public class GrpcTests
    {
        [TestMethod]
        public async Task GrpcService_A_Test()
        {
            var locator = new GrpcLocator {NoCache = true, Randomize = true};
            Console.WriteLine("Testing in {0}", locator.ServiceDomain);

            GrpcService svc = await locator.Locate("onumbers", ChannelCredentials.Insecure);

            Assert.IsNotNull(svc);
            Assert.AreEqual(5, svc.Endpoints.Count);

            foreach (GrpcEndpoint ep in svc.Endpoints)
                Console.WriteLine(ep);

            //Assert.AreEqual("vrdswarm11.rnd.ipzo.net:7006", svc.Endpoints[0].ToString());
            Assert.IsTrue(svc.Expires < DateTime.UtcNow.AddMinutes(1));
        }

        [TestMethod]
        public async Task GrpcService_B_Test()
        {
            var locator = new GrpcLocator();
            Console.WriteLine("Testing in {0}", locator.ServiceDomain);

            GrpcService svc = await locator.Locate("alpha-users", ChannelCredentials.Insecure);

            Assert.IsNotNull(svc);
            Assert.AreEqual(1, svc.Endpoints.Count);

            foreach (GrpcEndpoint ep in svc.Endpoints)
                Console.WriteLine(ep);

            Assert.AreEqual("vrdswarm11.rnd.ipzo.net:7014", svc.Endpoints[0].ToString());
            Assert.IsTrue(svc.Expires < DateTime.UtcNow.AddMinutes(1));
        }

        [TestMethod]
        public async Task GrpcService_Exec_Test()
        {
            var locator = new GrpcLocator();
            Console.WriteLine("Testing in {0}", locator.ServiceDomain);

            GrpcService svc = await locator.Locate("alpha-users", ChannelCredentials.Insecure);
            Assert.IsNotNull(svc);

            HealthCheckResponse reply = await svc.Execute(async channel =>
            {
                var client = new Health.HealthClient(channel);
                return await client.CheckAsync(new HealthCheckRequest {Service = "users"});
            }, TimeSpan.FromSeconds(2));


            Assert.AreEqual(HealthCheckResponse.Types.ServingStatus.Serving, reply.Status);
        }
    }
}
