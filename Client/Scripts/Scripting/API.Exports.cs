using Lidgren.Network;
using RageCoop.Core;
using RageCoop.Core.Scripting;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using static RageCoop.Core.Scripting.CustomEvents;

namespace RageCoop.Client.Scripting
{
    internal static unsafe partial class API
    {
        [ThreadStatic]
        static string _lastResult;

        [UnmanagedCallersOnly(EntryPoint = nameof(GetLastResult))]
        public static int GetLastResult(char* buf, int cbBufSize)
        {
            if (_lastResult == null)
                return 0;

            fixed (char* pErr = _lastResult)
            {
                var cbToCopy = sizeof(char) * (_lastResult.Length + 1);
                System.Buffer.MemoryCopy(pErr, buf, cbToCopy, Math.Min(cbToCopy, cbBufSize));
                if (cbToCopy > cbBufSize && cbBufSize > 0)
                {
                    buf[cbBufSize / sizeof(char) - 1] = '\0'; // Always add null terminator
                }
                return _lastResult.Length;
            }
        }
        public static void SetLastResult(string msg) => _lastResult = msg;

        [UnmanagedCallersOnly(EntryPoint = nameof(SetLastResult))]
        public static void SetLastResult(char* msg)
        {
            try
            {
                SetLastResult(msg == null ? null : new string(msg));
            }
            catch (Exception ex)
            {
                SHVDN.PInvoke.MessageBoxA(default, ex.ToString(), "error", default);
            }
        }

        [UnmanagedCallersOnly(EntryPoint = nameof(GetEventHash))]
        public static CustomEventHash GetEventHash(char* name) => new string(name);

        [UnmanagedCallersOnly(EntryPoint = nameof(SendCustomEvent))]
        public static void SendCustomEvent(CustomEventFlags flags, int hash, byte* data, int cbData)
        {
            var payload = new byte[cbData];
            Marshal.Copy((IntPtr)data, payload, 0, cbData);
            Networking.Peer.SendTo(new Packets.CustomEvent()
            {
                Flags = flags,
                Payload = payload,
                Hash = hash
            }, Networking.ServerConnection, ConnectionChannel.Event, NetDeliveryMethod.ReliableOrdered);
        }

        [UnmanagedCallersOnly(EntryPoint = nameof(InvokeCommand))]
        public static int InvokeCommand(char* name, int argc, char** argv)
        {
            try
            {
                var args = new string[argc];
                for (int i = 0; i < argc; i++)
                {
                    args[i] = new(argv[i]);
                }
                _lastResult = _invokeCommand(new string(name), args);
                return _lastResult.Length;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                SetLastResult(ex.ToString());
                return 0;
            }
        }

        [UnmanagedCallersOnly(EntryPoint = nameof(GetLastResultLenInChars))]
        public static int GetLastResultLenInChars() => _lastResult?.Length ?? 0;

        /// <summary>
        ///     Convert Entity ID to handle
        /// </summary>
        [UnmanagedCallersOnly(EntryPoint = nameof(IdToHandle))]
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
