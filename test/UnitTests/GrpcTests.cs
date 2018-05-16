using System;
using System.Linq;
using System.Threading;
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
            Assert.IsTrue(svc.Endpoints.Count > 0);

            // for testing - remove servers 4/5
            //svc.Endpoints.Remove(svc.Endpoints.First(ep => ep.Host == "vsrcoredock04.ipzhost.net"));
            //svc.Endpoints.Remove(svc.Endpoints.First(ep => ep.Host == "vsrcoredock05.ipzhost.net"));

            foreach (GrpcEndpoint ep in svc.Endpoints)
                Console.WriteLine(ep);

            Assert.IsTrue(svc.Expires < DateTime.UtcNow.AddMinutes(15));
            Console.WriteLine();

            int ts = Environment.TickCount;

            //TimeSpan tryNextAfter = Timeout.InfiniteTimeSpan;
            TimeSpan tryNextAfter = TimeSpan.FromMilliseconds(150);

            var channel = svc.Connect(TimeSpan.FromSeconds(3), tryNextAfter).Result;
            Assert.IsTrue(channel.State == ChannelState.Ready);

            Console.WriteLine("Connected To: '{0}' in {1}ms", channel.Target, Environment.TickCount-ts);
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

        [TestMethod]
        public async Task Cancel_Task_Test()
        {
            var cancel = new CancellationTokenSource(TimeSpan.FromSeconds(1));

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(2), cancel.Token);
            }
            catch (TaskCanceledException ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
