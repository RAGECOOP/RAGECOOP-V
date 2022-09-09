using GTA.Math;
using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Text;

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
        public static void Write(this NetOutgoingMessage m, Vector3 v)
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

        internal static bool IsSyncEvent(this PacketType p)
        {
            return (30 <= (byte)p) && ((byte)p <= 40);
        }
    }
}
