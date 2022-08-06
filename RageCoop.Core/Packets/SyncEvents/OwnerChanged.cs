using System;
using System.Collections.Generic;
using System.Text;

using Lidgren.Network;

namespace RageCoop.Core
{
    internal partial class Packets
    {

        internal class OwnerChanged : Packet
        {
            public override PacketType Type  => PacketType.OwnerChanged;
            public int ID { get; set; }

            public int NewOwnerID { get; set; }

            public override byte[] Serialize()
            {

                List<byte> byteArray = new List<byte>();

                byteArray.AddInt(ID);
                byteArray.AddInt(NewOwnerID);

               return byteArray.ToArray();
            }

            public override void Deserialize(byte[] array)
            {
                #region NetIncomingMessageToPacket
                BitReader reader = new BitReader(array);

                ID=reader.ReadInt32();
                NewOwnerID=reader.ReadInt32();
                
                #endregion
            }
        }




    }
}
