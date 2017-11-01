using System.Collections.Generic;
using System.Threading.Tasks;
using Unearth.Core;
using Unearth.Dns;

namespace Unearth.Kafka
{
    public class KafkaLocator : ServiceLocator<KafkaService>
    {
        public Task<KafkaService> Locate()
        {
            return Locate(null);
        }

        public override Task<KafkaService> Locate(string serviceName)
        {
            string longName;
            if (string.IsNullOrWhiteSpace(serviceName))
                longName = serviceName = "_kafka";
            else
                longName = $"{serviceName.ToLowerInvariant()}._kafka";

            // get name of service to resolve
            ServiceDnsName name = new ServiceDnsName
            {
                Domain = ServiceDomain,
                ServiceName = serviceName,
                Protocol = "kafka",
                DnsName = string.IsNullOrEmpty(ServiceDomain)
                    ? serviceName.ToLowerInvariant()
                    : $"{longName}._tcp.{ServiceDomain}"
            };

            return Locate(name, _ => ServiceLookup.SrvTxt(name, KafkaServiceFactory));
        }

        private static KafkaService KafkaServiceFactory(ServiceDnsName name, IEnumerable<DnsEntry> dnsEntries)
        {
            return new KafkaService(dnsEntries)
            {
                Name = name.ServiceName,
                Protocol = name.Protocol,
                Decryptor = { ServiceDomain = name.Domain }
            };
        }
    }
}
