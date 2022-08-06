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
            public override PacketType Type  => PacketType.ProjectileSync;
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
                byteArray.Add(Exploded?(byte)1:(byte)0);

                return byteArray.ToArray();

            }

            public override void Deserialize(byte[] array)
            {
                #region NetIncomingMessageToPacket
                BitReader reader = new BitReader(array);

                // Read id
                ID = reader.ReadInt32();

                // Read ShooterID
                ShooterID= reader.ReadInt32();

                WeaponHash= reader.ReadUInt32();

                // Read position
                Position = reader.ReadVector3();

                // Read rotation
                Rotation = reader.ReadVector3();

                // Read velocity
                Velocity =reader.ReadVector3();

                if (reader.ReadBoolean())
                {
                    Exploded=true;
                }

                #endregion
            }
        }
    }
}
