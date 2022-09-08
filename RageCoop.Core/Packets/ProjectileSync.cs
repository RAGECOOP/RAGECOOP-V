using GTA.Math;
using Lidgren.Network;

namespace RageCoop.Core
{
    internal partial class Packets
    {
        internal class ProjectileSync : Packet
        {
            public override PacketType Type => PacketType.ProjectileSync;
            public int ID { get; set; }

            public int ShooterID { get; set; }
            public uint WeaponHash { get; set; }

            public Vector3 Position { get; set; }

            public Vector3 Rotation { get; set; }

            public Vector3 Velocity { get; set; }

            public ProjectileDataFlags Flags { get; set; }



            protected override void Serialize(NetOutgoingMessage m)
            {



                // Write id
                m.Write(ID);

                // Write ShooterID
                m.Write(ShooterID);

                m.Write(WeaponHash);

                // Write position
                m.Write(Position);


                // Write rotation
                m.Write(Rotation);

                // Write velocity
                m.Write(Velocity);
                m.Write((byte)Flags);



            }

            public override void Deserialize(NetIncomingMessage m)
            {
                #region NetIncomingMessageToPacket


                // Read id
                ID = m.ReadInt32();

                // Read ShooterID
                ShooterID = m.ReadInt32();

                WeaponHash = m.ReadUInt32();

                // Read position
                Position = m.ReadVector3();

                // Read rotation
                Rotation = m.ReadVector3();

                // Read velocity
                Velocity = m.ReadVector3();

                Flags = (ProjectileDataFlags)m.ReadByte();

                #endregion
            }
        }
    }
}
