using System;
using System.Collections.Generic;
using System.Text;
using GTA;
using GTA.Math;
using Lidgren.Network;
using System.Linq;

namespace RageCoop.Core
{
    internal partial class Packets
    {

        public class VehicleSync : Packet
        {
            public override PacketType Type  => PacketType.VehicleSync;
            public int ID { get; set; }

            public int OwnerID { get; set; }

            public VehicleDataFlags Flags { get; set; }

            public Vector3 Position { get; set; }

            public Quaternion Quaternion { get; set; }
            // public Vector3 Rotation { get; set; }

            public Vector3 Velocity { get; set; }
            public Vector3 Acceleration { get; set; }

            public Vector3 RotationVelocity { get; set; }

            public float ThrottlePower { get; set; }
            public float BrakePower { get; set; }
            public float SteeringAngle { get; set; }
            public float DeluxoWingRatio { get; set; } = -1;

            #region FULL-SYNC
            public int ModelHash { get; set; }

            public float EngineHealth { get; set; }

            public byte[] Colors { get; set; }

            public Dictionary<int, int> Mods { get; set; }

            public VehicleDamageModel DamageModel { get; set; }

            public byte LandingGear { get; set; }
            public byte RoofState { get; set; }



            public VehicleLockStatus LockStatus { get; set; }

            public int Livery { get; set; } = -1;

            /// <summary>
            /// VehicleSeat,PedID
            /// </summary>
            public Dictionary<int, int> Passengers { get; set; }

            public byte RadioStation { get; set; } = 255;
            public string LicensePlate { get; set; }
            #endregion

            public override byte[] Serialize()
            {

                List<byte> byteArray = new List<byte>(100);

                byteArray.AddInt(ID);
                byteArray.AddInt(OwnerID);
                byteArray.AddUshort((ushort)Flags);
                byteArray.AddVector3(Position);
                byteArray.AddQuaternion(Quaternion);
                byteArray.AddVector3(Velocity);
                byteArray.AddVector3(Acceleration);
                byteArray.AddVector3(RotationVelocity);
                byteArray.AddFloat(ThrottlePower);
                byteArray.AddFloat(BrakePower);
                byteArray.AddFloat(SteeringAngle);

                if (Flags.HasVehFlag(VehicleDataFlags.IsDeluxoHovering))
                {
                    byteArray.AddFloat(DeluxoWingRatio);
                }

                if (Flags.HasVehFlag(VehicleDataFlags.IsFullSync))
                {
                    byteArray.AddInt(ModelHash);
                    byteArray.AddFloat(EngineHealth);

                    // Check
                    if (Flags.HasVehFlag(VehicleDataFlags.IsAircraft))
                    {
                        // Write the vehicle landing gear
                        byteArray.Add(LandingGear);
                    }
                    if (Flags.HasVehFlag(VehicleDataFlags.HasRoof))
                    {
                        byteArray.Add(RoofState);
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

                    // Write passengers
                    byteArray.AddRange(BitConverter.GetBytes(Passengers.Count));

                    foreach (KeyValuePair<int, int> p in Passengers)
                    {
                        byteArray.AddRange(BitConverter.GetBytes(p.Key));
                        byteArray.AddRange(BitConverter.GetBytes(p.Value));
                    }



                    // Write LockStatus
                    byteArray.Add((byte)LockStatus);

                    // Write RadioStation
                    byteArray.Add(RadioStation);

                    //　Write LicensePlate
                    while (LicensePlate.Length<8)
                    {
                        LicensePlate+=" ";
                    }
                    if (LicensePlate.Length>8)
                    {
                        LicensePlate=new string(LicensePlate.Take(8).ToArray());
                    }
                    byteArray.AddRange(Encoding.ASCII.GetBytes(LicensePlate));

                    byteArray.Add((byte)(Livery+1));
                }
                return byteArray.ToArray();
            }

            public override void Deserialize(byte[] array)
            {
                #region NetIncomingMessageToPacket
                BitReader reader = new BitReader(array);

                // Read vehicle id
                ID = reader.ReadInt32();

                OwnerID = reader.ReadInt32();

                Flags=(VehicleDataFlags)reader.ReadUInt16();

                // Read position
                Position = reader.ReadVector3();

                // Read quaternion
                Quaternion=reader.ReadQuaternion();

                // Read velocity
                Velocity =reader.ReadVector3();

                Acceleration=reader.ReadVector3();

                // Read rotation velocity
                RotationVelocity=reader.ReadVector3();

                // Read throttle power
                ThrottlePower=reader.ReadSingle();

                // Read brake power
                BrakePower=reader.ReadSingle();

                // Read steering angle
                SteeringAngle = reader.ReadSingle();


                if (Flags.HasVehFlag(VehicleDataFlags.IsDeluxoHovering))
                {
                    DeluxoWingRatio = reader.ReadSingle();
                }

                if (Flags.HasVehFlag(VehicleDataFlags.IsFullSync))
                {
                    // Read vehicle model hash
                    ModelHash = reader.ReadInt32();

                    // Read vehicle engine health
                    EngineHealth = reader.ReadSingle();


                    // Check
                    if (Flags.HasVehFlag(VehicleDataFlags.IsAircraft))
                    {
                        // Read vehicle landing gear
                        LandingGear = reader.ReadByte();
                    }
                    if (Flags.HasVehFlag(VehicleDataFlags.HasRoof))
                    {
                        RoofState=reader.ReadByte();
                    }

                    // Read vehicle colors
                    byte vehColor1 = reader.ReadByte();
                    byte vehColor2 = reader.ReadByte();
                    Colors = new byte[] { vehColor1, vehColor2 };

                    // Read vehicle mods
                    // Create new Dictionary
                    Mods = new Dictionary<int, int>();
                    // Read count of mods
                    short vehModCount = reader.ReadInt16();
                    // Loop
                    for (int i = 0; i < vehModCount; i++)
                    {
                        // Read the mod value
                        Mods.Add(reader.ReadInt32(), reader.ReadInt32());
                    }

                    if (reader.ReadBoolean())
                    {
                        // Read vehicle damage model
                        DamageModel = new VehicleDamageModel()
                        {
                            BrokenDoors = reader.ReadByte(),
                            OpenedDoors=reader.ReadByte(),
                            BrokenWindows = reader.ReadByte(),
                            BurstedTires = reader.ReadInt16(),
                            LeftHeadLightBroken = reader.ReadByte(),
                            RightHeadLightBroken = reader.ReadByte()
                        };
                    }


                    // Read Passengers
                    Passengers=new Dictionary<int, int>();
                    int count = reader.ReadInt32();
                    for (int i = 0; i<count; i++)
                    {
                        int seat, id;
                        seat = reader.ReadInt32();
                        id = reader.ReadInt32();
                        Passengers.Add(seat, id);

                    }


                    // Read LockStatus
                    LockStatus=(VehicleLockStatus)reader.ReadByte();

                    // Read RadioStation
                    RadioStation=reader.ReadByte();

                    LicensePlate=Encoding.ASCII.GetString(reader.ReadBytes(8));

                    Livery=(int)(reader.ReadByte()-1);
                }
                #endregion
            }
        }
    }
}
