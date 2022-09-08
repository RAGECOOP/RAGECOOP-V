using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace RageCoop.Core.Scripting
{
    /// <summary>
    /// 
    /// </summary>
    public static class CustomEvents
    {
        private static readonly MD5 Hasher = MD5.Create();
        private static readonly Dictionary<int, string> Hashed = new Dictionary<int, string>();
        internal static readonly int OnPlayerDied = Hash("RageCoop.OnPlayerDied");
        internal static readonly int SetWeather = Hash("RageCoop.SetWeather");
        internal static readonly int OnPedDeleted = Hash("RageCoop.OnPedDeleted");
        internal static readonly int OnVehicleDeleted = Hash("RageCoop.OnVehicleDeleted");
        internal static readonly int SetAutoRespawn = Hash("RageCoop.SetAutoRespawn");
        internal static readonly int SetDisplayNameTag = Hash("RageCoop.SetDisplayNameTag");
        internal static readonly int NativeCall = Hash("RageCoop.NativeCall");
        internal static readonly int NativeResponse = Hash("RageCoop.NativeResponse");
        internal static readonly int AllResourcesSent = Hash("RageCoop.AllResourcesSent");
        internal static readonly int ServerPropSync = Hash("RageCoop.ServerPropSync");
        internal static readonly int ServerBlipSync = Hash("RageCoop.ServerBlipSync");
        internal static readonly int SetEntity = Hash("RageCoop.SetEntity");
        internal static readonly int DeleteServerProp = Hash("RageCoop.DeleteServerProp");
        internal static readonly int UpdatePedBlip = Hash("RageCoop.UpdatePedBlip");
        internal static readonly int DeleteEntity = Hash("RageCoop.DeleteEntity");
        internal static readonly int DeleteServerBlip = Hash("RageCoop.DeleteServerBlip");
        internal static readonly int CreateVehicle = Hash("RageCoop.CreateVehicle");
        internal static readonly int WeatherTimeSync = Hash("RageCoop.WeatherTimeSync");
        internal static readonly int IsHost = Hash("RageCoop.IsHost");
        /// <summary>
        /// Get a Int32 hash of a string.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">The exception is thrown when the name did not match a previously computed one and the hash was the same.</exception>
        public static int Hash(string s)
        {
            var hash = BitConverter.ToInt32(Hasher.ComputeHash(Encoding.UTF8.GetBytes(s)), 0);
            lock (Hashed)
            {
                if (Hashed.TryGetValue(hash, out string name))
                {
                    if (name != s)
                    {
                        throw new ArgumentException($"Hashed value has collision with another name:{name}, hashed value:{hash}");
                    }

                    return hash;
                }

                Hashed.Add(hash, s);
                return hash;
            }
        }
    }
}
