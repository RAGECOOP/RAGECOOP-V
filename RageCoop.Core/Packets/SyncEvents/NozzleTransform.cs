using System;
using System.Collections.Generic;
using System.Text;

using Lidgren.Network;

namespace RageCoop.Core
{
    internal partial class Packets
    {
        internal class NozzleTransform : Packet
        {
            public override PacketType Type  => PacketType.NozzleTransform;
            public int VehicleID { get; set; }

            public bool Hover { get; set; }

            public override byte[] Serialize()
            {

                List<byte> byteArray = new List<byte>();

                byteArray.AddInt(VehicleID);
                byteArray.AddBool(Hover);

                return byteArray.ToArray();

            }

            public override void Deserialize(byte[] array)
            {
                #region NetIncomingMessageToPacket
                BitReader reader = new BitReader(array);
                VehicleID=reader.ReadInt32();
                Hover=reader.ReadBoolean();
                
                #endregion
            }
        }




    }
}
