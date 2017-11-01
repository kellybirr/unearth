using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Unearth.Core;
using Unearth.Dns;

namespace Unearth.Grpc
{
    public class GrpcService : ServiceBase<GrpcEndpoint>
    {
        public override string Protocol
        {
            get => "grpc";
            set { }
        }

        public ChannelCredentials Credentials { get; set; } = ChannelCredentials.Insecure;

        public GrpcService() { }

        public GrpcService(IEnumerable<GrpcEndpoint> endpoints) : base(endpoints) { }

        public GrpcService(IEnumerable<DnsEntry> dnsEntries) : base(dnsEntries) { }

        public GrpcService(IEnumerable<Uri> uris) : base(UrisToEndpoints(uris)) { }

        public async Task<Channel> Connect(TimeSpan timeout)
        {
            if (Endpoints == null || Endpoints.Count == 0)
                throw new IndexOutOfRangeException("No Endpoints Specified");

            var exceptions = new List<Exception>();
            foreach (GrpcEndpoint ep in Endpoints)
            {   // try connecting to servers in order
                try
                {
                    var channel = ep.GetChannel(Credentials);

                    DateTime timeoutUtc = DateTime.UtcNow.Add(timeout);
                    Task connectTask = channel.ConnectAsync(timeoutUtc);
                    await connectTask.ConfigureAwait(false);

                    if (connectTask.IsCompleted && channel.State == ChannelState.Ready)
                        return channel;
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }

            // throw exception
            throw new GrpcConnectException(exceptions);
        }

        public async Task<Channel[]> ConnectAll(TimeSpan timeout)
        {
            var tasks = new Task[Endpoints.Count];
            var channels = new List<Channel>();
            var exceptions = new List<Exception>();

            for (var i = 0; i < Endpoints.Count; i++)
            {   // try connecting to servers in order
                DateTime timeoutUtc = DateTime.UtcNow.Add(timeout);
                var channel = Endpoints[i].GetChannel(Credentials);
                tasks[i] = channel.ConnectAsync(timeoutUtc).ContinueWith(t =>
                {   // check result of connect action
                    if (t.IsCompleted && channel.State == ChannelState.Ready)
                        lock (channels) channels.Add(channel);

                    if (t.Exception != null)
                    {
                        lock (exceptions)
                            exceptions.AddRange(t.Exception.InnerExceptions);

                        t.Exception.Handle(_ => true);
                    }
                });
            }

            // await all tasks
            await Task.WhenAll(tasks).ConfigureAwait(false);

           // return open channels
            if (channels.Count > 0)
                return channels.ToArray();

            // throw exception
            throw new GrpcConnectException(exceptions);
        }

        public async Task Execute(Func<Channel, Task> action, TimeSpan connectTimeout)
        {
            Channel channel = null;
            try
            {
                channel = await Connect(connectTimeout);
                await action(channel);
            }
            finally
            {
                if (channel != null)
                    await channel.ShutdownAsync();
            }
        }

        public async Task<TResult> Execute<TResult>(Func<Channel, Task<TResult>> action, TimeSpan connectTimeout)
        {
            Channel channel = null;
            try
            {
                channel = await Connect(connectTimeout);
                return await action(channel);
            }
            finally
            {
                if (channel != null)
                    await channel.ShutdownAsync();
            }
        }

        public IEnumerable<Uri> Uris =>
            Endpoints.Select(ep => new Uri($"grpc://{ep.Host}:{ep.Port}", UriKind.Absolute));

        private static IEnumerable<GrpcEndpoint> UrisToEndpoints(IEnumerable<Uri> uris)
        {
            int priority = 0;
            foreach (Uri uri in uris)
            {
                if (uri.Scheme != "grpc")
                    throw new ArgumentOutOfRangeException(nameof(uri));

                if (string.IsNullOrWhiteSpace(uri.Host))
                    throw new ArgumentOutOfRangeException(nameof(uri));

                if (uri.Port == 0)
                    throw new ArgumentOutOfRangeException(nameof(uri));

                yield return new GrpcEndpoint
                {
                    Priority = ++priority,
                    Host = uri.Host,
                    Port = uri.Port,
                    Expires = DateTime.UtcNow.AddHours(1)
                };
            }
        }
    }

    public class GrpcEndpoint : ServiceEndpoint
    {
        public GrpcEndpoint() { }

        public GrpcEndpoint(string host, int port) : base(host, port) { }

        public GrpcEndpoint(DnsServiceEntry srv) : base(srv) { }

        public Channel GetChannel(ChannelCredentials credentials)
        {
            return new Channel(Host, Port, credentials);
        }
    }

    public class GrpcConnectException : AggregateException
    {
        public GrpcConnectException()
            : base("Unable to open any channel to service")
        { }

        public GrpcConnectException(IEnumerable<Exception> exceptions)
            : base("Unable to open any channel to service", exceptions)
        { }
    }
}
