using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Grpc.Core;
using Unearth.Core;
using Unearth.Dns;

namespace Unearth.Grpc
{
    public class GrpcLocator : ServiceLocator<GrpcService>
    {
        public bool NoText
        {
            get => _noText;
            set
            {
                _noText = value;
                Cache.Clear();
            }
        }
        private bool _noText;

        public override Task<GrpcService> Locate(string serviceName)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
                throw new ArgumentNullException(nameof(serviceName));

            // get name of service to resolve
            var name = new ServiceDnsName
            {
                Domain = ServiceDomain,
                ServiceName = serviceName,
                Protocol = "grpc",
                DnsName = string.IsNullOrEmpty(ServiceDomain)
                    ? serviceName.ToLowerInvariant()
                    : $"{serviceName.ToLowerInvariant()}._grpc._tcp.{ServiceDomain}"
            };

            // actual lookup function
            SrvLookup<GrpcService> locateFunc(string k)
            {
                return (_noText)
                    ? ServiceLookup.Srv(name, GrpcServiceFactory)
                    : ServiceLookup.SrvTxt(name, GrpcServiceFactory);
            }

            // preform lookup and return
            return Locate(name, locateFunc);
        }

        public async Task<GrpcService> Locate(string serviceName, ChannelCredentials credentials)
        {
            GrpcService service = await Locate(serviceName).ConfigureAwait(false);
            service.Credentials = credentials;

            return service;
        }

        private static GrpcService GrpcServiceFactory(ServiceDnsName name, IEnumerable<DnsEntry> dnsEntries)
        {
            return new GrpcService(dnsEntries) { Name = name.ServiceName };
        }
    }
}
