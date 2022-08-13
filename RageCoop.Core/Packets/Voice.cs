using System.Collections.Generic;

namespace RageCoop.Core
{
    internal partial class Packets
    {
        internal class Voice : Packet
        {
            public byte[] Buffer { get; set; }
            public int Recorded { get; set; }
            public override PacketType Type => PacketType.Voice;
            public override byte[] Serialize()
            {
                var data = new List<byte>();
                data.AddArray(Buffer);
                data.AddInt(Recorded);
                return data.ToArray();
            }
            public override void Deserialize(byte[] array)
            {
                var reader = new BitReader(array);
                Buffer = reader.ReadByteArray();
                Recorded = reader.ReadInt32();
            }
        }
    }
}
