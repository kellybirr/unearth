using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Unearth.Dns;

namespace Unearth.Smtp
{
    public class SmtpService : GenericService
    {
        public SmtpService() { }

        public SmtpService(IEnumerable<ServiceEndpoint> endpoints) : base(endpoints)
        { }

        public SmtpService(IEnumerable<DnsEntry> dnsEntries) : base(dnsEntries)
        { }

        public NetworkCredential Credentials
        {
            get
            {
                if (Parameters.TryGetString("UserName", out string user))
                {
                    Parameters.TryGetString("Password", out string pass);
                    return new NetworkCredential(user, pass ?? "");
                }

                return null;
            }
        }

        public IEnumerable<SmtpClient> GetClients()
        {
            return Endpoints.Select(ep => new SmtpClient(ep.Host, ep.Port) { Credentials = Credentials });
        }

        public async Task SendMail(MailMessage message)
        {
            await new SynchronizationContextRemover();

            var exceptions = new List<Exception>();
            foreach (ServiceEndpoint ep in Endpoints)
            {
                try
                {
                    var smtpClient = new SmtpClient(ep.Host, ep.Port) { Credentials = Credentials };
                    await smtpClient.SendMailAsync(message);

                    return;
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }

            throw new AggregateException(exceptions);
        }
    }
}
