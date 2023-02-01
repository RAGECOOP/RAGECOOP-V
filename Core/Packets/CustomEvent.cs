using System;
using GTA;
using Lidgren.Network;
using RageCoop.Core.Scripting;

namespace RageCoop.Core
{
    internal partial class Packets
    {
        internal class CustomEvent : Packet
        {
            public CustomEventFlags Flags;

            public CustomEvent(CustomEventFlags flags = CustomEventFlags.None)
            {
                Flags = flags;
            }

            public override PacketType Type => PacketType.CustomEvent;
            public int Hash { get; set; }
            public byte[] Payload;
            public object[] Args;

            protected override void Serialize(NetOutgoingMessage m)
            {
                m.Write((byte)Flags);
                m.Write(Hash);
                if (Args != null)
                {
                    lock (WriteBufferShared)
                    {
                        WriteBufferShared.Reset();
                        CustomEvents.WriteObjects(WriteBufferShared, Args);
                        Payload = WriteBufferShared.ToByteArray(WriteBufferShared.Position);
                    }
                }
                m.Write(Payload);
            }

            public unsafe override void Deserialize(NetIncomingMessage m)
            {
                Flags = (CustomEventFlags)m.ReadByte();
                Hash = m.ReadInt32();
                Payload = m.ReadBytes(m.LengthBytes - m.PositionInBytes);
                fixed (byte* p = Payload)
                {
                    lock (ReadBufferShared)
                    {
                        ReadBufferShared.Initialise(p,Payload.Length);
                        Args = CustomEvents.ReadObjects(ReadBufferShared);
                    }
                }
            }
        }
    }
}