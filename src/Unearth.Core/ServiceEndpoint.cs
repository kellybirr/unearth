using System;

namespace Unearth
{
    public class ServiceEndpoint
    {
        public ServiceEndpoint() { }

        public ServiceEndpoint(string host, int port)
        {
            Host = host;
            Port = port;

            Expires = DateTime.UtcNow;
        }

        public ServiceEndpoint(Dns.DnsServiceEntry srv)
        {
            Host = srv.Host;
            Port = srv.Port;
            Priority = srv.Priority;
            Weight = srv.Weight;
            Expires = srv.Expires;
        }

        public string Host { get; set; }
        public int Priority { get; set; }
        public int Weight { get; set; }
        public int Port { get; set; }
        public DateTime Expires { get; set; }

        public override string ToString() => $"{Host}:{Port}";

        public static T Create<T>(Dns.DnsServiceEntry srv)
            where T : ServiceEndpoint, new()
        {
            return new T
            {
                Host = srv.Host,
                Port = srv.Port,
                Priority = srv.Priority,
                Weight = srv.Weight,
                Expires = srv.Expires
            };
        }
    }
}
