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
        [Obsolete("Use `lookupParameters` option on Lookup() methods", true)]
        public bool NoText { get; set; }

        public override Task<GrpcService> Locate(string serviceName)
        {
            return Locate(serviceName, false);
        }

        public Task<GrpcService> Locate(string serviceName, bool lookupParameters)
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

            // build lookup object
            var serviceLookup = GrpcServiceLookup.Create(lookupParameters);
            serviceLookup.Factory = GrpcServiceFactory;
            serviceLookup.Name = name;

            // preform lookup and return
            return Locate(name, serviceLookup.LocateFunction);
        }

        public Task<GrpcService> Locate(string serviceName, ChannelCredentials credentials)
        {
            return Locate(serviceName, false, credentials);
        }

        public async Task<GrpcService> Locate(string serviceName, bool lookupParameters, ChannelCredentials credentials)
        {
            await new SynchronizationContextRemover();

            GrpcService service = await Locate(serviceName, lookupParameters);
            service.Credentials = credentials;

            return service;
        }

        protected GrpcService GrpcServiceFactory(ServiceDnsName name, IEnumerable<DnsEntry> dnsEntries)
        {
            ApplyDnsRandomizer(ref dnsEntries);

            return new GrpcService(dnsEntries)
            {
                Name = name.ServiceName,
                Decryptor = { ServiceDomain = name.Domain }
            };
        }

        private class GrpcServiceLookup
        {
            internal static GrpcServiceLookup Create(bool withText)
            {
                return (withText) ? new GrpcServiceLookupWithText() : new GrpcServiceLookup();
            }

            internal ServiceDnsName Name { get; set; }

            internal ServiceFactory<GrpcService> Factory { get; set; }

            internal virtual SrvLookup<GrpcService> LocateFunction(string dnsName) => ServiceLookup.Srv(Name, Factory);
        }

        private class GrpcServiceLookupWithText : GrpcServiceLookup
        {
            internal override SrvLookup<GrpcService> LocateFunction(string dnsName) => ServiceLookup.SrvTxt(Name, Factory);
        }
    }
}
