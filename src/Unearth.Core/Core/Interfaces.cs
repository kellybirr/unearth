using System;

namespace Unearth.Core
{
    public interface IServiceInfo
    {
        string Name { get; }
        string Protocol { get; }
        DateTime Expires { get; }
    }
}
