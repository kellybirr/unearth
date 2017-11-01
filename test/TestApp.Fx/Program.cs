using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unearth;
using Unearth.Database;
using Unearth.Dns;
using Unearth.Encryption;
using Unearth.Grpc;
using Unearth.WebApi;

namespace TestApp.Fx
{
    class Program
    {
        static readonly GrpcLocator _grpc = new GrpcLocator();
        static readonly WebApiLocator _webapi = new WebApiLocator();
        static readonly DatabaseLocator _database = new DatabaseLocator();
        static readonly GenericLocator _generic = new GenericLocator();

        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: ue.exe <grpc|http|sql|mongodb|dns|tcp|udp> <service-idenitifier> [dns-type]");
                Console.WriteLine("       OR");
                Console.WriteLine("       ue.exe enc <string> <key>");
                return;
            }

            try
            {
                Console.WriteLine("Service Domain = '{0}'", _generic.ServiceDomain);

                switch (args[0].ToLowerInvariant())
                {
                    case "enc":
                        var aes = new AesCrypto {KeyPhrase = args[2]};
                        Console.WriteLine("Encrypted = \"{aes:" + aes.Encrypt(args[1]) + "}\"");
                        break;
                    case "dns":
                        if (Enum.TryParse(args[2], out DnsRecordType recordType))
                        {
                            var query = new DnsQuery(args[1], recordType);
                            DnsEntry[] records = query.Resolve().Result;

                            foreach (DnsEntry rec in records)
                                Console.WriteLine($"({rec.Type}) {rec}");
                        }
                        break;
                    case "grpc":
                        var g = _grpc.Locate(args[1]).Result;
                        foreach (GrpcEndpoint ep in g.Endpoints)
                            Console.WriteLine(ep.ToString());
                        break;
                    case "http":
                        var w = _webapi.Locate(args[1]).Result;
                        foreach (Uri u in w.Uris)
                            Console.WriteLine(u.ToString());
                        break;
                    case "sql":
                        var sql = _database.Locate(args[1], DatabaseProtocol.Sql).Result;
                        foreach (string sqlConnectionString in sql.ConnectionStrings)
                            Console.WriteLine(sqlConnectionString);
                        break;
                    case "mongodb":
                        var mongo = _database.Locate(args[1], DatabaseProtocol.MongoDb).Result;
                        foreach (string mConnectionString in mongo.ConnectionStrings)
                            Console.WriteLine(mConnectionString);
                        break;
                    case "tcp":
                        var tcp = _generic.Locate(args[1], IpProtocol.TCP).Result;
                        foreach (ServiceEndpoint ep in tcp.Endpoints)
                            Console.WriteLine(ep.ToString());
                        break;
                    case "udp":
                        var udp = _generic.Locate(args[1], IpProtocol.UDP).Result;
                        foreach (ServiceEndpoint ep in udp.Endpoints)
                            Console.WriteLine(ep.ToString());
                        break;
                }
            }
            catch (Exception ex)
            {
                BreakdownExcception(ex);
            }
        }

        static void BreakdownExcception(Exception ex)
        {
            if (ex is AggregateException aex)
                foreach (Exception e2 in aex.InnerExceptions)
                    BreakdownExcception(e2);
            else
                Console.WriteLine(ex.Message);
        }

        //static void Extreme_DNS_Test()
        //{
        //    const int TIME = 10, COUNT = TIME * 1000;

        //    var stopAt = DateTime.Now.AddMinutes(TIME);
        //    while (DateTime.Now < stopAt)
        //    {
        //        //var tasks = new List<Task>();
        //        for (var i = 0; i < COUNT; i++)
        //        {
        //            var dns = new DnsQuery("onumbers-db._sql._tcp.kmb.home", DnsRecordType.TXT);
        //            var task = dns.TryResolve().ContinueWith(OnResolved);
        //            //tasks.Add( task );
        //        }

        //        //Task.WaitAll(tasks.ToArray());
        //        if (_exceptions.Count > 0)
        //            throw new AggregateException(_exceptions);

        //        Console.WriteLine("{0} Queried at {1:G}", COUNT, DateTime.Now);

        //        Thread.Sleep(TimeSpan.FromSeconds(TIME/2));
        //    }
        //}

        //static void OnResolved(Task<DnsEntry[]> t)
        //{
        //    try
        //    {
        //        var res = t.Result;
        //        //foreach (DnsEntry entry in res)
        //        //    Console.WriteLine(entry);
        //    }
        //    catch (Exception ex)
        //    {
        //        _exceptions.Add(ex);
        //    }
        //}

        //private static async Task Query_Internal(string queryStr, DnsRecordType type)
        //{
        //    var query = new DnsQuery(queryStr, type);
        //    DnsEntry[] records = await query.TryResolve();

        //    foreach (DnsEntry rec in records)
        //        Console.WriteLine($"({rec.Type}) {rec}");
        //}

    }
}
