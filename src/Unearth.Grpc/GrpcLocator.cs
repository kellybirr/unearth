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
            GrpcServiceLookup serviceLookup = _noText
                ? new GrpcServiceLookup(name)
                : new GrpcServiceLookupWithText(name);

            // preform lookup and return
            return Locate(name, serviceLookup.LocateFunction);
        }

        public async Task<GrpcService> Locate(string serviceName, ChannelCredentials credentials)
        {
            await new SynchronizationContextRemover();

            GrpcService service = await Locate(serviceName);
            service.Credentials = credentials;

            return service;
        }

        private class GrpcServiceLookup
        {
            internal GrpcServiceLookup(ServiceDnsName name)
            {
                Name = name;
            }

            protected ServiceDnsName Name { get; }

            internal virtual SrvLookup<GrpcService> LocateFunction(string dnsName)
            {
                return ServiceLookup.Srv(Name, GrpcServiceFactory);
            }

            protected GrpcService GrpcServiceFactory(ServiceDnsName name, IEnumerable<DnsEntry> dnsEntries)
            {
                return new GrpcService(dnsEntries) { Name = name.ServiceName };
            }
        }

        private class GrpcServiceLookupWithText : GrpcServiceLookup
        {
            internal GrpcServiceLookupWithText(ServiceDnsName name) : base(name)
            { }

            internal override SrvLookup<GrpcService> LocateFunction(string dnsName)
            {
                return ServiceLookup.SrvTxt(Name, GrpcServiceFactory);
            }
        }
    }
}
