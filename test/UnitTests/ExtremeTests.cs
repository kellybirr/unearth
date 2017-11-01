using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Unearth.Database;
using Unearth.Dns;
using Unearth.Grpc;

namespace ServiceResolver.UnitTests
{
    [TestClass, Ignore]
    public class ExtremeTests
    {
        [TestMethod]
        public void Extreme_DNS_Test()
        {
            const int TIME = 2, COUNT = TIME * 2000;

            var stopAt = DateTime.Now.AddMinutes(TIME);
            while (DateTime.Now < stopAt)
            {
                var tasks = new List<Task>();
                var exceptions = new List<Exception>();

                for (var i = 0; i < COUNT; i++)
                {
                    var dns = new DnsQuery("zbox.kmb.home", DnsRecordType.A);
                    tasks.Add(dns.TryResolve().ContinueWith(t =>
                    {
                        try
                        {
                            var res = t.Result;
                        }
                        catch (Exception ex)
                        {
                            exceptions.Add(ex);
                        }
                    }));
                }

                Task.WaitAll(tasks.ToArray());
                if (exceptions.Count > 0)
                    throw new AggregateException(exceptions);

                Console.WriteLine("{0} Queried at {1:G}", COUNT, DateTime.Now);

                Task.Delay(TimeSpan.FromSeconds(TIME)).Wait();
            }
        }

        [TestMethod]
        public void Extreme_Grpc_Test()
        {
            const int TIME = 4, COUNT = TIME * 2000;

            var locator = new GrpcLocator();
            var stopAt = DateTime.Now.AddMinutes(TIME);
            while (DateTime.Now < stopAt)
            {
                var tasks = new List<Task>();
                var exceptions = new List<Exception>();

                for (var i = 0; i < COUNT; i++)
                {
                    var task = locator.Locate("onumbers");
                    tasks.Add(task.ContinueWith(t =>
                    {
                        try
                        {
                            var res = t.Result;
                        }
                        catch (Exception ex)
                        {
                            exceptions.Add(ex);
                        }
                    }));
                }

                Task.WaitAll(tasks.ToArray());
                if (exceptions.Count > 0)
                    throw new AggregateException(exceptions);

                Console.WriteLine("{0} Located at {1:G}", COUNT, DateTime.Now);

                Task.Delay(TimeSpan.FromSeconds(TIME)).Wait();
            }
        }

        [TestMethod]
        public void Extreme_DB_Test_Cached()
        {
            NumbersDb.Locator.NoCache = false;
            Extreme_DB_Test();
        }

        [TestMethod]
        public void Extreme_DB_Test_NoCache()
        {
            NumbersDb.Locator.NoCache = true;
            Extreme_DB_Test();
        }

        private void Extreme_DB_Test()
        {
            const int TIME = 3, COUNT = TIME * 2000;

            var stopAt = DateTime.Now.AddMinutes(TIME);
            while (DateTime.Now < stopAt)
            {
                var tasks = new List<Task>();
                var exceptions = new List<Exception>();

                for (var i = 0; i < COUNT; i++)
                {
                    tasks.Add(NumbersDb.Open().ContinueWith(t =>
                    {
                        try
                        {
                            using (NumbersDb db = t.Result)
                                db.Dispose();
                        }
                        catch (Exception ex)
                        {
                            exceptions.Add(ex);
                            Console.WriteLine(ex.Message);
                        }
                    }));
                }

                Task.WaitAll(tasks.ToArray());
                if (exceptions.Count > 0)
                    throw new AggregateException(exceptions);

                Console.WriteLine("{0} Connected at {1:G}", COUNT, DateTime.Now);

                Task.Delay(TimeSpan.FromSeconds(TIME)).Wait();
            }
        }

        public class NumbersDb : IDisposable
        {
            public const string DbServiceName = "onumbers-db";

            public static DatabaseLocator Locator { get; } = new DatabaseLocator();

            public static async Task<NumbersDb> Open()
            {
                await Locator.Locate(DbServiceName, DatabaseProtocol.Sql);
                return new NumbersDb();
            }

            public void Dispose()
            { }
        }

    }
}
