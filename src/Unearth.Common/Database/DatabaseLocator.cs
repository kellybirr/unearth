using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unearth.Core;
using Unearth.Dns;

namespace Unearth.Database
{
    public class DatabaseLocator : ServiceLocator<DatabaseService>
    {
        public override Task<DatabaseService> Locate(string serviceName)
        {
            return Locate(serviceName, DatabaseProtocol.Sql);
        }

        public Task<DatabaseService> Locate(string serviceName, DatabaseProtocol protocol)
        {
            return Locate(serviceName, protocol.ToString());
        }

        public Task<DatabaseService> Locate(DatabaseProtocol protocol)
        {
            // lower-invariant protocol & service name
            string protocolName = protocol.ToString().ToLowerInvariant();
            string serviceName = $"_{protocolName}";

            // get name of service to resolve
            ServiceDnsName name = new ServiceDnsName
            {
                Domain = ServiceDomain,
                ServiceName = serviceName,
                Protocol = protocolName,
                DnsName = string.IsNullOrEmpty(ServiceDomain)
                    ? serviceName.ToLowerInvariant()
                    : $"{serviceName.ToLowerInvariant()}._tcp.{ServiceDomain}"
            };

            // locate and return
            return Locate(name, _ => ServiceLookup.SrvTxt(name, DbServiceFactory));
        }

        public Task<DatabaseService> Locate(string serviceName, string protocol)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
                throw new ArgumentNullException(nameof(serviceName));

            if (string.IsNullOrWhiteSpace(protocol))
                throw new ArgumentNullException(nameof(protocol));

            // lower-invariant protocol
            protocol = protocol.ToLowerInvariant();

            // get name of service to resolve
            ServiceDnsName name = new ServiceDnsName
            {
                Domain = ServiceDomain,
                ServiceName = serviceName,
                Protocol = protocol,
                DnsName = string.IsNullOrEmpty(ServiceDomain)
                    ? serviceName.ToLowerInvariant()
                    : $"{serviceName.ToLowerInvariant()}._{protocol}._tcp.{ServiceDomain}"
            };

            // locate and return
            return Locate(name, _ => ServiceLookup.SrvTxt(name, DbServiceFactory));
        }

        private DatabaseService DbServiceFactory(ServiceDnsName name, IEnumerable<DnsEntry> dnsEntries)
        {
            ApplyDnsRandomizer(ref dnsEntries);

            return new DatabaseService(dnsEntries)
            {
                Name = name.ServiceName,
                Protocol = name.Protocol,
                Decryptor = {ServiceDomain = name.Domain}
            };
        }
    }

    public enum DatabaseProtocol
    {
        Sql,
        MongoDb,
        Redis
    }
}
