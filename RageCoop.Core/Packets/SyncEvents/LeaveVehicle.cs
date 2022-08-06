using System;
using System.Collections.Generic;
using System.Text;

using Lidgren.Network;

namespace RageCoop.Core
{
    internal partial class Packets
    {

        internal class LeaveVehicle : Packet
        {
            public override PacketType Type { get { return PacketType.LeaveVehicle; } }
            public int ID { get; set; }


            public override byte[] Serialize()
            {

                List<byte> byteArray = new List<byte>();

                byteArray.AddInt(ID);

                return byteArray.ToArray();

            }

            public override void Deserialize(byte[] array)
            {
                #region NetIncomingMessageToPacket
                BitReader reader = new BitReader(array);

                ID=reader.ReadInt();
                
                #endregion
            }
        }




    }
}
