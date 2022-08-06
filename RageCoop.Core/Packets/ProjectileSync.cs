using System;
using System.Collections.Generic;
using System.Text;
using GTA.Math;
using Lidgren.Network;

namespace RageCoop.Core
{
    internal partial class Packets
    {
        internal class ProjectileSync : Packet
        {
            public override PacketType Type { get { return PacketType.ProjectileSync; } }
            public int ID { get; set; }

            public int ShooterID { get; set; }
            public uint WeaponHash { get; set; }

            public Vector3 Position { get; set; }

            public Vector3 Rotation { get; set; }

            public Vector3 Velocity { get; set; }

            public bool Exploded { get; set; }



            public override byte[] Serialize()
            {

                List<byte> byteArray = new List<byte>();

                // Write id
                byteArray.AddInt(ID);

                // Write ShooterID
                byteArray.AddInt(ShooterID);

                byteArray.AddUint(WeaponHash);

                // Write position
                byteArray.AddVector3(Position);


                // Write rotation
                byteArray.AddVector3(Rotation);

                // Write velocity
                byteArray.AddVector3(Velocity);

                if (Exploded) { byteArray.Add(1); }

                return byteArray.ToArray();

            }

            public override void Deserialize(byte[] array)
            {
                #region NetIncomingMessageToPacket
                BitReader reader = new BitReader(array);

                // Read id
                ID = reader.ReadInt();

                // Read ShooterID
                ShooterID= reader.ReadInt();

                WeaponHash= reader.ReadUInt();

                // Read position
                Position = reader.ReadVector3();

                // Read rotation
                Rotation = reader.ReadVector3();

                // Read velocity
                Velocity =reader.ReadVector3();

                if (reader.CanRead(1))
                {
                    Exploded=true;
                }

                #endregion
            }
        }
    }
}
