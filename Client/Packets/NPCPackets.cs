using System;
using System.Collections.Generic;

using Lidgren.Network;

namespace CoopClient
{
    internal partial class Packets
    {
        public class FullSyncNpc : Packet
        {
            public long NetHandle { get; set; }

            public int ModelHash { get; set; }

            public Dictionary<byte, short> Clothes { get; set; }

            public int Health { get; set; }

            public LVector3 Position { get; set; }

            public LVector3 Rotation { get; set; }

            public LVector3 Velocity { get; set; }

            public byte Speed { get; set; }

            public LVector3 AimCoords { get; set; }

            public uint CurrentWeaponHash { get; set; }

            public ushort? Flag { get; set; } = 0;

            public override void PacketToNetOutGoingMessage(NetOutgoingMessage message)
            {
                #region PacketToNetOutGoingMessage
                message.Write((byte)PacketTypes.FullSyncNpc);

                List<byte> byteArray = new List<byte>();

                // Write player + ped handle
                byteArray.AddRange(BitConverter.GetBytes(NetHandle));

                // Write npc flags
                byteArray.AddRange(BitConverter.GetBytes(Flag.Value));

                // Write npc model hash
                byteArray.AddRange(BitConverter.GetBytes(ModelHash));

                // Write npc position
                byteArray.AddRange(BitConverter.GetBytes(Position.X));
                byteArray.AddRange(BitConverter.GetBytes(Position.Y));
                byteArray.AddRange(BitConverter.GetBytes(Position.Z));

                // Write npc rotation
                byteArray.AddRange(BitConverter.GetBytes(Rotation.X));
                byteArray.AddRange(BitConverter.GetBytes(Rotation.Y));
                byteArray.AddRange(BitConverter.GetBytes(Rotation.Z));

                // Write npc clothes
                // Write the count of clothes
                byteArray.AddRange(BitConverter.GetBytes((ushort)Clothes.Count));
                // Loop the dictionary and add the values
                foreach (KeyValuePair<byte, short> cloth in Clothes)
                {
                    byteArray.Add(cloth.Key);
                    byteArray.AddRange(BitConverter.GetBytes(cloth.Value));
                }

                // Write npc health
                byteArray.AddRange(BitConverter.GetBytes(Health));

                // Write npc velocity
                byteArray.AddRange(BitConverter.GetBytes(Velocity.X));
                byteArray.AddRange(BitConverter.GetBytes(Velocity.Y));
                byteArray.AddRange(BitConverter.GetBytes(Velocity.Z));

                // Write npc speed
                byteArray.Add(Speed);

                if (Flag.HasValue)
                {
                    if ((Flag.Value & (byte)PedDataFlags.IsAiming) != 0 || (Flag.Value & (byte)PedDataFlags.IsShooting) != 0)
                    {
                        // Write player aim coords
                        byteArray.AddRange(BitConverter.GetBytes(Rotation.X));
                        byteArray.AddRange(BitConverter.GetBytes(Rotation.Y));
                        byteArray.AddRange(BitConverter.GetBytes(Rotation.Z));
                    }
                }

                // Write npc weapon hash
                byteArray.AddRange(BitConverter.GetBytes(CurrentWeaponHash));

                byte[] result = byteArray.ToArray();

                message.Write(result.Length);
                message.Write(result);
                #endregion
            }

            public override void NetIncomingMessageToPacket(byte[] array)
            {
                #region NetIncomingMessageToPacket
                BitReader reader = new BitReader(array);

                // Read player + ped handle
                NetHandle = reader.ReadLong();

                // Read npc flag
                Flag = reader.ReadUShort();

                // Read npc model hash
                ModelHash = reader.ReadInt();

                // Read npc position
                Position = new LVector3()
                {
                    X = reader.ReadFloat(),
                    Y = reader.ReadFloat(),
                    Z = reader.ReadFloat()
                };

                // Read npc rotation
                Rotation = new LVector3()
                {
                    X = reader.ReadFloat(),
                    Y = reader.ReadFloat(),
                    Z = reader.ReadFloat()
                };

                // Read npc clothes
                // Create new Dictionary
                Clothes = new Dictionary<byte, short>();
                // Read the count of clothes
                ushort clothCount = reader.ReadUShort();
                // For clothCount
                for (ushort i = 0; i < clothCount; i++)
                {
                    // Read cloth value
                    Clothes.Add(reader.ReadByte(), reader.ReadShort());
                }

                // Read npc health
                Health = reader.ReadByte();

                // Read npc velocity
                Velocity = new LVector3()
                {
                    X = reader.ReadFloat(),
                    Y = reader.ReadFloat(),
                    Z = reader.ReadFloat()
                };

                // Read npc speed
                Speed = reader.ReadByte();

                // Read npc flag values
                if (Flag.HasValue)
                {
                    if ((Flag.Value & (byte)PedDataFlags.IsAiming) != 0 || (Flag.Value & (byte)PedDataFlags.IsShooting) != 0)
                    {
                        AimCoords = new LVector3()
                        {
                            X = reader.ReadFloat(),
                            Y = reader.ReadFloat(),
                            Z = reader.ReadFloat()
                        };
                    }
                }

                // Read npc weapon hash
                CurrentWeaponHash = reader.ReadUInt();
                #endregion
            }
        }

        public class FullSyncNpcVeh : Packet
        {
            public long NetHandle { get; set; }

            public long VehHandle { get; set; }

            public int ModelHash { get; set; }

            public Dictionary<byte, short> Clothes { get; set; }

            public int Health { get; set; }

            public LVector3 Position { get; set; }

            public int VehModelHash { get; set; }

            public short VehSeatIndex { get; set; }

            public LQuaternion VehRotation { get; set; }

            public float VehEngineHealth { get; set; }

            public float VehRPM { get; set; }

            public LVector3 VehVelocity { get; set; }

            public float VehSpeed { get; set; }

            public float VehSteeringAngle { get; set; }

            public byte[] VehColors { get; set; }

            public Dictionary<int, int> VehMods { get; set; }

            public VehicleDamageModel VehDamageModel { get; set; }

            public byte VehLandingGear { get; set; }

            public ushort? Flag { get; set; }

            public override void PacketToNetOutGoingMessage(NetOutgoingMessage message)
            {
                #region PacketToNetOutGoingMessage
                message.Write((byte)PacketTypes.FullSyncNpcVeh);

                List<byte> byteArray = new List<byte>();

                // Write player + npc netHandle
                byteArray.AddRange(BitConverter.GetBytes(NetHandle));

                // Write player + vehicle handle
                byteArray.AddRange(BitConverter.GetBytes(VehHandle));

                // Write vehicles flags
                byteArray.AddRange(BitConverter.GetBytes(Flag.Value));

                // Write npc health
                byteArray.AddRange(BitConverter.GetBytes(Health));

                // Write npc model hash
                byteArray.AddRange(BitConverter.GetBytes(ModelHash));

                // Write npc clothes
                // Write the count of clothes
                byteArray.AddRange(BitConverter.GetBytes((ushort)Clothes.Count));
                // Loop the dictionary and add the values
                foreach (KeyValuePair<byte, short> cloth in Clothes)
                {
                    byteArray.Add(cloth.Key);
                    byteArray.AddRange(BitConverter.GetBytes(cloth.Value));
                }

                // Write vehicle model hash
                byteArray.AddRange(BitConverter.GetBytes(VehModelHash));

                // Write player seat index
                byteArray.AddRange(BitConverter.GetBytes(VehSeatIndex));

                // Write vehicle position
                byteArray.AddRange(BitConverter.GetBytes(Position.X));
                byteArray.AddRange(BitConverter.GetBytes(Position.Y));
                byteArray.AddRange(BitConverter.GetBytes(Position.Z));

                // Write vehicle rotation
                byteArray.AddRange(BitConverter.GetBytes(VehRotation.X));
                byteArray.AddRange(BitConverter.GetBytes(VehRotation.Y));
                byteArray.AddRange(BitConverter.GetBytes(VehRotation.Z));
                byteArray.AddRange(BitConverter.GetBytes(VehRotation.W));

                // Write vehicle engine health
                byteArray.AddRange(BitConverter.GetBytes(VehEngineHealth));

                // Write vehicle rpm
                byteArray.AddRange(BitConverter.GetBytes(VehRPM));

                // Write vehicle velocity
                byteArray.AddRange(BitConverter.GetBytes(VehVelocity.X));
                byteArray.AddRange(BitConverter.GetBytes(VehVelocity.Y));
                byteArray.AddRange(BitConverter.GetBytes(VehVelocity.Z));

                // Write vehicle speed
                byteArray.AddRange(BitConverter.GetBytes(VehSpeed));

                // Write vehicle steering angle
                byteArray.AddRange(BitConverter.GetBytes(VehSteeringAngle));

                // Check
                if (Flag.HasValue)
                {
                    if ((Flag.Value & (ushort)VehicleDataFlags.IsPlane) != 0)
                    {
                        // Write the vehicle landing gear
                        byteArray.AddRange(BitConverter.GetBytes(VehLandingGear));
                    }
                }

                // Write vehicle colors
                byteArray.Add(VehColors[0]);
                byteArray.Add(VehColors[1]);

                // Write vehicle mods
                // Write the count of mods
                byteArray.AddRange(BitConverter.GetBytes((short)VehMods.Count));
                // Loop the dictionary and add the values
                foreach (KeyValuePair<int, int> mod in VehMods)
                {
                    // Write the mod value
                    byteArray.AddRange(BitConverter.GetBytes(mod.Key));
                    byteArray.AddRange(BitConverter.GetBytes(mod.Value));
                }

                if (!VehDamageModel.Equals(default(VehicleDamageModel)))
                {
                    // Write boolean = true
                    byteArray.Add(0x01);
                    // Write vehicle damage model
                    byteArray.Add(VehDamageModel.BrokenDoors);
                    byteArray.Add(VehDamageModel.BrokenWindows);
                    byteArray.AddRange(BitConverter.GetBytes(VehDamageModel.BurstedTires));
                    byteArray.AddRange(BitConverter.GetBytes(VehDamageModel.PuncturedTires));
                }
                else
                {
                    // Write boolean = false
                    byteArray.Add(0x00);
                }

                byte[] result = byteArray.ToArray();

                message.Write(result.Length);
                message.Write(result);
                #endregion
            }

            public override void NetIncomingMessageToPacket(byte[] array)
            {
                #region NetIncomingMessageToPacket
                BitReader reader = new BitReader(array);

                // Read player + npc netHandle
                NetHandle = reader.ReadLong();

                // Reader player + vehicle handle
                VehHandle = reader.ReadLong();

                // Read vehicle flags
                Flag = reader.ReadUShort();

                // Read npc health
                Health = reader.ReadInt();

                // Read npc model hash
                ModelHash = reader.ReadInt();

                // Read npc clothes
                // Create new Dictionary
                Clothes = new Dictionary<byte, short>();
                // Read the count of clothes
                ushort clothCount = reader.ReadUShort();
                // For clothCount
                for (int i = 0; i < clothCount; i++)
                {
                    // Read cloth value
                    Clothes.Add(reader.ReadByte(), reader.ReadShort());
                }

                // Read vehicle model hash
                VehModelHash = reader.ReadInt();

                // Read npc seat index
                VehSeatIndex = reader.ReadShort();

                // Read vehicle position
                Position = new LVector3()
                {
                    X = reader.ReadFloat(),
                    Y = reader.ReadFloat(),
                    Z = reader.ReadFloat()
                };

                // Read vehicle rotation
                VehRotation = new LQuaternion()
                {
                    X = reader.ReadFloat(),
                    Y = reader.ReadFloat(),
                    Z = reader.ReadFloat(),
                    W = reader.ReadFloat()
                };

                // Read vehicle engine health
                VehEngineHealth = reader.ReadFloat();

                // Read vehicle rpm
                VehRPM = reader.ReadFloat();

                // Read vehicle velocity
                VehVelocity = new LVector3()
                {
                    X = reader.ReadFloat(),
                    Y = reader.ReadFloat(),
                    Z = reader.ReadFloat()
                };

                // Read vehicle speed
                VehSpeed = reader.ReadFloat();

                // Read vehicle steering angle
                VehSteeringAngle = reader.ReadFloat();

                // Check
                if (Flag.HasValue)
                {
                    if ((Flag.Value & (int)VehicleDataFlags.IsPlane) != 0)
                    {
                        // Read vehicle landing gear
                        VehLandingGear = (byte)reader.ReadShort();
                    }
                }

                // Read vehicle colors
                byte vehColor1 = reader.ReadByte();
                byte vehColor2 = reader.ReadByte();
                VehColors = new byte[] { vehColor1, vehColor2 };

                // Read vehicle mods
                // Create new Dictionary
                VehMods = new Dictionary<int, int>();
                // Read count of mods
                short vehModCount = reader.ReadShort();
                // Loop
                for (int i = 0; i < vehModCount; i++)
                {
                    // Read the mod value
                    VehMods.Add(reader.ReadInt(), reader.ReadInt());
                }

                if (reader.ReadBool())
                {
                    // Read vehicle damage model
                    VehDamageModel = new VehicleDamageModel()
                    {
                        BrokenDoors = reader.ReadByte(),
                        BrokenWindows = reader.ReadByte(),
                        BurstedTires = reader.ReadUShort(),
                        PuncturedTires = reader.ReadUShort()
                    };
                }
                #endregion
            }
        }
    }
}
