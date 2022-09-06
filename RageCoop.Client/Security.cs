using RageCoop.Core;
using System.IO;
using System.Security.Cryptography;
namespace RageCoop.Client
{
    internal class Security
    {
        public RSA ServerRSA { get; set; }
        public Aes ClientAes { get; set; } = Aes.Create();
        private readonly Logger Logger;
        public Security(Logger logger)
        {
            Logger = logger;
            ClientAes.GenerateKey();
            ClientAes.GenerateIV();
        }
        public void GetSymmetricKeysCrypted(out byte[] cryptedKey, out byte[] cryptedIV)
        {
            // Logger?.Debug($"Aes.Key:{ClientAes.Key.Dump()}, Aes.IV:{ClientAes.IV.Dump()}");
            cryptedKey = ServerRSA.Encrypt(ClientAes.Key, RSAEncryptionPadding.Pkcs1);
            cryptedIV = ServerRSA.Encrypt(ClientAes.IV, RSAEncryptionPadding.Pkcs1);
        }
        public byte[] Encrypt(byte[] data)
        {
            return new CryptoStream(new MemoryStream(data), ClientAes.CreateEncryptor(), CryptoStreamMode.Read).ReadToEnd();
        }
        public byte[] Decrypt(byte[] data)
        {
            return new CryptoStream(new MemoryStream(data), ClientAes.CreateDecryptor(), CryptoStreamMode.Read).ReadToEnd();
        }
        public void SetServerPublicKey(byte[] modulus, byte[] exponent)
        {
            var para = new RSAParameters();
            para.Modulus = modulus;
            para.Exponent = exponent;
            ServerRSA = RSA.Create(para);
        }
        public void Regen()
        {
            ClientAes = Aes.Create();
            ClientAes.GenerateKey();
            ClientAes.GenerateIV();
        }
    }
}
