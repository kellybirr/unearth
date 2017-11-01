using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Unearth.Encryption;

namespace ServiceResolver.UnitTests
{
    [TestClass]
    public class EncryptionTests
    {
        [TestMethod]
        public void Encryption_Test()
        {
            string testStr = Convert.ToBase64String(Encoding.ASCII.GetBytes("dev:test"));
            AesCrypto encryptor = new AesCrypto {KeyPhrase = "dev"};

            Console.WriteLine(encryptor.Encrypt(testStr));
        }

        [TestMethod, Ignore]
        public void Decryption_Test()
        {
            string testStr = "TIksnh/5Qf33CzOBvVeiog==";
            AesCrypto encryptor = new AesCrypto { KeyPhrase = "dev" };

            Console.WriteLine(encryptor.Decrypt(testStr));
        }
    }
}
