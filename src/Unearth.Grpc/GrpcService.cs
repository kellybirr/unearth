using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Unearth.Core;
using Unearth.Dns;

namespace Unearth.Grpc
{
    public class GrpcService : ServiceBase<GrpcEndpoint>
    {
        private static readonly TimeSpan Default_TryNextAfter = TimeSpan.FromMilliseconds(200);        

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

        public Task<Channel> Connect(TimeSpan timeout)
        {
            return Connect(timeout, Default_TryNextAfter);
        }

        public async Task<Channel> Connect(TimeSpan timeout, TimeSpan tryNextAfter)
        {
            await new SynchronizationContextRemover();

            if (Endpoints == null || Endpoints.Count == 0)
                throw new IndexOutOfRangeException("No Endpoints Specified");

            // if infinite timespan
            if (tryNextAfter == Timeout.InfiniteTimeSpan)
                return await ConnectSlow(timeout);

            var tasks = new List<Task>();            
            var exceptions = new List<Exception>();

            using (var cancel = new CancellationTokenSource())
            {
                Channel returnChannel = null;
                object resultLock = new object();

                foreach (var ep in Endpoints)
                {
                    // try connecting to servers in order
                    DateTime timeoutUtc = DateTime.UtcNow.Add(timeout);
                    var channel = ep.GetChannel(Credentials);
                    tasks.Add(channel.ConnectAsync(timeoutUtc).ContinueWith(async t =>
                    {
                        // check result of connect action
                        if (channel.State == ChannelState.Ready)
                        {
                            lock (resultLock)
                            {
                                if (t.IsCompleted && (returnChannel == null))
                                    returnChannel = channel;    // winner
                            }

                            if (channel != returnChannel) // not the winner
                                await channel.ShutdownAsync();
                        }

                        if (t.Exception != null)
                        {
                            lock (exceptions)
                                exceptions.AddRange(t.Exception.InnerExceptions);

                            t.Exception.Handle(_ => true);
                        }
                    }, cancel.Token));

                    // wait for connect or delay
                    if (tryNextAfter > TimeSpan.Zero)
                    {
                        try
                        {
                            tasks.Add( Task.Delay(tryNextAfter, cancel.Token) );
                            Task completedTask = await Task.WhenAny(tasks);

                            tasks.Remove(completedTask);
                        }
                        catch (TaskCanceledException) { }
                    }

                    // return if got a connect
                    if (returnChannel != null)
                    {
                        if (! cancel.IsCancellationRequested)
                            cancel.Cancel();

                        return returnChannel;
                    }
                }

                // try again after loop
                try { await Task.WhenAny(tasks); }
                catch (TaskCanceledException) { }

                if (returnChannel != null)
                    return returnChannel;

                // last try
                try { await Task.WhenAll(tasks); }
                catch (TaskCanceledException) { }

                if (returnChannel != null)
                    return returnChannel;
            }

            // throw exception
            throw new GrpcConnectException(exceptions);
        }

        private async Task<Channel> ConnectSlow(TimeSpan timeout)
        {
            var exceptions = new List<Exception>();
            foreach (GrpcEndpoint ep in Endpoints)
            {   // try connecting to servers in order
                try
                {
                    var channel = ep.GetChannel(Credentials);

                    DateTime timeoutUtc = DateTime.UtcNow.Add(timeout);
                    Task connectTask = channel.ConnectAsync(timeoutUtc);
                    await connectTask;

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
            await new SynchronizationContextRemover();

            if (Endpoints == null || Endpoints.Count == 0)
                throw new IndexOutOfRangeException("No Endpoints Specified");

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
            await Task.WhenAll(tasks);

           // return open channels
            if (channels.Count > 0)
                return channels.ToArray();

            // throw exception
            throw new GrpcConnectException(exceptions);
        }

        public Task Execute(Func<Channel, Task> action, TimeSpan connectTimeout)
        {
            return Execute(action, connectTimeout, Default_TryNextAfter);
        }

        public async Task Execute(Func<Channel, Task> action, TimeSpan connectTimeout, TimeSpan tryNextAfter)
        {
            await new SynchronizationContextRemover();

            Channel channel = null;
            try
            {
                channel = await Connect(connectTimeout, tryNextAfter);
                await action(channel);
            }
            finally
            {
                if (channel != null)
                    await channel.ShutdownAsync();
            }
        }

        public Task<TResult> Execute<TResult>(Func<Channel, Task<TResult>> action, TimeSpan connectTimeout)
        {
            return Execute(action, connectTimeout, Default_TryNextAfter);
        }

        public async Task<TResult> Execute<TResult>(Func<Channel, Task<TResult>> action, TimeSpan connectTimeout, TimeSpan tryNextAfter)
        {
            await new SynchronizationContextRemover();

            Channel channel = null;
            try
            {
                channel = await Connect(connectTimeout, tryNextAfter);
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
                    Expires = DateTime.UtcNow.AddMinutes(1)
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
