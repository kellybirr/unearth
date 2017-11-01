using System.Collections.Generic;
using System.Threading.Tasks;
using Unearth.Core;
using Unearth.Dns;

namespace Unearth.Smtp
{
    public class SmtpLocator : ServiceLocator<SmtpService>
    {
        public Task<SmtpService> Locate()
        {
            return Locate( null );
        }

        public override Task<SmtpService> Locate(string serviceName)
        {
            string longName;
            if (string.IsNullOrWhiteSpace(serviceName))
                longName = serviceName = "_smtp";
            else
                longName = $"{serviceName.ToLowerInvariant()}._smtp";

            // get name of service to resolve
            ServiceDnsName name = new ServiceDnsName
            {
                Domain = ServiceDomain,
                ServiceName = serviceName,
                Protocol = "smtp",
                DnsName = string.IsNullOrEmpty(ServiceDomain)
                    ? serviceName.ToLowerInvariant()
                    : $"{longName}._tcp.{ServiceDomain}"
            };

            return Locate(name, _ => ServiceLookup.SrvTxt(name, SmtpServiceFactory));
        }

        private static SmtpService SmtpServiceFactory(ServiceDnsName name, IEnumerable<DnsEntry> dnsEntries)
        {
            return new SmtpService(dnsEntries)
            {
                Name = name.ServiceName,
                Protocol = name.Protocol,
                Decryptor = { ServiceDomain = name.Domain }
            };
        }
    }
}
