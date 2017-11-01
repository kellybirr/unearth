using System.Collections.Generic;
using System.Text;
using Confluent.Kafka;
using Unearth.Dns;

namespace Unearth.Kafka
{
    public class KafkaService : GenericService
    {
        public KafkaService() { }

        public KafkaService(IEnumerable<ServiceEndpoint> endpoints) : base(endpoints)
        { }

        public KafkaService(IEnumerable<DnsEntry> dnsEntries) : base(dnsEntries)
        { }

        public string Brokers
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
    }
}
