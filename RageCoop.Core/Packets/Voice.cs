using System.Collections.Generic;

namespace RageCoop.Core
{
    internal partial class Packets
    {
        internal class Voice : Packet
        {
            public byte[] Buffer { get; set; }
            public override PacketType Type => PacketType.Voice;
            public override byte[] Serialize()
            {
                return Buffer;
            }
            public override void Deserialize(byte[] array)
            {
                Buffer = array;
            }
        }
    }
}
