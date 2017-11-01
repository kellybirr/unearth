using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Unearth.Smtp;

namespace ServiceResolver.UnitTests
{
    [TestClass]
    public class SmtpTests
    {

        [TestMethod]
        public async Task Locate_Smtp_Test()
        {
            var locator = new SmtpLocator {ServiceDomain = "dev-test.rnd.ipzo.net"};
            SmtpService smtp = await locator.Locate();
            //smtp.Decryptor.KeyPhrase = "dev";

            Assert.IsNotNull(smtp);
            Assert.IsNotNull(smtp.Credentials);

            Console.WriteLine(smtp.Credentials.UserName);
            Assert.AreEqual("rnd.test@mailapp.imprezzio.com", smtp.Credentials.UserName);

            Console.WriteLine(smtp.Credentials.Password);
        }
    }
}
