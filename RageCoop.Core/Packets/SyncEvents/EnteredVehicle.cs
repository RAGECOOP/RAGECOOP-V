using System;
using System.Collections.Generic;
using System.Text;

using Lidgren.Network;

namespace RageCoop.Core
{
    internal partial class Packets
    {
        internal class EnteredVehicle : Packet
        {
            public override PacketType Type  => PacketType.EnteredVehicle;
            public int PedID { get; set; }

            public int VehicleID { get; set; }

            public short VehicleSeat { get; set; }

            public override byte[] Serialize()
            {

                List<byte> byteArray = new List<byte>();

                byteArray.AddInt(PedID);
                byteArray.AddInt(VehicleID);
                byteArray.AddShort(VehicleSeat);

                return byteArray.ToArray();

            }

            public override void Deserialize(byte[] array)
            {
                #region NetIncomingMessageToPacket
                BitReader reader = new BitReader(array);

                PedID=reader.ReadInt32();
                VehicleID=reader.ReadInt32();
                VehicleSeat=reader.ReadInt16();
                
                #endregion
            }
        }




    }
}
