using System;
using System.Collections.Generic;
using System.Text;

using Lidgren.Network;

namespace RageCoop.Core
{
    public partial class Packets
    {
        public class NozzleTransform : Packet
        {
            public int VehicleID { get; set; }

            public bool Hover { get; set; }

            public override void Pack(NetOutgoingMessage message)
            {
                #region PacketToNetOutGoingMessage
                message.Write((byte)PacketType.NozzleTransform);

                List<byte> byteArray = new List<byte>();

                byteArray.AddInt(VehicleID);
                if (Hover) { byteArray.Add(1); }

                byte[] result = byteArray.ToArray();

                message.Write(result.Length);
                message.Write(result);
                #endregion
            }

            public override void Unpack(byte[] array)
            {
                #region NetIncomingMessageToPacket
                BitReader reader = new BitReader(array);
                VehicleID=reader.ReadInt();
                Hover=reader.CanRead(1);
                
                #endregion
            }
        }




    }
}
