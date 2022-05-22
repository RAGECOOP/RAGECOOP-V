using System;
using System.Collections.Generic;
using System.Text;

using Lidgren.Network;

namespace RageCoop.Core
{
    public partial class Packets
    {

        public class BulletShot : Packet
        {
            public int OwnerID { get; set; }

            public uint WeaponHash { get; set; }

            public LVector3 StartPosition { get; set; }
            public LVector3 EndPosition { get; set; }

            public override void Pack(NetOutgoingMessage message)
            {
                #region PacketToNetOutGoingMessage
                message.Write((byte)PacketTypes.BulletShot);

                List<byte> byteArray = new List<byte>();

                // Write OwnerID 
                byteArray.AddRange(BitConverter.GetBytes(OwnerID));

                // Write weapon hash
                byteArray.AddRange(BitConverter.GetBytes(WeaponHash));

                // Write StartPosition
                byteArray.AddLVector3(StartPosition);

                // Write EndPosition
                byteArray.AddLVector3(EndPosition);


                byte[] result = byteArray.ToArray();

                message.Write(result.Length);
                message.Write(result);
                #endregion
            }

            public override void Unpack(byte[] array)
            {
                #region NetIncomingMessageToPacket
                BitReader reader = new BitReader(array);

                // Read OwnerID
                OwnerID=reader.ReadInt();

                // Read WeponHash
                WeaponHash=reader.ReadUInt();

                // Read StartPosition
                StartPosition=reader.ReadLVector3();

                // Read EndPosition
                EndPosition=reader.ReadLVector3();
                #endregion
            }
        }




    }
}
