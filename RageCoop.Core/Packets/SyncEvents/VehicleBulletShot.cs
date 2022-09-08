using GTA.Math;
using Lidgren.Network;

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

            protected override void Serialize(NetOutgoingMessage m)
            {



                m.Write(OwnerID);
                m.Write(Bone);
                m.Write(WeaponHash);
                m.Write(StartPosition);
                m.Write(EndPosition);



            }

            public override void Deserialize(NetIncomingMessage m)
            {
                #region NetIncomingMessageToPacket


                OwnerID = m.ReadInt32();
                Bone = m.ReadUInt16();
                WeaponHash = m.ReadUInt32();
                StartPosition = m.ReadVector3();
                EndPosition = m.ReadVector3();
                #endregion
            }
        }




    }
}
