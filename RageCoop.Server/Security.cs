using RageCoop.Core;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
namespace RageCoop.Server
{
    internal class Security
    {
        private readonly Logger Logger;
        public Security(Logger logger)
        {
            Logger = logger;
        }
        public RSA RSA = RSA.Create(2048);
        private readonly Dictionary<IPEndPoint, Aes> SecuredConnections = new Dictionary<IPEndPoint, Aes>();

        public bool HasSecuredConnection(IPEndPoint target)
        {
            return SecuredConnections.ContainsKey(target);
        }

        public byte[] Encrypt(byte[] data, IPEndPoint target)
        {
            var ms = new MemoryStream();
            using (var cs = new CryptoStream(ms, SecuredConnections[target].CreateEncryptor(), CryptoStreamMode.Write))
            {
                cs.Write(data, 0, data.Length);
            }
            return ms.ToArray();
        }
        public byte[] Decrypt(byte[] data, IPEndPoint target)
        {
            return new CryptoStream(new MemoryStream(data), SecuredConnections[target].CreateDecryptor(), CryptoStreamMode.Read).ReadToEnd();
        }

        public void AddConnection(IPEndPoint endpoint, byte[] cryptedKey, byte[] cryptedIV)
        {
            var key = RSA.Decrypt(cryptedKey, RSAEncryptionPadding.Pkcs1);
            var iv = RSA.Decrypt(cryptedIV, RSAEncryptionPadding.Pkcs1);
            // Logger?.Debug($"key:{key.Dump()}, iv:{iv.Dump()}");
            var conAes = Aes.Create();
            conAes.Key = key;
            conAes.IV = iv;
            if (!SecuredConnections.ContainsKey(endpoint))
            {
                SecuredConnections.Add(endpoint, conAes);
            }
            else
            {
                SecuredConnections[endpoint] = conAes;
            }
        }
        public void RemoveConnection(IPEndPoint ep)
        {
            if (SecuredConnections.ContainsKey(ep))
            {
                SecuredConnections.Remove(ep);
            }
        }
        public void GetPublicKey(out byte[] modulus, out byte[] exponent)
        {
            var key = RSA.ExportParameters(false);
            modulus = key.Modulus;
            exponent = key.Exponent;
        }
        public void ClearConnections()
        {
            SecuredConnections.Clear();
        }

    }
}
