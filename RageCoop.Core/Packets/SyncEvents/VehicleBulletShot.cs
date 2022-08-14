using System;
using System.Collections.Generic;
using System.Text;
using GTA.Math;

namespace RageCoop.Core
{
    internal partial class Packets
    {

        internal class VehicleBulletShot : Packet
        {
            public override PacketType Type => PacketType.VehicleBulletShot;
            public int OwnerID { get; set; }
            public ushort Bone { get; set; }
            public uint WeaponHash { get; set; }

            public Vector3 StartPosition { get; set; }
            public Vector3 EndPosition { get; set; }

            public override byte[] Serialize()
            {

                List<byte> byteArray = new List<byte>();

                byteArray.AddInt(OwnerID);
                byteArray.AddUshort(Bone);
                byteArray.AddUint(WeaponHash);
                byteArray.AddVector3(StartPosition);
                byteArray.AddVector3(EndPosition);

                return byteArray.ToArray();

            }

            public override void Deserialize(byte[] array)
            {
                #region NetIncomingMessageToPacket
                BitReader reader = new BitReader(array);

                OwnerID=reader.ReadInt32();
                Bone=reader.ReadUInt16();
                WeaponHash=reader.ReadUInt32();
                StartPosition=reader.ReadVector3();
                EndPosition=reader.ReadVector3();
                #endregion
            }
        }




    }
}
