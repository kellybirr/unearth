using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unearth.Core;
using Unearth.Dns;

namespace Unearth.WebApi
{
    public class WebApiLocator : ServiceLocator<WebApiService>
    {
        public override Task<WebApiService> Locate(string serviceName)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
                throw new ArgumentNullException(nameof(serviceName));

            // get name of service to resolve
            ServiceDnsName name = new ServiceDnsName
            {
                Domain = ServiceDomain,
                ServiceName = serviceName,
                Protocol = "http",
                DnsName = string.IsNullOrEmpty(ServiceDomain)
                    ? serviceName.ToLowerInvariant()
                    : $"{serviceName.ToLowerInvariant()}._http._tcp.{ServiceDomain}"
            };

            return Locate(name, _ => ServiceLookup.SrvTxt(name, WebApiServiceFactory));
        }

        private WebApiService WebApiServiceFactory(ServiceDnsName name, IEnumerable<DnsEntry> dnsEntries)
        {
            ApplyDnsRandomizer(ref dnsEntries);

            return new WebApiService(dnsEntries)
            {
                Name = name.ServiceName,
                Protocol = name.Protocol,
                Decryptor = {ServiceDomain = name.Domain}
            };
        }
    }
}
