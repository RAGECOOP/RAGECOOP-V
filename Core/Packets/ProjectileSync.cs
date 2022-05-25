using System;
using System.Collections.Generic;
using System.Text;
using Lidgren.Network;

namespace RageCoop.Core
{
    public partial class Packets
    {
        public class ProjectileSync : Packet
        {

            public int ID { get; set; }

            public int ShooterID { get; set; }
            public uint WeaponHash { get; set; }

            public LVector3 Position { get; set; }

            public LVector3 Rotation { get; set; }

            public LVector3 Velocity { get; set; }

            public bool Exploded { get; set; }



            public override void Pack(NetOutgoingMessage message)
            {
                #region PacketToNetOutGoingMessage
                message.Write((byte)PacketTypes.ProjectileSync);

                List<byte> byteArray = new List<byte>();

                // Write id
                byteArray.AddInt(ID);

                // Write ShooterID
                byteArray.AddInt(ShooterID);

                byteArray.AddUint(WeaponHash);

                // Write position
                byteArray.AddLVector3(Position);


                // Write rotation
                byteArray.AddLVector3(Rotation);

                // Write velocity
                byteArray.AddLVector3(Velocity);

                if (Exploded) { byteArray.Add(1); }

                byte[] result = byteArray.ToArray();

                message.Write(result.Length);
                message.Write(result);
                #endregion
            }

            public override void Unpack(byte[] array)
            {
                #region NetIncomingMessageToPacket
                BitReader reader = new BitReader(array);

                // Read id
                ID = reader.ReadInt();

                // Read ShooterID
                ShooterID= reader.ReadInt();

                WeaponHash= reader.ReadUInt();

                // Read position
                Position = reader.ReadLVector3();

                // Read rotation
                Rotation = reader.ReadLVector3();

                // Read velocity
                Velocity =reader.ReadLVector3();

                if (reader.CanRead(1))
                {
                    Exploded=true;
                }

                #endregion
            }
        }
    }
}
