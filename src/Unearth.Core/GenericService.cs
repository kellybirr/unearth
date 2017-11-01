using System.Collections.Generic;
using Unearth.Core;
using Unearth.Dns;

namespace Unearth
{
    public class GenericService : ServiceBase<ServiceEndpoint>
    {
        public GenericService()
        { }

        public GenericService(IEnumerable<ServiceEndpoint> endpoints) : base(endpoints)
        { }

        public GenericService(IEnumerable<DnsEntry> dnsEntries) : base(dnsEntries)
        { }
    }
}
