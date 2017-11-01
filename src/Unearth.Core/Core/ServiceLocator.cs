using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Unearth.Dns;

namespace Unearth.Core
{
    public abstract class ServiceLocator<TService> where TService : class, IServiceInfo
    {
        protected ServiceLocator()
        {
            _serviceDomain = Environment.GetEnvironmentVariable("SERVICE_DOMAIN");
            Cache = new ConcurrentDictionary<string, SrvLookup<TService>>(StringComparer.OrdinalIgnoreCase);
        }

        protected ConcurrentDictionary<string, SrvLookup<TService>> Cache { get; }

        public void Clear() => Cache.Clear();

        public string ServiceDomain
        {
            get => _serviceDomain;
            set
            {
                _serviceDomain = value;
                Cache.Clear();
            }
        }
        private string _serviceDomain;

        public virtual bool NoCache
        {
            get => _noCache;
            set
            {
                _noCache = value;
                if (value) Cache.Clear();
            }
        }
        private bool _noCache;

        public abstract Task<TService> Locate(string serviceName);

        protected async Task<TService> Locate(ServiceDnsName name, Func<string, SrvLookup<TService>> locateFunc)
        {
            var task = (NoCache) ? LocateNow(name, locateFunc) : LocateCached(name, locateFunc);
            return await task.ConfigureAwait(false);
        }

        private async Task<TService> LocateNow(ServiceDnsName name, Func<string, SrvLookup<TService>> locateFunc)
        {
            return (await locateFunc(name.DnsName).Task.ConfigureAwait(false)).Invoke();
        }

        private async Task<TService> LocateCached(ServiceDnsName name, Func<string, SrvLookup<TService>> locateFunc)
        {
            TService service = null;
            void SetRetry()
            {
                Cache.TryRemove(name.DnsName, out var dummy);
                service = null;
            }

            int trys = 3;  // max retries
            while (service == null && trys-- > 0)
            {
                try
                {
                    // check cache & expire time
                    service = (await Cache.GetOrAdd(name.DnsName, locateFunc).Task.ConfigureAwait(false)).Invoke();
                    if (service.Expires < DateTime.UtcNow) SetRetry();
                }
                catch (DnsResolveException)
                {
                    SetRetry();    // Wait 1 sec & try again
                    await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                }
            }

            return service;
        }
    }
}
