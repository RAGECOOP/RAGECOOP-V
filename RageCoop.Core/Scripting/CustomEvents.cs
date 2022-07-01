using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;

namespace RageCoop.Core.Scripting
{
    /// <summary>
    /// 
    /// </summary>
    public static class CustomEvents
    {
        static MD5 Hasher = MD5.Create();
        static Dictionary<int,string> Hashed=new Dictionary<int,string>();
        public static readonly int SetWeather = Hash("RageCoop.SetWeather");
        public static readonly int OnPlayerDied = Hash("RageCoop.OnPlayerDied");
        public static readonly int SetAutoRespawn = Hash("RageCoop.SetAutoRespawn");
        public static readonly int NativeCall = Hash("RageCoop.NativeCall");
        public static readonly int NativeResponse = Hash("RageCoop.NativeResponse");
        public static readonly int AllResourcesSent = Hash("RageCoop.AllResourcesSent");
        /// <summary>
        /// Get a Int32 hash of a string.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">The exception is thrown when the name did not match a previously computed one and the hash was the same.</exception>
        public static int Hash(string s)
        {
            var hash = BitConverter.ToInt32(Hasher.ComputeHash(Encoding.UTF8.GetBytes(s)), 0);
            string name;
            lock (Hashed)
            {
                if (Hashed.TryGetValue(hash, out name))
                {
                    if (name!=s)
                    {
                        throw new ArgumentException($"Hashed value has collision with another name:{name}, hashed value:{hash}");
                    }
                    else
                    {
                        return hash;
                    }
                }
                else
                {
                    Hashed.Add(hash, s);
                    return hash;
                }
            }
        }
    }
}
