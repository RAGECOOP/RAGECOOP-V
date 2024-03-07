using GTA.Math;
using Lidgren.Network;

namespace RageCoop.Core
{
    internal static class PacketExtensions
    {
        internal static bool IsSyncEvent(this PacketType p)
        {
            return 30 <= (byte)p && (byte)p <= 40;
        }

        #region MESSAGE-READ

        public static LVector3 ReadVector3(this NetIncomingMessage m)
        {
            return new LVector3
            {
                X = m.ReadFloat(),
                Y = m.ReadFloat(),
                Z = m.ReadFloat()
            };
        }

        public static LVector2 ReadVector2(this NetIncomingMessage m)
        {
            return new LVector2
            {
                X = m.ReadFloat(),
                Y = m.ReadFloat()
            };
        }

        public static Quaternion ReadQuaternion(this NetIncomingMessage m)
        {
            return new Quaternion
            {
                X = m.ReadFloat(),
                Y = m.ReadFloat(),
                Z = m.ReadFloat(),
                W = m.ReadFloat()
            };
        }

        public static byte[] ReadByteArray(this NetIncomingMessage m)
        {
            return m.ReadBytes(m.ReadInt32());
        }

        #endregion

        #region MESSAGE-WRITE

        public static void Write(this NetOutgoingMessage m, LVector3 v)
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
    }
}