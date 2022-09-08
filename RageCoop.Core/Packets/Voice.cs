using Lidgren.Network;

namespace RageCoop.Core
{
    internal partial class Packets
    {
        internal class Voice : Packet
        {
            public int ID { get; set; }
            public byte[] Buffer { get; set; }
            public int Recorded { get; set; }
            public override PacketType Type => PacketType.Voice;
            protected override void Serialize(NetOutgoingMessage m)
            {
                m.Write(ID);
                m.Write(Buffer);
                m.Write(Recorded);

            }
            public override void Deserialize(NetIncomingMessage m)
            {

                ID = m.ReadInt32();
                Buffer = m.ReadByteArray();
                Recorded = m.ReadInt32();
            }
        }
    }
}
