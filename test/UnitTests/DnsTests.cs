using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Unearth.Dns;

namespace ServiceResolver.UnitTests
{
    [TestClass]
    public class DnsTests
    {
        [TestMethod]
        public async Task SRV_Query_Test()
        {
            await Query_Internal("_ldap._tcp.imprezzio.org", DnsRecordType.SRV);
        }

        [TestMethod]
        public async Task MX_Query_Test()
        {
            await Query_Internal("imprezzio.com", DnsRecordType.MX);
        }

        [TestMethod]
        public async Task TXT_Query_Test()
        {
            await Query_Internal("test-txt.kmb.home", DnsRecordType.TXT);
        }

        [TestMethod]
        public async Task SD_Query_Test()
        {
            string sd = "onumbers-db._sql._tcp.kmb.home";
            await Task.WhenAll(
                Query_Internal(sd, DnsRecordType.SRV),
                Query_Internal(sd, DnsRecordType.TXT)
            );
        }

        private async Task Query_Internal(string queryStr, DnsRecordType type)
        {
            var query = new DnsQuery(queryStr, type);
            DnsEntry[] records = await query.Resolve();

            Assert.IsTrue(records.Length > 0);
            Assert.AreEqual(DnsQueryStatus.Found, query.QueryStatus);

            foreach (DnsEntry rec in records)
                Console.WriteLine($"({rec.Type}) {rec}");
        }

        [TestMethod]
        public async Task SRV_Query_Speed_Test()
        {
            for (int i = 0; i < 20; i++)
            {
                DateTime start = DateTime.UtcNow;
                var query = new DnsQuery("_ldap._tcp.imprezzio.org", DnsRecordType.SRV);
                await query.Resolve();

                double ms = DateTime.UtcNow.Subtract(start).TotalMilliseconds;
                Console.WriteLine($"[{i}] = {ms}ms");
            }

        }


        [TestMethod]
        public async Task SRV_Query_Test_Hosts()
        {
            var query = new DnsQuery("_ldap._tcp.imprezzio.org", DnsRecordType.SRV);
            DnsEntry[] records = await query.Resolve();

            Assert.IsTrue(records.Length > 0);
            Assert.AreEqual(DnsQueryStatus.Found, query.QueryStatus);

            foreach (DnsEntry rec in records)
            {
                Console.WriteLine($"*** {rec} ***");
                foreach (DnsHostEntry host in query.GetHostEntries((DnsServiceEntry)rec))
                    Console.WriteLine($"({host.Type}) {host}");

                Console.WriteLine();
            }
        }


        [TestMethod]
        public async Task SRV_Fail_Test()
        {
            var query = new DnsQuery("_unknown._tcp.imprezzio.org", DnsRecordType.SRV);
            DnsEntry[] records = await query.TryResolve();

            Assert.AreEqual(0, records.Length);
            Assert.AreEqual(DnsQueryStatus.NotFound, query.QueryStatus);
        }

        [TestMethod, ExpectedException(typeof(DnsResolveException))]
        public async Task A_Fail_Test()
        {
            var query = new DnsQuery("does-not-exist.themostobscure.com", DnsRecordType.A);
            await query.Resolve();
        }
    }
}
