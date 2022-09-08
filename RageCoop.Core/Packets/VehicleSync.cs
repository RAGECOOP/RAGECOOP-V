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

            public byte RadioStation { get; set; } = 255;
            public string LicensePlate { get; set; }
            #endregion

            protected override void Serialize(NetOutgoingMessage m)
            {


                m.Write(ID);
                m.Write(OwnerID);
                m.Write((ushort)Flags);
                m.Write(Position);
                m.Write(Quaternion);
                m.Write(Velocity);
                m.Write(RotationVelocity);
                m.Write(ThrottlePower);
                m.Write(BrakePower);
                m.Write(SteeringAngle);

                if (Flags.HasVehFlag(VehicleDataFlags.IsDeluxoHovering))
                {
                    m.Write(DeluxoWingRatio);
                }

                if (Flags.HasVehFlag(VehicleDataFlags.IsFullSync))
                {
                    m.Write(ModelHash);
                    m.Write(EngineHealth);

                    // Check
                    if (Flags.HasVehFlag(VehicleDataFlags.IsAircraft))
                    {
                        // Write the vehicle landing gear
                        m.Write(LandingGear);
                    }
                    if (Flags.HasVehFlag(VehicleDataFlags.HasRoof))
                    {
                        m.Write(RoofState);
                    }

                    // Write vehicle colors
                    m.Write(Colors[0]);
                    m.Write(Colors[1]);

                    // Write vehicle mods
                    // Write the count of mods
                    m.Write((short)Mods.Count);
                    // Loop the dictionary and add the values
                    foreach (KeyValuePair<int, int> mod in Mods)
                    {
                        // Write the mod value
                        m.Write(mod.Key);
                        m.Write(mod.Value);
                    }

                    if (!DamageModel.Equals(default(VehicleDamageModel)))
                    {
                        // Write boolean = true
                        m.Write(true);
                        // Write vehicle damage model
                        m.Write(DamageModel.BrokenDoors);
                        m.Write(DamageModel.OpenedDoors);
                        m.Write(DamageModel.BrokenWindows);
                        m.Write(DamageModel.BurstedTires);
                        m.Write(DamageModel.LeftHeadLightBroken);
                        m.Write(DamageModel.RightHeadLightBroken);
                    }
                    else
                    {
                        // Write boolean = false
                        m.Write(false);
                    }

                    // Write LockStatus
                    m.Write((byte)LockStatus);

                    // Write RadioStation
                    m.Write(RadioStation);

                    //　Write LicensePlate
                    m.Write(LicensePlate);

                    m.Write((byte)(Livery+1));
                }
            }

            public override void Deserialize(NetIncomingMessage m)
            {
                #region NetIncomingMessageToPacket

                ID = m.ReadInt32();
                OwnerID = m.ReadInt32();
                Flags=(VehicleDataFlags)m.ReadUInt16();
                Position = m.ReadVector3();
                Quaternion=m.ReadQuaternion();
                Velocity =m.ReadVector3();
                RotationVelocity=m.ReadVector3();
                ThrottlePower=m.ReadFloat();
                BrakePower=m.ReadFloat();
                SteeringAngle = m.ReadFloat();


                if (Flags.HasVehFlag(VehicleDataFlags.IsDeluxoHovering))
                {
                    DeluxoWingRatio = m.ReadFloat();
                }

                if (Flags.HasVehFlag(VehicleDataFlags.IsFullSync))
                {
                    // Read vehicle model hash
                    ModelHash = m.ReadInt32();

                    // Read vehicle engine health
                    EngineHealth = m.ReadFloat();


                    // Check
                    if (Flags.HasVehFlag(VehicleDataFlags.IsAircraft))
                    {
                        // Read vehicle landing gear
                        LandingGear = m.ReadByte();
                    }
                    if (Flags.HasVehFlag(VehicleDataFlags.HasRoof))
                    {
                        RoofState=m.ReadByte();
                    }

                    // Read vehicle colors
                    byte vehColor1 = m.ReadByte();
                    byte vehColor2 = m.ReadByte();
                    Colors = new byte[] { vehColor1, vehColor2 };

                    // Read vehicle mods
                    // Create new Dictionary
                    Mods = new Dictionary<int, int>();
                    // Read count of mods
                    short vehModCount = m.ReadInt16();
                    // Loop
                    for (int i = 0; i < vehModCount; i++)
                    {
                        // Read the mod value
                        Mods.Add(m.ReadInt32(), m.ReadInt32());
                    }

                    if (m.ReadBoolean())
                    {
                        // Read vehicle damage model
                        DamageModel = new VehicleDamageModel()
                        {
                            BrokenDoors = m.ReadByte(),
                            OpenedDoors=m.ReadByte(),
                            BrokenWindows = m.ReadByte(),
                            BurstedTires = m.ReadInt16(),
                            LeftHeadLightBroken = m.ReadByte(),
                            RightHeadLightBroken = m.ReadByte()
                        };
                    }


                    // Read LockStatus
                    LockStatus=(VehicleLockStatus)m.ReadByte();

                    // Read RadioStation
                    RadioStation=m.ReadByte();

                    LicensePlate=m.ReadString();

                    Livery=(int)(m.ReadByte()-1);
                }
                #endregion
            }
        }
    }
}
