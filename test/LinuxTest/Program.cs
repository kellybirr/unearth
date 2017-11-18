using System;
using Unearth.Dns;

namespace LinuxTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var query = new DnsQuery("leadalerts._grpc._tcp.dev-test.rnd.ipzo.net", DnsRecordType.SRV);
            var records = query.Resolve().Result;
		    foreach (DnsEntry record in records) 
            {
			     Console.WriteLine(record.ToString());
            }
        }
    }
}
