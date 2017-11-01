using System;
using System.Collections.Generic;
using System.Text;

namespace Unearth.Core
{
    public class ServiceDnsName
    {
        public string Domain { get; set; }
        public string DnsName { get; set; }
        public string ServiceName { get; set; }
        public string Protocol { get; set; }

        public override string ToString() => DnsName;
    }
}
