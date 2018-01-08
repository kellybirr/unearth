using System;
using Unearth.Dns;
using Unearth.Database;

namespace LinuxTest
{
    class Program
    {
        static void Main(string[] args)
        {
            //var query = new DnsQuery("onumbers-db._sql._tcp.dev-test.rnd.ipzo.net", DnsRecordType.TXT);
            //var query = new DnsQuery("leadalerts._grpc._tcp.dev-test.rnd.ipzo.net", DnsRecordType.SRV);
            //var query = new DnsQuery("gmail.com", DnsRecordType.MX);
            // var records = query.Resolve().Result;
		    // foreach (DnsEntry record in records) 
            // {
			//      Console.WriteLine(record.ToString());
            // }

            var locator = new DatabaseLocator { ServiceDomain = "dev-test.rnd.ipzo.net" };
            
            var service = locator.Locate("onumbers-db", DatabaseProtocol.Sql).Result;
            service.Decryptor.KeyPhrase = "dev";

            foreach (var connStr in service.ConnectionStrings)
                Console.WriteLine(connStr);
        }
    }
}
