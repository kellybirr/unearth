using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Unearth.Configuration;

namespace Unearth.Encryption
{
    public class AesCrypto : IDecryptor
    {
        private readonly byte[] _key = new byte[32];
        private readonly byte[] _iv = new byte[16];
        private string _serviceDomain, _keyPhrase;

        public string ServiceDomain
        {
            get => _serviceDomain;
            set
            {
                _serviceDomain = value;
                GenerateKey();
            }
        }

        public string KeyPhrase
        {
            get => _keyPhrase;
            set
            {
                _keyPhrase = value;
                GenerateKey();
            }
        }

        public AesCrypto()
        {
            _serviceDomain = Environment.GetEnvironmentVariable("SERVICE_DOMAIN");
            _keyPhrase = SecretConfiguration.Pepper;

            GenerateKey();
        }

        private void GenerateKey()
        {
            // get pass-phrase
            string passPhrase = $"{_keyPhrase}@{_serviceDomain}";
            byte[] passBytes = Encoding.UTF8.GetBytes(passPhrase);

            // generate bytes via hash
            SHA384 sha = SHA384.Create();
            byte[] hashBytes = sha.ComputeHash(passBytes);

            // get key and IV
            Array.Copy(hashBytes, 0, _key, 0, _key.Length);
            Array.Copy(hashBytes, 32, _iv, 0, _iv.Length);
        }

        public string Encrypt(string clearText)
        {
            byte[] clearBytes = Encoding.UTF8.GetBytes(clearText);

            using (var ms = new MemoryStream())
            {
                using (var aes = Aes.Create())
                using (ICryptoTransform crypto = aes.CreateEncryptor(_key, _iv))
                using (var cryptoStream = new CryptoStream(ms, crypto, CryptoStreamMode.Write))
                    cryptoStream.Write(clearBytes, 0, clearBytes.Length);

                byte[] cipherBytes = ms.ToArray();
                return Convert.ToBase64String(cipherBytes);
            }
        }

        public string Decrypt(string cipherText)
        {
            try
            {
                byte[] cipherBytes = Convert.FromBase64String(cipherText);

                using (var ms = new MemoryStream(cipherBytes))
                {
                    using (var aes = Aes.Create())
                    using (ICryptoTransform crypto = aes.CreateDecryptor(_key, _iv))
                    using (var cryptoStream = new CryptoStream(ms, crypto, CryptoStreamMode.Read))
                    using (var reader = new StreamReader(cryptoStream, Encoding.UTF8))
                        return reader.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                throw new CryptographicException("AES Decryption Failed", ex);
            }
        }
    }
}
