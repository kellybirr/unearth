using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Grpc.Core;
using Unearth.Core;
using Unearth.Dns;
using Unearth.Grpc;

namespace TestWeb.Fx.Controllers
{
    public class HomeController : Controller
    {
        private static readonly GrpcLocator _locator = new GrpcLocator {NoCache = false};

        // GET: Home
        public ActionResult Index()
        {
            //var dnsQuery = new DnsQuery("sms._grpc._tcp.kmb.home", DnsRecordType.SRV);
            //var dnsEntries = dnsQuery.Resolve().Result;

            //var svcName = new ServiceDnsName {DnsName = "sms._grpc._tcp.kmb.home"};
            //var srv = ServiceLookup.Srv(svcName, SvcFactory).Task.Result;

            GrpcService service = _locator.Locate("sms", ChannelCredentials.Insecure).Result;
            return View("Index", service);
        }

        //private static ServiceFactory<GrpcService> SvcFactory(ServiceDnsName name, IEnumerable<DnsEntry> entries)
        //{
        //    return null;
        //}
    }
}