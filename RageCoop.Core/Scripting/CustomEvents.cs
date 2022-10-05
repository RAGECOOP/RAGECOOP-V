using GTA.Native;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace RageCoop.Core.Scripting
{
    /// <summary>
    /// Describes how the event should be sent or processed
    /// </summary>
    public enum CustomEventFlags : byte
    {
        None=0,
        
        /// <summary>
        /// Data will be encrypted and decrypted on target client
        /// </summary>
        Encrypted=1,

        /// <summary>
        /// Event will be queued and fired in script thread, specify this flag if your handler will call native functions.
        /// </summary>
        Queued=2,

    }

    /// <summary>
    /// Struct to identify different event using hash
    /// </summary>
    public struct CustomEventHash
    {
        private static readonly MD5 Hasher = MD5.Create();
        private static readonly Dictionary<int, string> Hashed = new Dictionary<int, string>();
        /// <summary>
        /// Hash value
        /// </summary>
        public int Hash;
        /// <summary>
        /// Create from hash
        /// </summary>
        /// <param name="hash"></param>
        public static implicit operator CustomEventHash(int hash)
        {
            return new CustomEventHash() { Hash = hash };
        }
        /// <summary>
        /// Create from string
        /// </summary>
        /// <param name="name"></param>
        public static implicit operator CustomEventHash(string name)
        {
            return new CustomEventHash() { Hash = FromString(name) };

        }
        /// <summary>
        /// Get a Int32 hash of a string.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">The exception is thrown when the name did not match a previously computed one and the hash was the same.</exception>
        public static int FromString(string s)
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
        /// <summary>
        /// To int
        /// </summary>
        /// <param name="h"></param>
        public static implicit operator int(CustomEventHash h)
        {
            return h.Hash;
        }
    }
    /// <summary>
    /// 
    /// </summary>
    public static class CustomEvents
    {
        internal static readonly CustomEventHash OnPlayerDied = "RageCoop.OnPlayerDied";
        internal static readonly CustomEventHash SetWeather = "RageCoop.SetWeather";
        internal static readonly CustomEventHash OnPedDeleted = "RageCoop.OnPedDeleted";
        internal static readonly CustomEventHash OnVehicleDeleted = "RageCoop.OnVehicleDeleted";
        internal static readonly CustomEventHash SetAutoRespawn = "RageCoop.SetAutoRespawn";
        internal static readonly CustomEventHash SetDisplayNameTag = "RageCoop.SetDisplayNameTag";
        internal static readonly CustomEventHash NativeCall = "RageCoop.NativeCall";
        internal static readonly CustomEventHash NativeResponse = "RageCoop.NativeResponse";
        internal static readonly CustomEventHash AllResourcesSent = "RageCoop.AllResourcesSent";
        internal static readonly CustomEventHash ServerPropSync = "RageCoop.ServerPropSync";
        internal static readonly CustomEventHash ServerBlipSync = "RageCoop.ServerBlipSync";
        internal static readonly CustomEventHash SetEntity = "RageCoop.SetEntity";
        internal static readonly CustomEventHash DeleteServerProp = "RageCoop.DeleteServerProp";
        internal static readonly CustomEventHash UpdatePedBlip = "RageCoop.UpdatePedBlip";
        internal static readonly CustomEventHash DeleteEntity = "RageCoop.DeleteEntity";
        internal static readonly CustomEventHash DeleteServerBlip = "RageCoop.DeleteServerBlip";
        internal static readonly CustomEventHash CreateVehicle = "RageCoop.CreateVehicle";
        internal static readonly CustomEventHash WeatherTimeSync = "RageCoop.WeatherTimeSync";
        internal static readonly CustomEventHash IsHost = "RageCoop.IsHost";

    }
}
