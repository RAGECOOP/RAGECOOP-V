using System;
using System.Collections.Generic;
using System.Text;
using GTA;
using Lidgren.Network;

namespace RageCoop.Core
{
    public partial class Packets
    {
        /// <summary>
        /// Non-critical stuff, such as damage model, landing gear, health, etc..
        /// </summary>
        public class VehicleStateSync : Packet
        {
            public int ID { get; set; }

            // ID of player responsible for syncing this vehicle
            public int OwnerID { get; set; }

            public int ModelHash { get; set; }

            public float EngineHealth { get; set; }

            public byte[] Colors { get; set; }

            public Dictionary<int, int> Mods { get; set; }

            public VehicleDamageModel DamageModel { get; set; }

            public byte LandingGear { get; set; }

            public VehicleDataFlags Flag { get; set; }


            public VehicleLockStatus LockStatus { get; set; }

            /// <summary>
            /// VehicleSeat,PedID
            /// </summary>
            public Dictionary<int, int> Passengers { get; set; }

            public override void Pack(NetOutgoingMessage message)
            {
                #region PacketToNetOutGoingMessage
                message.Write((byte)PacketTypes.VehicleStateSync);

                List<byte> byteArray = new List<byte>();

                // Write player netHandle
                byteArray.AddRange(BitConverter.GetBytes(ID));

                //Write vehicle flag
                byteArray.AddRange(BitConverter.GetBytes((ushort)Flag));

                // Write vehicle model hash
                byteArray.AddRange(BitConverter.GetBytes(ModelHash));


                // Write vehicle engine health
                byteArray.AddRange(BitConverter.GetBytes(EngineHealth));

                // Check
                if (Flag.HasFlag(VehicleDataFlags.IsAircraft))
                {
                    // Write the vehicle landing gear
                    byteArray.AddRange(BitConverter.GetBytes(LandingGear));
                }

                // Write vehicle colors
                byteArray.Add(Colors[0]);
                byteArray.Add(Colors[1]);

                // Write vehicle mods
                // Write the count of mods
                byteArray.AddRange(BitConverter.GetBytes((short)Mods.Count));
                // Loop the dictionary and add the values
                foreach (KeyValuePair<int, int> mod in Mods)
                {
                    // Write the mod value
                    byteArray.AddRange(BitConverter.GetBytes(mod.Key));
                    byteArray.AddRange(BitConverter.GetBytes(mod.Value));
                }

                if (!DamageModel.Equals(default(VehicleDamageModel)))
                {
                    // Write boolean = true
                    byteArray.Add(0x01);
                    // Write vehicle damage model
                    byteArray.Add(DamageModel.BrokenDoors);
                    byteArray.Add(DamageModel.OpenedDoors);
                    byteArray.Add(DamageModel.BrokenWindows);
                    byteArray.AddRange(BitConverter.GetBytes(DamageModel.BurstedTires));
                    byteArray.Add(DamageModel.LeftHeadLightBroken);
                    byteArray.Add(DamageModel.RightHeadLightBroken);
                }
                else
                {
                    // Write boolean = false
                    byteArray.Add(0x00);
                }

                // Write OwnerID
                byteArray.AddRange(BitConverter.GetBytes(OwnerID));

                // Write passengers
                byteArray.AddRange(BitConverter.GetBytes(Passengers.Count));

                foreach (KeyValuePair<int, int> p in Passengers)
                {
                    byteArray.AddRange(BitConverter.GetBytes(p.Key));
                    byteArray.AddRange(BitConverter.GetBytes(p.Value));
                }



                // Write LockStatus
                byteArray.Add((byte)LockStatus);

                byte[] result = byteArray.ToArray();

                message.Write(result.Length);
                message.Write(result);
                #endregion
            }

            public override void Unpack(byte[] array)
            {
                #region NetIncomingMessageToPacket
                BitReader reader = new BitReader(array);

                // Read vehicle id
                ID = reader.ReadInt();

                // Read vehicle flags
                Flag = (VehicleDataFlags)reader.ReadUShort();

                // Read vehicle model hash
                ModelHash = reader.ReadInt();

                // Read vehicle engine health
                EngineHealth = reader.ReadFloat();


                // Check
                if (Flag.HasFlag(VehicleDataFlags.IsAircraft))
                {
                    // Read vehicle landing gear
                    LandingGear = (byte)reader.ReadShort();
                }

                // Read vehicle colors
                byte vehColor1 = reader.ReadByte();
                byte vehColor2 = reader.ReadByte();
                Colors = new byte[] { vehColor1, vehColor2 };

                // Read vehicle mods
                // Create new Dictionary
                Mods = new Dictionary<int, int>();
                // Read count of mods
                short vehModCount = reader.ReadShort();
                // Loop
                for (int i = 0; i < vehModCount; i++)
                {
                    // Read the mod value
                    Mods.Add(reader.ReadInt(), reader.ReadInt());
                }

                if (reader.ReadBool())
                {
                    // Read vehicle damage model
                    DamageModel = new VehicleDamageModel()
                    {
                        BrokenDoors = reader.ReadByte(),
                        OpenedDoors=reader.ReadByte(),
                        BrokenWindows = reader.ReadByte(),
                        BurstedTires = reader.ReadShort(),
                        LeftHeadLightBroken = reader.ReadByte(),
                        RightHeadLightBroken = reader.ReadByte()
                    };
                }

                // Read OwnerID
                OwnerID= reader.ReadInt();


                // Read Passengers
                Passengers=new Dictionary<int, int>();
                int count = reader.ReadInt();
                for (int i = 0; i<count; i++)
                {
                    int seat, id;
                    seat = reader.ReadInt();
                    id = reader.ReadInt();
                    Passengers.Add(seat, id);

                }


                // Read LockStatus
                LockStatus=(VehicleLockStatus)reader.ReadByte();
                #endregion
            }
        }

        public class VehicleSync : Packet
        {
            public int ID { get; set; }

            public LVector3 Position { get; set; }

            public LVector3 Rotation { get; set; }

            public LVector3 Velocity { get; set; }

            public LVector3 RotationVelocity { get; set; }

            public float ThrottlePower { get; set; }
            public float BrakePower { get; set; }
            public float SteeringAngle { get; set; }


            public override void Pack(NetOutgoingMessage message)
            {
                #region PacketToNetOutGoingMessage
                message.Write((byte)PacketTypes.VehicleSync);

                List<byte> byteArray = new List<byte>();

                // Write vehicle id
                byteArray.AddInt(ID);

                // Write position
                byteArray.AddLVector3(Position);


                // Write rotation
                byteArray.AddLVector3(Rotation);

                // Write velocity
                byteArray.AddLVector3(Velocity);

                // Write rotation velocity
                byteArray.AddLVector3(RotationVelocity);


                byteArray.AddFloat(ThrottlePower);

                byteArray.AddFloat(BrakePower);

                // Write vehicle steering angle
                byteArray.AddFloat(SteeringAngle);


                byte[] result = byteArray.ToArray();

                message.Write(result.Length);
                message.Write(result);
                #endregion
            }

            public override void Unpack(byte[] array)
            {
                #region NetIncomingMessageToPacket
                BitReader reader = new BitReader(array);

                // Read vehicle id
                ID = reader.ReadInt();

                // Read position
                Position = reader.ReadLVector3();

                // Read rotation
                Rotation = reader.ReadLVector3();

                // Read velocity
                Velocity =reader.ReadLVector3();

                // Read rotation velocity
                RotationVelocity=reader.ReadLVector3();

                // Read throttle power
                ThrottlePower=reader.ReadFloat();

                // Read brake power
                BrakePower=reader.ReadFloat();

                // Read steering angle
                SteeringAngle = reader.ReadFloat();

                #endregion
            }
        }
    }
}
