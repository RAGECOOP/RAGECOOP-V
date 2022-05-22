using System;
using System.Collections.Generic;
using System.Text;

using Lidgren.Network;

namespace RageCoop.Core
{
    public partial class Packets
    {

        public class LeaveVehicle : Packet
        {
            public int ID { get; set; }


            public override void Pack(NetOutgoingMessage message)
            {
                #region PacketToNetOutGoingMessage
                message.Write((byte)PacketTypes.LeaveVehicle);

                List<byte> byteArray = new List<byte>();

                byteArray.AddInt(ID);

                byte[] result = byteArray.ToArray();

                message.Write(result.Length);
                message.Write(result);
                #endregion
            }

            public override void Unpack(byte[] array)
            {
                #region NetIncomingMessageToPacket
                BitReader reader = new BitReader(array);

                ID=reader.ReadInt();
                
                #endregion
            }
        }




    }
}
