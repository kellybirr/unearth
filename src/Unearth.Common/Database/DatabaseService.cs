using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Unearth.Dns;

namespace Unearth.Database
{
    public class DatabaseService : GenericService
    {
        private const string ADO = "ado", SQL = "sql";
        private const string MONGODB = "mongodb";
        private const string REDIS = "redis";

        public DatabaseService()
        { }

        public DatabaseService(IEnumerable<ServiceEndpoint> endpoints) : base(endpoints)
        { }

        public DatabaseService(IEnumerable<DnsEntry> dnsEntries) : base(dnsEntries)
        { }

        public DatabaseProtocol DatabaseProtocol
        {
            // ReSharper disable once ArrangeAccessorOwnerBody
            get => (DatabaseProtocol)Enum.Parse(typeof(DatabaseProtocol), Protocol, true);
        }

        public IEnumerable<string> ConnectionStrings
        {
            get
            {
                // check for endpoints
                if (Endpoints.Count == 0)
                    throw new InvalidOperationException("No Endpoints Provided");

                // default syntax based on protocol (default SQL=ADO)
                if (! Parameters.TryGetString("syntax", out string syntax))
                    syntax = (Protocol == SQL) ? ADO : Protocol;

                switch (syntax.ToLowerInvariant())
                {
                    case ADO:
                        return BuildAdoConnectionStrings();
                    case MONGODB:
                        return BuildMongoConnectionString();
                    case REDIS:
                        return BuilRedisConnectionString();
                    default:
                        throw new NotSupportedException($"Connection string syntax `{syntax}` is not supported");
                }
            }
        }



        private IEnumerable<string> BuildMongoConnectionString()
        {
            // check parameters
            if (! Parameters.ContainsKey("database"))
                throw new ArgumentException("'database' parameter is required");

            // start building string
            var sb = new StringBuilder("mongodb://");

            // check for user/pass
            if (Parameters.TryGetString("@User", out string userName))
            {
                sb.Append(userName);
                if (Parameters.TryGetString("@Password", out string password))
                    sb.AppendFormat(":{0}", password);

                sb.Append('@');
            }

            // get server list
            for (var i = 0; i < Endpoints.Count; i++)
            {
                if (i > 0) sb.Append(',');

                ServiceEndpoint ep = Endpoints[i];
                sb.Append($"{ep.Host}:{ep.Port}");
            }

            // append database
            sb.Append('/');
            sb.Append(Parameters["database"]);

            // append options
            int opt = 0;
            foreach (var kv in Parameters)
            {
                if (kv.Key.StartsWith("#")) continue;

                switch (kv.Key.ToLowerInvariant())
                {
                    case "syntax":
                    case "database":
                        break;
                    default:
                        sb.Append((++opt == 1) ? '?' : '&');
                        sb.Append($"{kv.Key}={kv.Value}");
                        break;
                }
            }

            yield return sb.ToString();
        }

        private IEnumerable<string> BuildAdoConnectionStrings()
        {
            // get base string
            var sb = new StringBuilder();
            foreach (var kv in Parameters)
            {
                if (kv.Key.StartsWith("#")) continue;

                switch (kv.Key.ToLowerInvariant())
                {
                    case "syntax":
                        break;
                    default:
                        sb.Append($"{kv.Key}={kv.Value};");
                        break;
                }
            }

            // enumerate endpoints
            for (var i = 0; i < Endpoints.Count; i++)
            {
                ServiceEndpoint endpoint = Endpoints[i];

                string str = _rxHost.Replace(sb.ToString(), endpoint.Host);
                str = _rxPort.Replace(str, endpoint.Port.ToString());

                if (_rxHost2.IsMatch(str))
                {
                    var j = (i < (Endpoints.Count - 1)) ? i + 1 : 0;
                    ServiceEndpoint ep2 = Endpoints[j];

                    str = _rxHost2.Replace(str, ep2.Host);
                    str = _rxPort2.Replace(str, ep2.Port.ToString());
                }

                yield return str;
            }
        }

        private IEnumerable<string> BuilRedisConnectionString()
        {
            // start building string
            var sb = new StringBuilder();

            // add endpoints
            foreach (ServiceEndpoint ep in Endpoints)
            {
                if (sb.Length > 0) sb.Append(',');
                sb.Append($"{ep.Host}:{ep.Port}");
            }

            // check for ssl as a default
            if (Endpoints.All(ep => ep.Port == 6380) && !Parameters.ContainsKey("ssl"))
                sb.Append(",ssl=True");

            // append options
            foreach (var kv in Parameters)
            {
                if (kv.Key.StartsWith("#")) continue;

                switch (kv.Key.ToLowerInvariant())
                {
                    case "syntax":
                        break;
                    default:
                        sb.Append($",{kv.Key}={kv.Value}");
                    break;
                }
            }

            yield return sb.ToString();
        }

        // use for SRV host:port substitution
        private readonly Regex _rxHost = new Regex(@"\{srv\:host\}", RegexOptions.IgnoreCase);
        private readonly Regex _rxPort = new Regex(@"\{srv\:port\}", RegexOptions.IgnoreCase);

        // use for SRV host:port substitution w/ fail-over partners
        private readonly Regex _rxHost2 = new Regex(@"\{srv2\:host\}", RegexOptions.IgnoreCase);
        private readonly Regex _rxPort2 = new Regex(@"\{srv2\:port\}", RegexOptions.IgnoreCase);
    }
}
