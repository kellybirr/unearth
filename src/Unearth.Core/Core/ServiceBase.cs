using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Primitives;
using Unearth.Dns;
using Unearth.Encryption;



namespace Unearth.Core
{
    public abstract class ServiceBase<TEp> : IServiceInfo
        where TEp : ServiceEndpoint, new()
    {
        public virtual string Name { get; set; }

        public virtual string Protocol { get; set; }

        public virtual IList<TEp> Endpoints => _endpoints;
        private readonly List<TEp> _endpoints;

        public virtual IDictionary<string, StringValues> Parameters
        {
            get
            {
                if (! _decoded) EnsureDecoded();
                return _parameters;
            }
        }

        private readonly Dictionary<string, StringValues> _parameters;
        private volatile bool _decoded;

        public virtual IDecryptor Decryptor => _decryptor.Value;
        private readonly Lazy<AesCrypto> _decryptor = new Lazy<AesCrypto>(() => new AesCrypto());

        protected ServiceBase()
        {
            _endpoints = new List<TEp>();
            _parameters = new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase);
        }

        protected ServiceBase(IEnumerable<TEp> endpoints)
        {
            _endpoints = endpoints.OrderBy(r => r.Priority).ToList();
            _parameters = new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase);
        }

        protected ServiceBase(IEnumerable<DnsEntry> dnsEntries) : this()
        {
            _endpoints = new List<TEp>();
            _parameters = new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase);

            foreach (DnsEntry dns in dnsEntries)
            {
                if (dns is DnsServiceEntry srv)
                {   // SRV
                    TEp ep = ServiceEndpoint.Create<TEp>(srv);
                    _endpoints.Add(ep);
                }
                else if (dns is DnsTextEntry txt)
                {   // TXT
                    foreach (KeyValuePair<string, StringValues> pair in txt.ToDictionary())
                    {
                        if (_parameters.TryGetValue(pair.Key, out StringValues values))
                            _parameters[pair.Key] = StringValues.Concat(values, pair.Value);
                        else
                            _parameters.Add(pair.Key, pair.Value);
                    }
                }
            }

            if (_endpoints.Count > 0) // sort endpoints by priority
                _endpoints.Sort((ep1, ep2) => ep1.Priority.CompareTo(ep2.Priority));
        }

        public DateTime Expires => Endpoints.Min(e => e.Expires);


        private void EnsureDecoded()
        {
            lock (_parameters)
            {
                if (_decoded) return;

                KeyValuePair<string, StringValues>[] values = _parameters.ToArray();
                foreach (KeyValuePair<string, StringValues> kv in values)
                {
                    StringValues str = DecodeParameterStrings(kv.Value);
                    _parameters[kv.Key] = str;
                }

                _decoded = true;
            }
        }

        private StringValues DecodeParameterStrings(StringValues values)
        {
            var decoded = new string[values.Count];

            // ReSharper disable once ForCanBeConvertedToForeach
            for (int i = 0; i < values.Count; i++)
            {
                string str = values[i];

                // check for environment variables
                Match m = _rxEnv.Match(str);
                while (m.Success)
                {
                    string variable = m.Groups["v"].Value;
                    string value = Environment.GetEnvironmentVariable(variable) ?? "";
                    str = str.Replace(m.Value, value);

                    m = m.NextMatch();
                }

                // check for encrypted variables
                m = _rxAes.Match(str);
                while (m.Success)
                {
                    string encrypted = m.Groups["v"].Value;
                    string value = Decryptor.Decrypt(encrypted);
                    str = str.Replace(m.Value, value);

                    m = m.NextMatch();
                }

                decoded[i] = str;
            }

            return new StringValues(decoded);
        }

        // use for environment substitution
        private readonly Regex _rxEnv = new Regex(@"\{env\:(?<v>\w+)\}", RegexOptions.IgnoreCase);

        // use for encrypted values
        private readonly Regex _rxAes = new Regex(@"\{aes\:(?<v>[A-Za-z0-9\+\/\=]+)\}", RegexOptions.IgnoreCase);
    }

}
