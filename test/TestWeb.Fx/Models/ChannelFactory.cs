using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Grpc.Core;
using Unearth.Grpc;

namespace TestWeb.Fx.Models
{
    public static class ChannelFactory
    {
        //private static readonly GrpcLocator _locator = new GrpcLocator() {ServiceDomain = "dev-test.rnd.ipzo.net"};
        private static readonly GrpcLocator _locator = new GrpcLocator();

        public static Channel GetChannel(string serviceName) => GetChannelAsync(serviceName).Result;

        public static async Task<Channel> GetChannelAsync(string serviceName)
        {
            string hostName = serviceName;
            if (!string.IsNullOrEmpty(hostName))
            {
                if (hostName[0] == '@')
                {
                    GrpcService service = await _locator.Locate(hostName.Substring(1), ChannelCredentials.Insecure).ConfigureAwait(false);
                    return await service.Connect(TimeSpan.FromSeconds(2));
                }

                var uri = new Uri(hostName);
                var channel = new Channel(uri.Host, uri.Port, ChannelCredentials.Insecure);
                //await channel.ConnectAsync(DateTime.UtcNow.AddSeconds(15));

                return channel;
            }

            throw new ArgumentException($"Service name does not appear in the configuration: {serviceName}",
                nameof(serviceName));
        }
    }
}