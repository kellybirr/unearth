using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unearth.Dns;
using CTask = System.Threading.Tasks.Task;

namespace Unearth.Core
{
    public delegate TService ServiceFactory<out TService>(ServiceDnsName name, IEnumerable<DnsEntry> dnsEntries);

    public static class ServiceLookup
    {
        public static SrvLookup<TService> Srv<TService>(ServiceDnsName name, ServiceFactory<TService> factory)
        {
            return new SrvLookup<TService>(name, factory).Start();
        }

        public static SrvLookup<TService> SrvTxt<TService>(ServiceDnsName name, ServiceFactory<TService> factory)
        {
            return new SrvTxtLookup<TService>(name, factory).Start();
        }

        public static SrvLookup<TService> Txt<TService>(ServiceDnsName name, ServiceFactory<TService> factory)
        {
            return new TxtLookup<TService>(name, factory).Start();
        }
    }

    public class SrvLookup<TService>
    {
        protected TaskCompletionSource<Func<TService>> Completion { get; }
        protected ServiceFactory<TService> Factory { get; }
        protected DnsEntry[] DnsEntries { get; set; }
        protected DnsQuery SrvQuery { get; }
        protected ServiceDnsName Name { get; }

        public virtual Task<Func<TService>> Task => Completion.Task;

        public SrvLookup(ServiceDnsName name, ServiceFactory<TService> factory)
        {
            Name = name;
            Factory = factory;
            SrvQuery = new DnsQuery(name.DnsName, DnsRecordType.SRV);
            Completion = new TaskCompletionSource<Func<TService>>(this);
        }

        protected TService ResultFactory() => Factory(Name, DnsEntries);

        public virtual SrvLookup<TService> Start()
        {
            // Query DNS servers
            SrvQuery.Resolve().ContinueWith(t =>
            {
                try
                {
                    DnsEntries = t.Result;
                    Completion.SetResult(ResultFactory);
                }
                catch (Exception ex)
                {
                    Completion.SetException(ex);
                }
            });

            return this;
        }
    }

    public class SrvTxtLookup<TService> : SrvLookup<TService>
    {
        private DnsQuery TxtQuery { get; }

        public SrvTxtLookup(ServiceDnsName name, ServiceFactory<TService> factory)
            : base(name, factory)
        {
            TxtQuery = new DnsQuery(name.DnsName, DnsRecordType.TXT);
        }

        public override SrvLookup<TService> Start()
        {
            // Query DNS servers (SRV Required, TXT Optional)
            var tasks = new[] { SrvQuery.Resolve(), TxtQuery.TryResolve() };
            CTask.WhenAll(tasks).ContinueWith(t =>
            {
                try
                {
                    DnsEntries = t.Result[0].Union(t.Result[1]).ToArray();
                    Completion.SetResult(ResultFactory);
                }
                catch (Exception ex)
                {
                    Completion.SetException(ex);
                }
            });

            return this;
        }
    }

    public class TxtLookup<TService> : SrvLookup<TService>
    {
        private DnsQuery TxtQuery { get; }

        public TxtLookup(ServiceDnsName name, ServiceFactory<TService> factory)
            : base(name, factory)
        {
            TxtQuery = new DnsQuery(name.DnsName, DnsRecordType.TXT);
        }

        public override SrvLookup<TService> Start()
        {
            // try to resolve - optional
            TxtQuery.TryResolve().ContinueWith(t =>
            {
                try
                {
                    DnsEntries = t.Result;
                    Completion.SetResult(ResultFactory);
                }
                catch (Exception ex)
                {
                    Completion.SetException(ex);
                }
            });

            return this;
        }
    }
}
