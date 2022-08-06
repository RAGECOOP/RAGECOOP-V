using System;
using System.Collections.Generic;
using System.Text;

using Lidgren.Network;

namespace RageCoop.Core
{
    internal partial class Packets
    {

        internal class PedKilled : Packet
        {
            public override PacketType Type  => PacketType.PedKilled;
            public int VictimID { get; set; }

            public override byte[] Serialize()
            {

                List<byte> byteArray = new List<byte>();

                byteArray.AddInt(VictimID);
                return byteArray.ToArray();

            }

            public override void Deserialize(byte[] array)
            {
                #region NetIncomingMessageToPacket
                BitReader reader = new BitReader(array);

                VictimID=reader.ReadInt32();

                #endregion
            }
        }




    }
}
