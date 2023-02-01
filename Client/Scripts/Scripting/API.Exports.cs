using Lidgren.Network;
using RageCoop.Core.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using static RageCoop.Core.Scripting.CustomEvents;

namespace RageCoop.Client.Scripting
{
    public static unsafe partial class API
    {
        [UnmanagedCallersOnly(EntryPoint = "Connect")]
        public static void Connect(char* address) => Connect(new string(address));

        [UnmanagedCallersOnly(EntryPoint = "GetLocalPlayerID")]
        public static int GetLocalPlayerID() => LocalPlayerID;

        /// <summary>
        /// Get configuration value
        /// </summary>
        /// <param name="szName">The name of the config</param>
        /// <param name="buf">Buffer to store retrived value</param>
        /// <param name="bufSize">Buffer size</param>
        /// <returns>The string length of returned value, not including the null terminator</returns>

        [UnmanagedCallersOnly(EntryPoint = "GetConfigValue")]
        public static int GetConfigValue(char* szName, char* buf, int bufSize)
        {
            var name = new string(szName);
            var value = name switch
            {
                nameof(Config.EnableAutoRespawn) => Config.EnableAutoRespawn.ToString(),
                nameof(Config.Username) => Config.Username.ToString(),
                nameof(Config.BlipColor) => Config.BlipColor.ToString(),
                nameof(Config.BlipScale) => Config.BlipScale.ToString(),
                nameof(Config.BlipSprite) => Config.BlipSprite.ToString(),
                _ => null
            };

            if (value == null)
                return 0;

            fixed (char* p = value)
            {
                var cbRequired = (value.Length + 1) * sizeof(char);
                Buffer.MemoryCopy(p, buf, bufSize, cbRequired);
                return value.Length;
            }
        }

        [UnmanagedCallersOnly(EntryPoint = "SetConfigValue")]
        public static void SetConfigValue(char* szName, char* szValue)
        {
            var name = new string(szName);
            var value = new string(szValue);
            switch (name)
            {
                case nameof(Config.EnableAutoRespawn): Config.EnableAutoRespawn = bool.Parse(value); break;
                case nameof(Config.Username): Config.Username = value; break;
                case nameof(Config.BlipColor): Config.BlipColor = Enum.Parse<BlipColor>(value); break;
                case nameof(Config.BlipScale): Config.BlipScale = float.Parse(value); break;
                case nameof(Config.BlipSprite): Config.BlipSprite = Enum.Parse<BlipSprite>(value); break;
            };
        }

        [UnmanagedCallersOnly(EntryPoint = "LocalChatMessage")]
        public static void LocalChatMessage(char* from, char* msg) => LocalChatMessage(new string(from), new string(msg));

        [UnmanagedCallersOnly(EntryPoint = "SendChatMessage")]
        public static void SendChatMessage(char* msg) => SendChatMessage(new string(msg));

        [UnmanagedCallersOnly(EntryPoint = "GetEventHash")]
        public static CustomEventHash GetEventHash(char* name)=>new string(name);

        public static void SendCustomEvent(int hash, CustomEventFlags flags, byte* data, int cbData)
        {

        }

        /// <summary>
        ///     Convert Entity ID to handle
        /// </summary>
        [UnmanagedCallersOnly(EntryPoint = "IdToHandle")]
        public static int IdToHandle(byte type, int id)
        {
            return type switch
            {
                T_ID_PROP => EntityPool.GetPropByID(id)?.MainProp?.Handle ?? 0,
                T_ID_PED => EntityPool.GetPedByID(id)?.MainPed?.Handle ?? 0,
                T_ID_VEH => EntityPool.GetVehicleByID(id)?.MainVehicle?.Handle ?? 0,
                T_ID_BLIP => EntityPool.GetBlipByID(id)?.Handle ?? 0,
                _ => 0,
            };
        }
    }
}
