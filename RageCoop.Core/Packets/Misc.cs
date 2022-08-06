using System;
using System.Collections.Generic;
using Lidgren.Network;

namespace RageCoop.Core
{
    internal partial class Packets
    {
        internal class PingPong : Packet
        {
            public override PacketType Type { get { return PacketType.PingPong; } }
            public override byte[] Serialize()
            {
                return new byte[0];
            }

            public override void Deserialize(byte[] array)
            {
            }
        }
    }
}
