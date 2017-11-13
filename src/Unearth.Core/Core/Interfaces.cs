using System;

namespace Unearth.Core
{
    public interface IServiceInfo
    {
        string Name { get; }
        string Protocol { get; }
        DateTime Expires { get; }
    }

    public delegate SrvLookup<TService> SrvLookupFunction<TService>(string dnsName)
        where TService : class, IServiceInfo;
}
