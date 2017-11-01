using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Unearth.Dns;

#if (NETSTANDARD2_0)
using Microsoft.Extensions.Primitives;
#endif

namespace Unearth.WebApi
{
    public class WebApiService : GenericService
    {
        public const string Http = "http";
        public const string Https = "https";

        private Dictionary<string, string> _headers;
        private readonly object _syncLock = new object();

        public WebApiService()
        { }

        public WebApiService(IEnumerable<ServiceEndpoint> endpoints) : base(endpoints)
        { }

        public WebApiService(IEnumerable<DnsEntry> dnsEntries) : base(dnsEntries)
        { }

        public IDictionary<string, string> Headers
        {
            get
            {
                if (_headers == null) PopulateHeaders();
                return _headers;
            }
        }

        [SuppressMessage("ReSharper", "RedundantBaseQualifier")]
        private void PopulateHeaders()
        {
            lock (_syncLock)
            {
                if (_headers != null) return;
                _headers = new Dictionary<string, string>();

                // move ! parameters to headers
                foreach (KeyValuePair<string, StringValues> kv in base.Parameters)
                {
                    if (kv.Key.StartsWith("!"))
                        _headers.Add(kv.Key.Substring(1), kv.Value);
                }
            }
        }

        public IEnumerable<Uri> Uris
        {
            get
            {
                foreach (ServiceEndpoint ep in Endpoints)
                {
                    // get scheme
                    if (!Parameters.TryGetString("Scheme", out string scheme))
                    {
                        scheme = (ep.Port == 443) ? Https : Http;
                        if (scheme == Http && Parameters.TryGetString("Secure", out string secureStr))
                            if (bool.TryParse(secureStr, out bool isSecure))
                                scheme = isSecure ? Https : Http;
                    }

                    // start building w/ scheme
                    var sb = new StringBuilder(scheme);
                    sb.Append("://");

                    // check for user/pass
                    if (Parameters.TryGetString("@User", out string userName))
                    {
                        sb.Append(userName);
                        if (Parameters.TryGetString("@Password", out string password))
                            sb.AppendFormat(":{0}", password);

                        sb.Append('@');
                    }

                    // append host
                    sb.Append(ep.Host);

                    // check port for non-default
                    if (!((scheme == Http && ep.Port == 80) || (scheme == Https && ep.Port == 443)))
                        sb.AppendFormat(":{0}", ep.Port);

                    // append path
                    if (Parameters.TryGetString("Path", out string path))
                    {
                        if (!path.StartsWith("/")) sb.Append('/');
                        sb.Append(path);
                    }

                    // return
                    var uri = new Uri(sb.ToString(), UriKind.Absolute);
                    yield return uri;
                }
            }
        }
    }
}
