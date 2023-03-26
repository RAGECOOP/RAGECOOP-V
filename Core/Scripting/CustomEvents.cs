using GTA;
using GTA.Math;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace RageCoop.Core.Scripting
{
    /// <summary>
    ///     Describes how the event should be sent or processed
    /// </summary>
    public enum CustomEventFlags : byte
    {
        None = 0,

        /// <summary>
        ///     Data will be encrypted and decrypted on target client
        /// </summary>
        Encrypted = 1,

        /// <summary>
        ///     Event will be queued and fired in script thread, specify this flag if your handler will call native functions.
        /// </summary>
        Queued = 2
    }

    /// <summary>
    ///     Struct to identify different event using hash
    /// </summary>
    public struct CustomEventHash
    {
        private static readonly MD5 Hasher = MD5.Create();
        private static readonly Dictionary<int, string> Hashed = new Dictionary<int, string>();

        /// <summary>
        ///     Hash value
        /// </summary>
        public int Hash;

        /// <summary>
        ///     Create from hash
        /// </summary>
        /// <param name="hash"></param>
        public static implicit operator CustomEventHash(int hash)
        {
            return new CustomEventHash { Hash = hash };
        }

        /// <summary>
        ///     Create from string
        /// </summary>
        /// <param name="name"></param>
        public static implicit operator CustomEventHash(string name)
        {
            return new CustomEventHash { Hash = FromString(name) };
        }

        /// <summary>
        ///     Get a Int32 hash of a string.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">
        ///     The exception is thrown when the name did not match a previously computed one and
        ///     the hash was the same.
        /// </exception>
        public static int FromString(string s)
        {
            var hash = BitConverter.ToInt32(Hasher.ComputeHash(Encoding.UTF8.GetBytes(s)), 0);
            lock (Hashed)
            {
                if (Hashed.TryGetValue(hash, out var name))
                {
                    if (name != s)
                        throw new ArgumentException(
                            $"Hashed value has collision with another name:{name}, hashed value:{hash}");

                    return hash;
                }

                Hashed.Add(hash, s);
                return hash;
            }
        }

        /// <summary>
        ///     To int
        /// </summary>
        /// <param name="h"></param>
        public static implicit operator int(CustomEventHash h)
        {
            return h.Hash;
        }
    }

    /// <summary>
    /// Common processing for custome client\server events
    /// </summary>
    public static partial class CustomEvents
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

        #region TYPE CONSTANTS

        public const byte T_BYTE = 1;
        public const byte T_SHORT = 2;
        public const byte T_USHORT = 3;
        public const byte T_INT = 4;
        public const byte T_UINT = 5;
        public const byte T_LONG = 6;
        public const byte T_ULONG = 7;
        public const byte T_FLOAT = 8;
        public const byte T_BOOL = 9;
        public const byte T_STR = 10;
        public const byte T_VEC3 = 11;
        public const byte T_QUAT = 12;
        public const byte T_MODEL = 13;
        public const byte T_VEC2 = 14;
        public const byte T_BYTEARR = 15;
        public const byte T_ID_PROP = 50;
        public const byte T_ID_PED = 51;
        public const byte T_ID_VEH = 52;
        public const byte T_ID_BLIP = 60;

        #endregion

        public static void WriteObjects(BufferWriter b, params object[] objs)
        {
            b.WriteVal(objs.Length);
            foreach (var obj in objs)
            {
                switch (obj)
                {
                    case byte value:
                        b.WriteVal(T_BYTE);
                        b.WriteVal(value);
                        break;
                    case short value:
                        b.WriteVal(T_SHORT);
                        b.WriteVal(value);
                        break;
                    case ushort value:
                        b.WriteVal(T_USHORT);
                        b.WriteVal(value);
                        break;
                    case int value:
                        b.WriteVal(T_INT);
                        b.WriteVal(value);
                        break;
                    case uint value:
                        b.WriteVal(T_UINT);
                        b.WriteVal(value);
                        break;
                    case long value:
                        b.WriteVal(T_LONG);
                        b.WriteVal(value);
                        break;
                    case ulong value:
                        b.WriteVal(T_ULONG);
                        b.WriteVal(value);
                        break;
                    case float value:
                        b.WriteVal(T_FLOAT);
                        b.WriteVal(value);
                        break;
                    case bool value:
                        b.WriteVal(T_BOOL);
                        b.WriteVal(value);
                        break;
                    case string value:
                        b.WriteVal(T_STR);
                        b.Write(value);
                        break;
                    case Vector2 value:
                        b.WriteVal(T_VEC2);
                        var vec2 = (LVector2)value;
                        b.Write(ref vec2);
                        break;
                    case LVector2 value:
                        b.WriteVal(T_VEC2);
                        b.Write(ref value);
                        break;
                    case Vector3 value:
                        b.WriteVal(T_VEC3);
                        var vec3 = (LVector3)value;
                        b.Write(ref vec3);
                        break;
                    case LVector3 value:
                        b.WriteVal(T_VEC3);
                        b.Write(ref value);
                        break;
                    case Quaternion value:
                        b.WriteVal(T_QUAT);
                        var quat = (LQuaternion)value;
                        b.Write(ref quat);
                        break;
                    case LQuaternion value:
                        b.WriteVal(T_QUAT);
                        b.Write(ref value);
                        break;
                    case Model value:
                        b.WriteVal(T_MODEL);
                        b.WriteVal(value);
                        break;
                    case byte[] value:
                        b.WriteVal(T_BYTEARR);
                        b.WriteArray(value);
                        break;
                    case Tuple<byte, byte[]> value:
                        b.WriteVal(value.Item1);
                        b.Write(new ReadOnlySpan<byte>(value.Item2));
                        break;
                    default:
                        throw new Exception("Unsupported object type: " + obj.GetType());
                }
            }
        }
        public static object[] ReadObjects(BufferReader r)
        {
            var Args = new object[r.ReadVal<int>()];
            for (var i = 0; i < Args.Length; i++)
            {
                var type = r.ReadVal<byte>();
                switch (type)
                {
                    case T_BYTE:
                        Args[i] = r.ReadVal<byte>();
                        break;
                    case T_SHORT:
                        Args[i] = r.ReadVal<short>();
                        break;
                    case T_USHORT:
                        Args[i] = r.ReadVal<ushort>();
                        break;
                    case T_INT:
                        Args[i] = r.ReadVal<int>();
                        break;
                    case T_UINT:
                        Args[i] = r.ReadVal<uint>();
                        break;
                    case T_LONG:
                        Args[i] = r.ReadVal<long>();
                        break;
                    case T_ULONG:
                        Args[i] = r.ReadVal<ulong>();
                        break;
                    case T_FLOAT:
                        Args[i] = r.ReadVal<float>();
                        break;
                    case T_BOOL:
                        Args[i] = r.ReadVal<bool>();
                        break;
                    case T_STR:
                        r.Read(out string str);
                        Args[i] = str;
                        break;
                    case T_VEC3:
                        r.Read(out LVector3 vec);
                        Args[i] = (Vector3)vec;
                        break;
                    case T_QUAT:
                        r.Read(out LQuaternion quat);
                        Args[i] = (Quaternion)quat;
                        break;
                    case T_MODEL:
                        Args[i] = r.ReadVal<Model>();
                        break;
                    case T_VEC2:
                        r.Read(out LVector2 vec2);
                        Args[i] = (Vector2)vec2;
                        break;
                    case T_BYTEARR:
                        Args[i] = r.ReadArray<byte>();
                        break;
                    case T_ID_BLIP:
                    case T_ID_PED:
                    case T_ID_PROP:
                    case T_ID_VEH:
                        Args[i] = IdToHandle(type, r.ReadVal<int>());
                        break;
                    default:
                        throw new InvalidOperationException($"Unexpected type: {type}");
                }
            }
            return Args;
        }

        static unsafe delegate* unmanaged<byte, int, int> _idToHandlePtr;
        public static unsafe int IdToHandle(byte type, int id)
        {
            if (_idToHandlePtr == default)
            {
                if (SHVDN.Core.GetPtr == default)
                    throw new InvalidOperationException("Not client");

                _idToHandlePtr = (delegate* unmanaged<byte, int, int>)SHVDN.Core.GetPtr("RageCoop.Client.Scripting.API.IdToHandle");
                if (_idToHandlePtr == default)
                    throw new KeyNotFoundException("IdToHandle function not found");
            }

            return _idToHandlePtr(type, id);
        }
    }
}