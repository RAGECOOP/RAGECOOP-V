using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;

namespace RageCoop.Core.Scripting.Events
{
    public interface ICustomEvent
    {
        int EventID { get; set; }
        byte[] Serialize();
        void Deserialize(byte[] data);
    }
    public abstract class CustomEvent:ICustomEvent
    {
        public abstract int EventID { get; set; }
        public abstract byte[] Serialize();
        public abstract void Deserialize(byte[] data);
    }
    public static class Hasher
    {
        public static int Hash(string s)
        {
            MD5 md5Hasher = MD5.Create();
            var hashed = md5Hasher.ComputeHash(Encoding.UTF8.GetBytes(s));
            return BitConverter.ToInt32(hashed, 0);
        }
    }
}
