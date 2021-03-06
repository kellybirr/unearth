﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unearth.Dns;

namespace Unearth.Core
{
    public abstract class ServiceLocator<TService> where TService : class, IServiceInfo
    {
        private delegate Task<TService> LocateDelegate(ServiceDnsName name, SrvLookupFunction<TService> locateFunc);

        protected ServiceLocator()
        {
            _serviceDomain = Environment.GetEnvironmentVariable("SERVICE_DOMAIN");
            Cache = new ServiceCache<TService>(this);
        }

        protected ServiceCache<TService> Cache { get; }

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

        public bool Randomize { get; set; }

        public abstract Task<TService> Locate(string serviceName);

        protected async Task<TService> Locate(ServiceDnsName name, SrvLookupFunction<TService> locateFunc)
        {
            const int MAX_TRIES = 4, WAIT_AFTER = 2;

            await new SynchronizationContextRemover();

            int trys = 0; // max retries
            TService service = null;
            DnsResolveException lastException = null;
            while (service == null && ++trys <= MAX_TRIES)
            {
                if (trys > WAIT_AFTER) // Wait 1 sec
                    await Task.Delay(TimeSpan.FromSeconds(1));

                try
                {
                    LocateDelegate locator = (NoCache) ? (LocateDelegate)LocateNow : LocateCached;
                    service = await locator(name, locateFunc);
                }
                catch (DnsResolveException dex)
                {
                    lastException = dex;
                    service = null;
                }
            }

            if (service == null)
                throw lastException ?? new DnsResolveException(name.DnsName);

            return service;
        }

        private async Task<TService> LocateNow(ServiceDnsName name, SrvLookupFunction<TService> locateFunc)
        {
            Task<Func<TService>> factoryTask = locateFunc(name.DnsName).Task;
            Func<TService> factory = await factoryTask;

            return factory.Invoke();
        }

        private async Task<TService> LocateCached(ServiceDnsName name, SrvLookupFunction<TService> locateFunc)
        {
            try
            {
                // get lookup and factory
                SrvLookup<TService> cachedLookup = Cache.GetOrAdd(name.DnsName, locateFunc);
                Func<TService> factory = await cachedLookup.Task;

                // check cache & expire time
                TService service = factory.Invoke();
                if (service.Expires < DateTime.UtcNow)
                {
                    // update lookup, factory and service
                    SrvLookup<TService> newLookup = Cache.CheckAndUpdate(name.DnsName, cachedLookup, locateFunc);
                    factory = await newLookup.Task;
                    service = factory.Invoke();
                }

                return service;
            }
            catch (DnsResolveException)
            {
                Cache.Remove(name.DnsName);
                throw;
            }
        }

        protected void ApplyDnsRandomizer(ref IEnumerable<DnsEntry> dnsEntries)
        {            
            if (Randomize)  // re-sort pseudo-randomly, honoring preference/priority
                dnsEntries = dnsEntries.OrderBy(e => (e as IOrderedDnsEntry2)?.Randomizer);
        }
    }
}
