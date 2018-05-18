using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using StackExchange.Redis;
using Unearth.Dns;

namespace Unearth.Redis
{
    public class RedisService : GenericService
    {
        public RedisService() { }

        public RedisService(IEnumerable<ServiceEndpoint> endpoints) : base(endpoints)
        { }

        public RedisService(IEnumerable<DnsEntry> dnsEntries) : base(dnsEntries)
        { }

        public Task<ConnectionMultiplexer> Connect()
        {
            return ConnectionMultiplexer.ConnectAsync(ConnectionString);
        }

        public Task<ConnectionMultiplexer> Connect(TextWriter log)
        {
            return ConnectionMultiplexer.ConnectAsync(ConnectionString, log);
        }

        public string ServerList
        {
            get
            {
                // get endpoints
                var sb = new StringBuilder();
                foreach (ServiceEndpoint ep in Endpoints)
                {
                    if (sb.Length > 0) sb.Append(',');
                    sb.Append($"{ep.Host}:{ep.Port}");
                }

                return sb.ToString();
            }
        }

        public string ConnectionString
        {
            get
            {
                var sb = new StringBuilder();
                sb.Append(ServerList);

                foreach (var kv in Parameters)
                    sb.Append($";{kv.Key}={kv.Value}");

                return sb.ToString();
            }
        }
    }
}
