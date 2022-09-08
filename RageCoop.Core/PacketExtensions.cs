using System;
using System.Collections.Generic;
using System.Text;
using GTA.Math;
using Lidgren.Network;

namespace RageCoop.Core
{
    internal static class PacketExtensions
    {
        #region MESSAGE-READ
        public static Vector3 ReadVector3(this NetIncomingMessage m)
        {
            return new Vector3
            {
                X = m.ReadFloat(),
                Y = m.ReadFloat(),
                Z = m.ReadFloat(),
            };
        }
        public static Vector2 ReadVector2(this NetIncomingMessage m)
        {
            return new Vector2
            {
                X = m.ReadFloat(),
                Y = m.ReadFloat(),
            };
        }
        public static Quaternion ReadQuaternion(this NetIncomingMessage m)
        {
            return new Quaternion
            {
                X = m.ReadFloat(),
                Y = m.ReadFloat(),
                Z = m.ReadFloat(),
                W = m.ReadFloat(),
            };
        }
        public static byte[] ReadByteArray(this NetIncomingMessage m)
        {
            return m.ReadBytes(m.ReadInt32());
        }
        #endregion

        #region MESSAGE-WRITE
        public static void Write(this NetOutgoingMessage m,Vector3 v)
        {
            m.Write(v.X);
            m.Write(v.Y);
            m.Write(v.Z);
        }
        public static void Write(this NetOutgoingMessage m, Quaternion q)
        {
            m.Write(q.X);
            m.Write(q.Y);
            m.Write(q.Z);
            m.Write(q.W);
        }
        public static void WriteByteArray(this NetOutgoingMessage m, byte[] b)
        {
            m.Write(b.Length);
            m.Write(b);
        }
        #endregion

        #region BYTE-LIST
        public static void AddVector3(this List<byte> bytes, Vector3 vec3)
        {
            bytes.AddRange(BitConverter.GetBytes(vec3.X));
            bytes.AddRange(BitConverter.GetBytes(vec3.Y));
            bytes.AddRange(BitConverter.GetBytes(vec3.Z));
        }
        public static void AddQuaternion(this List<byte> bytes, Quaternion quat)
        {
            bytes.AddRange(BitConverter.GetBytes(quat.X));
            bytes.AddRange(BitConverter.GetBytes(quat.Y));
            bytes.AddRange(BitConverter.GetBytes(quat.Z));
            bytes.AddRange(BitConverter.GetBytes(quat.W));
        }
        public static void AddInt(this List<byte> bytes, int i)
        {
            bytes.AddRange(BitConverter.GetBytes(i));
        }
        public static void AddUint(this List<byte> bytes, uint i)
        {
            bytes.AddRange(BitConverter.GetBytes(i));
        }
        public static void AddShort(this List<byte> bytes, short i)
        {
            bytes.AddRange(BitConverter.GetBytes(i));
        }
        public static void AddUshort(this List<byte> bytes, ushort i)
        {
            bytes.AddRange(BitConverter.GetBytes(i));
        }
        public static void AddLong(this List<byte> bytes, long i)
        {
            bytes.AddRange(BitConverter.GetBytes(i));
        }
        public static void AddUlong(this List<byte> bytes, ulong i)
        {
            bytes.AddRange(BitConverter.GetBytes(i));
        }
        public static void AddFloat(this List<byte> bytes, float i)
        {
            bytes.AddRange(BitConverter.GetBytes(i));
        }
        public static void AddBool(this List<byte> bytes, bool b)
        {
            bytes.Add(b ? (byte)1 : (byte)0);
        }
        public static void AddString(this List<byte> bytes, string s)
        {
            var sb = Encoding.UTF8.GetBytes(s);
            bytes.AddInt(sb.Length);
            bytes.AddRange(sb);
        }
        public static void AddArray(this List<byte> bytes, byte[] toadd)
        {
            bytes.AddInt(toadd.Length);
            bytes.AddRange(toadd);
        }
#endregion

        internal static bool IsSyncEvent(this PacketType p)
        {
            return (30 <= (byte)p) && ((byte)p <= 40);
        }
    }
}
