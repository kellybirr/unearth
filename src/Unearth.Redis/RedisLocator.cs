using System.Collections.Generic;
using System.Threading.Tasks;
using Unearth.Core;
using Unearth.Dns;

namespace Unearth.Redis
{
    public class RedisLocator : ServiceLocator<RedisService>
    {
        public Task<RedisService> Locate()
        {
            return Locate(null);
        }

        public override Task<RedisService> Locate(string serviceName)
        {
            string longName;
            if (string.IsNullOrWhiteSpace(serviceName))
                longName = serviceName = "_redis";
            else
                longName = $"{serviceName.ToLowerInvariant()}._redis";

            // get name of service to resolve
            ServiceDnsName name = new ServiceDnsName
            {
                Domain = ServiceDomain,
                ServiceName = serviceName,
                Protocol = "redis",
                DnsName = string.IsNullOrEmpty(ServiceDomain)
                    ? serviceName.ToLowerInvariant()
                    : $"{longName}._tcp.{ServiceDomain}"
            };

            return Locate(name, _ => ServiceLookup.SrvTxt(name, RedisServiceFactory));
        }

        private RedisService RedisServiceFactory(ServiceDnsName name, IEnumerable<DnsEntry> dnsEntries)
        {
            ApplyDnsRandomizer(ref dnsEntries);

            return new RedisService(dnsEntries)
            {
                Name = name.ServiceName,
                Protocol = name.Protocol,
                Decryptor = { ServiceDomain = name.Domain }
            };
        }
    }
}
