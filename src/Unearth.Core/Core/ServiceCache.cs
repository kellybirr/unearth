using System;
using System.Collections.Generic;

namespace Unearth.Core
{
    public class ServiceCache<TService> where TService : class, IServiceInfo
    {
        private readonly object _syncLock = new object();

        private readonly Dictionary<string, SrvLookup<TService>> _bag
            = new Dictionary<string, SrvLookup<TService>>(StringComparer.OrdinalIgnoreCase);

        public ServiceLocator<TService> Owner { get; }

        public event EventHandler<ServiceCacheUpdateEventArgs> Updated;

        public ServiceCache(ServiceLocator<TService> owner)
        {
            Owner = owner;
        }

        public SrvLookup<TService> GetOrAdd(string serviceName, SrvLookupFunction<TService> lookupFunc)
        {
            // ReSharper disable once InlineOutVariableDeclaration
            SrvLookup<TService> result;

            // ReSharper disable once InconsistentlySynchronizedField
            if (_bag.TryGetValue(serviceName, out result) && (result != null))
                return result;

            lock (_syncLock)
            {
                if (_bag.TryGetValue(serviceName, out result) && (result != null))
                    return result;

                result = lookupFunc(serviceName);
                _bag[serviceName] = result;

                // fire update event
                Updated?.Invoke(this, new ServiceCacheUpdateEventArgs { DnsName = serviceName });

                return result;
            }
        }

        public SrvLookup<TService> CheckAndUpdate(string serviceName, SrvLookup<TService> oldValue, SrvLookupFunction<TService> lookupFunc)
        {
            lock (_syncLock)
            {
                // ReSharper disable once InlineOutVariableDeclaration
                SrvLookup<TService> result;

                // if already updated
                if (_bag.TryGetValue(serviceName, out result) && (! ReferenceEquals(result, oldValue)))
                    return result;

                result = lookupFunc(serviceName);
                _bag[serviceName] = result;

                // fire updated event
                Updated?.Invoke(this, new ServiceCacheUpdateEventArgs {DnsName = serviceName});

                return result;
            }
        }

        public void Remove(string serviceName)
        {
            lock (_syncLock)
                _bag.Remove(serviceName);
        }

        public void Clear()
        {
            lock (_syncLock)
                _bag.Clear();
        }
    }

    public class ServiceCacheUpdateEventArgs : EventArgs
    {
        public string DnsName { get; set; }
    }
}
