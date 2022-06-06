using System;
using System.Collections.Generic;
using System.Text;

using Lidgren.Network;

namespace RageCoop.Core
{
    public partial class Packets
    {

        public class PedKilled : Packet
        {
            public int VictimID { get; set; }

            public override void Pack(NetOutgoingMessage message)
            {
                #region PacketToNetOutGoingMessage
                message.Write((byte)PacketTypes.PedKilled);

                List<byte> byteArray = new List<byte>();

                byteArray.AddInt(VictimID);
                byte[] result = byteArray.ToArray();

                message.Write(result.Length);
                message.Write(result);
                #endregion
            }

            public override void Unpack(byte[] array)
            {
                #region NetIncomingMessageToPacket
                BitReader reader = new BitReader(array);

                VictimID=reader.ReadInt();

                #endregion
            }
        }




    }
}
