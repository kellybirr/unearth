using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unearth.Core;
using Unearth.Dns;

namespace Unearth
{
    public class GenericLocator : ServiceLocator<GenericService>
    {
        public override Task<GenericService> Locate(string serviceName)
        {
            return Locate(serviceName, IpProtocol.TCP);
        }

        public Task<GenericService> Locate(string serviceName, IpProtocol protocol)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
                throw new ArgumentNullException(nameof(serviceName));

            // get name of service to resolve
            string sProtocol = protocol.ToString().ToLowerInvariant();
            ServiceDnsName name = new ServiceDnsName
            {
                Domain = ServiceDomain,
                ServiceName = serviceName,
                Protocol = sProtocol,
                DnsName = string.IsNullOrEmpty(ServiceDomain)
                    ? serviceName.ToLowerInvariant()
                    : $"{serviceName.ToLowerInvariant()}._{sProtocol}.{ServiceDomain}"
            };

            return Locate(name, _ => ServiceLookup.SrvTxt(name, MyServiceFactory));
        }

        private GenericService MyServiceFactory(ServiceDnsName name, IEnumerable<DnsEntry> dnsEntries)
        {
            ApplyDnsRandomizer(ref dnsEntries);

            return new GenericService(dnsEntries)
            {
                Name = name.ServiceName,
                Protocol = name.Protocol,
                Decryptor = { ServiceDomain = name.Domain }
            };
        }
    }
}
