using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Unearth.Dns
{
    public class DnsQuery : IDnsQuery
    {
        private readonly IDnsQuery _dns;

        public DnsQuery(string query, DnsRecordType type)
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentNullException(nameof(query));

            if (query.Length > 255)
                throw new ArgumentOutOfRangeException(nameof(query));

#if (NETSTANDARD2_0)
            _dns = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? (IDnsQuery)new Windows.WDnsQuery(query, type)
                : (IDnsQuery)new Linux.LDnsQuery(query, type);

#else
            _dns = new Windows.WDnsQuery(query, type);
#endif
        }

        public DnsQueryStatus QueryStatus => _dns.QueryStatus;

        public DnsRecordType Type => _dns.Type;

        public string Query => _dns.Query;

        public DnsEntry[] AllRecords => _dns.AllRecords;

        public async Task<DnsEntry[]> TryResolve()
        {
            return await _dns.TryResolve().ConfigureAwait(false);
        }

        public async Task<DnsEntry[]> Resolve()
        {
            var res = await _dns.TryResolve().ConfigureAwait(false);

            if (res?.Length == 0)
                throw new DnsResolveException(Query);

            return res;
        }

        public DnsHostEntry[] GetHostEntries(DnsMailExchangeEntry mx)
        {
            if (_dns.AllRecords == null)
                throw new InvalidOperationException("Must 'Resolve()' before getting hosts");

            return (from r in _dns.AllRecords
                    let host = r as DnsHostEntry
                    where host?.Name == mx.Exchanger
                    select host
                    ).ToArray();
        }

        public DnsHostEntry[] GetHostEntries(DnsServiceEntry srv)
        {
            if (_dns.AllRecords == null)
                throw new InvalidOperationException("Must 'Resolve()' before getting hosts");

            return (from r in _dns.AllRecords
                let host = r as DnsHostEntry
                where host?.Name == srv.Host
                select host
            ).ToArray();
        }
    }

    internal interface IDnsQuery
    {
        DnsQueryStatus QueryStatus { get; }
        DnsRecordType Type { get; }
        string Query { get; }
        DnsEntry[] AllRecords { get; }
        Task<DnsEntry[]> TryResolve();
    }

    public enum DnsQueryStatus
    {
        Unknown,
        NotFound,
        Found,
        Timeout
    }

    public enum DnsRecordType
    {
        A = 1,
        NS = 2,
        CNAME = 5,
        PTR = 12,
        MX = 15,
        TXT = 16,
        AAAA = 28,
        SRV = 33
    }

    public class DnsResolveException : Exception
    {
        public DnsResolveException(string query) : base($"`{query}` not found")
        { }
    }

    public class DnsTimeoutException : Exception
    {
        public DnsTimeoutException(string query) : base($"`{query}` lookup timed out")
        { }
    }
}
