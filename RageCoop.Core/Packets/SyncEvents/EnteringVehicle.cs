using System;
using System.Collections.Generic;
using System.Text;

using Lidgren.Network;

namespace RageCoop.Core
{
    public partial class Packets
    {
        public class EnteringVehicle : Packet
        {
            public int PedID { get; set; }

            public int VehicleID { get; set; }

            public short VehicleSeat { get; set; }

            public override void Pack(NetOutgoingMessage message)
            {
                #region PacketToNetOutGoingMessage
                message.Write((byte)PacketType.EnteringVehicle);

                List<byte> byteArray = new List<byte>();

                byteArray.AddInt(PedID);
                byteArray.AddInt(VehicleID);
                byteArray.AddInt(VehicleSeat);

                byte[] result = byteArray.ToArray();

                message.Write(result.Length);
                message.Write(result);
                #endregion
            }

            public override void Unpack(byte[] array)
            {
                #region NetIncomingMessageToPacket
                BitReader reader = new BitReader(array);

                PedID=reader.ReadInt();
                VehicleID=reader.ReadInt();
                VehicleSeat=reader.ReadShort();
                
                #endregion
            }
        }




    }
}
