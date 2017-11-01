namespace Unearth.Encryption
{
    public interface IDecryptor
    {
        string ServiceDomain { get; set; }
        string KeyPhrase { get; set; }
        string Decrypt(string cipherText);
    }

    public interface IEncryptor
    {
        string ServiceDomain { get; set; }
        string KeyPhrase { get; set; }
        string Encrypt(string clearText);
    }

}
