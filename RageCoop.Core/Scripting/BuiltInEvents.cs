using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;

namespace RageCoop.Core.Scripting
{
    internal static class CustomEvents
    {
        static MD5 Hasher = MD5.Create();
        public static int SendWeather = Hash("RageCoop.SendWeather");
        public static int OnPlayerDied = Hash("RageCoop.OnPlayerDied");
        public static int SetAutoRespawn = Hash("RageCoop.SetAutoRespawn");
        public static int Hash(string s)
        {
            return BitConverter.ToInt32(Hasher.ComputeHash(Encoding.UTF8.GetBytes(s)), 0);
        }
    }
}
