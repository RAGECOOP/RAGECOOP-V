using System;

namespace RageCoop.Core
{
    internal class PublicKey
    {
        public byte[] Exponent;
        public byte[] Modulus;

        public static PublicKey FromServerInfo(ServerInfo info)
        {
            return new PublicKey
            {
                Modulus = Convert.FromBase64String(info.publicKeyModulus),
                Exponent = Convert.FromBase64String(info.publicKeyExponent)
            };
        }
    }
}