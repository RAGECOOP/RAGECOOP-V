using System;
using System.Collections.Generic;
using System.Text;

using Lidgren.Network;

namespace CoopServer
{
    internal partial class Packets
    {
        public class Handshake : Packet
        {
            public long NetHandle { get; set; }

            public string Username { get; set; }

            public string ModVersion { get; set; }

            public bool NPCsAllowed { get; set; }

            public override void PacketToNetOutGoingMessage(NetOutgoingMessage message)
            {
                #region PacketToNetOutGoingMessage
                message.Write((byte)PacketTypes.Handshake);

                List<byte> byteArray = new List<byte>();

                // Write NetHandle
                byteArray.AddRange(BitConverter.GetBytes(NetHandle));

                // Write Username
                byte[] usernameBytes = Encoding.UTF8.GetBytes(Username);
                byteArray.AddRange(BitConverter.GetBytes(usernameBytes.Length));
                byteArray.AddRange(usernameBytes);

                // Write ModVersion
                byte[] modVersionBytes = Encoding.UTF8.GetBytes(ModVersion);
                byteArray.AddRange(BitConverter.GetBytes(modVersionBytes.Length));
                byteArray.AddRange(modVersionBytes);

                // Write NpcsAllowed
                byteArray.Add(NPCsAllowed ? (byte)0x01 : (byte)0x00);

                byte[] result = byteArray.ToArray();

                message.Write(result.Length);
                message.Write(result);
                #endregion
            }

            public override void NetIncomingMessageToPacket(byte[] array)
            {
                #region NetIncomingMessageToPacket
                BitReader reader = new BitReader(array);

                // Read player netHandle
                NetHandle = reader.ReadLong();

                // Read Username
                int usernameLength = reader.ReadInt();
                Username = reader.ReadString(usernameLength);

                // Read ModVersion
                int modVersionLength = reader.ReadInt();
                ModVersion = reader.ReadString(modVersionLength);

                // Read NPCsAllowed
                NPCsAllowed = reader.ReadBool();
                #endregion
            }
        }

        public class PlayerConnect : Packet
        {
            public long NetHandle { get; set; }

            public string Username { get; set; }

            public override void PacketToNetOutGoingMessage(NetOutgoingMessage message)
            {
                #region PacketToNetOutGoingMessage
                message.Write((byte)PacketTypes.PlayerConnect);

                List<byte> byteArray = new List<byte>();

                // Write NetHandle
                byteArray.AddRange(BitConverter.GetBytes(NetHandle));

                // Get Username bytes
                byte[] usernameBytes = Encoding.UTF8.GetBytes(Username);

                // Write UsernameLength
                byteArray.AddRange(BitConverter.GetBytes(usernameBytes.Length));

                // Write Username
                byteArray.AddRange(usernameBytes);

                byte[] result = byteArray.ToArray();

                message.Write(result.Length);
                message.Write(result);
                #endregion
            }

            public override void NetIncomingMessageToPacket(byte[] array)
            {
                #region NetIncomingMessageToPacket
                BitReader reader = new BitReader(array);

                // Read player netHandle
                NetHandle = reader.ReadLong();

                // Read Username
                int usernameLength = reader.ReadInt();
                Username = reader.ReadString(usernameLength);
                #endregion
            }
        }

        public class PlayerDisconnect : Packet
        {
            public long NetHandle { get; set; }

            public override void PacketToNetOutGoingMessage(NetOutgoingMessage message)
            {
                #region PacketToNetOutGoingMessage
                message.Write((byte)PacketTypes.PlayerDisconnect);

                List<byte> byteArray = new List<byte>();

                byteArray.AddRange(BitConverter.GetBytes(NetHandle));

                byte[] result = byteArray.ToArray();

                message.Write(result.Length);
                message.Write(result);
                #endregion
            }

            public override void NetIncomingMessageToPacket(byte[] array)
            {
                #region NetIncomingMessageToPacket
                BitReader reader = new BitReader(array);

                NetHandle = reader.ReadLong();
                #endregion
            }
        }

        public class FullSyncPlayer : Packet
        {
            public long NetHandle { get; set; }

            public int PedHandle { get; set; }

            public int Health { get; set; }

            public int ModelHash { get; set; }

            public LVector3 Position { get; set; }

            public LVector3 Rotation { get; set; }

            public Dictionary<byte, short> Clothes { get; set; }

            public LVector3 Velocity { get; set; }

            public byte Speed { get; set; }

            public LVector3 AimCoords { get; set; }

            public uint CurrentWeaponHash { get; set; }

            public Dictionary<uint, bool> WeaponComponents { get; set; }

            public ushort? Flag { get; set; }

            public float? Latency { get; set; }

            public override void PacketToNetOutGoingMessage(NetOutgoingMessage message)
            {
                #region PacketToNetOutGoingMessage
                message.Write((byte)PacketTypes.FullSyncPlayer);

                List<byte> byteArray = new List<byte>();

                // Write player netHandle
                byteArray.AddRange(BitConverter.GetBytes(NetHandle));

                // Write player ped handle
                byteArray.AddRange(BitConverter.GetBytes(PedHandle));

                // Write player health
                byteArray.AddRange(BitConverter.GetBytes(Health));

                // Write player flags
                byteArray.AddRange(BitConverter.GetBytes(Flag.Value));

                // Write player model hash
                byteArray.AddRange(BitConverter.GetBytes(ModelHash));

                // Write player position
                byteArray.AddRange(BitConverter.GetBytes(Position.X));
                byteArray.AddRange(BitConverter.GetBytes(Position.Y));
                byteArray.AddRange(BitConverter.GetBytes(Position.Z));

                // Write player rotation
                byteArray.AddRange(BitConverter.GetBytes(Rotation.X));
                byteArray.AddRange(BitConverter.GetBytes(Rotation.Y));
                byteArray.AddRange(BitConverter.GetBytes(Rotation.Z));

                // Write player clothes
                // Write the count of clothes
                byteArray.AddRange(BitConverter.GetBytes((ushort)Clothes.Count));
                // Loop the dictionary and add the values
                foreach (KeyValuePair<byte, short> cloth in Clothes)
                {
                    byteArray.Add(cloth.Key);
                    byteArray.AddRange(BitConverter.GetBytes(cloth.Value));
                }

                // Write player velocity
                byteArray.AddRange(BitConverter.GetBytes(Velocity.X));
                byteArray.AddRange(BitConverter.GetBytes(Velocity.Y));
                byteArray.AddRange(BitConverter.GetBytes(Velocity.Z));

                // Write player speed
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

                // Write player weapon hash
                byteArray.AddRange(BitConverter.GetBytes(CurrentWeaponHash));

                // Write player weapon components
                if (WeaponComponents != null)
                {
                    byteArray.Add(0x01);
                    byteArray.AddRange(BitConverter.GetBytes((ushort)WeaponComponents.Count));
                    foreach (KeyValuePair<uint, bool> component in WeaponComponents)
                    {
                        byteArray.AddRange(BitConverter.GetBytes(component.Key));
                        byteArray.AddRange(BitConverter.GetBytes(component.Value));
                    }
                }
                else
                {
                    // Player weapon doesn't have any components
                    byteArray.Add(0x00);
                }

                // Check if player latency has value
                if (Latency.HasValue)
                {
                    // Write player latency
                    byteArray.AddRange(BitConverter.GetBytes(Latency.Value));
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

                // Read player netHandle
                NetHandle = reader.ReadLong();

                // Read player pedHandle
                PedHandle = reader.ReadInt();

                // Read player health
                Health = reader.ReadInt();

                // Read player flag
                Flag = reader.ReadUShort();

                // Read player model hash
                ModelHash = reader.ReadInt();

                // Read player position
                Position = new LVector3()
                {
                    X = reader.ReadFloat(),
                    Y = reader.ReadFloat(),
                    Z = reader.ReadFloat()
                };

                // Read player rotation
                Rotation = new LVector3()
                {
                    X = reader.ReadFloat(),
                    Y = reader.ReadFloat(),
                    Z = reader.ReadFloat()
                };

                // Read player clothes
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

                // Read player velocity
                Velocity = new LVector3()
                {
                    X = reader.ReadFloat(),
                    Y = reader.ReadFloat(),
                    Z = reader.ReadFloat()
                };

                // Read player speed
                Speed = reader.ReadByte();

                // Read player flag values
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

                // Read player weapon hash
                CurrentWeaponHash = reader.ReadUInt();

                // Read player weapon components
                if (reader.ReadBool())
                {
                    WeaponComponents = new Dictionary<uint, bool>();
                    ushort comCount = reader.ReadUShort();
                    for (ushort i = 0; i < comCount; i++)
                    {
                        WeaponComponents.Add(reader.ReadUInt(), reader.ReadBool());
                    }
                }

                // Try to read latency
                if (reader.CanRead(4))
                {
                    // Read player latency
                    Latency = reader.ReadFloat();
                }
                #endregion
            }
        }

        public class FullSyncPlayerVeh : Packet
        {
            public long NetHandle { get; set; }

            public int PedHandle { get; set; }

            public int Health { get; set; }

            public int VehicleHandle { get; set; }

            public int ModelHash { get; set; }

            public Dictionary<byte, short> Clothes { get; set; }

            public int VehModelHash { get; set; }

            public short VehSeatIndex { get; set; }

            public LVector3 Position { get; set; }

            public LQuaternion VehRotation { get; set; }

            public float VehEngineHealth { get; set; }

            public float VehRPM { get; set; }

            public LVector3 VehVelocity { get; set; }

            public float VehSpeed { get; set; }

            public float VehSteeringAngle { get; set; }

            public LVector3 VehAimCoords { get; set; }

            public byte[] VehColors { get; set; }

            public Dictionary<int, int> VehMods { get; set; }

            public VehicleDamageModel VehDamageModel { get; set; }

            public byte VehLandingGear { get; set; }

            public ushort? Flag { get; set; }

            public float? Latency { get; set; }

            public override void PacketToNetOutGoingMessage(NetOutgoingMessage message)
            {
                #region PacketToNetOutGoingMessage
                message.Write((byte)PacketTypes.FullSyncPlayerVeh);

                List<byte> byteArray = new List<byte>();

                // Write player netHandle
                byteArray.AddRange(BitConverter.GetBytes(NetHandle));

                // Write player ped handle
                byteArray.AddRange(BitConverter.GetBytes(PedHandle));

                // Write vehicles flags
                byteArray.AddRange(BitConverter.GetBytes(Flag.Value));

                // Write player health
                byteArray.AddRange(BitConverter.GetBytes(Health));

                // Write player ped handle
                byteArray.AddRange(BitConverter.GetBytes(VehicleHandle));

                // Write player model hash
                byteArray.AddRange(BitConverter.GetBytes(ModelHash));

                // Write player clothes
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
                    if ((Flag.Value & (ushort)VehicleDataFlags.OnTurretSeat) != 0)
                    {
                        // Write player aim coords
                        byteArray.AddRange(BitConverter.GetBytes(VehAimCoords.X));
                        byteArray.AddRange(BitConverter.GetBytes(VehAimCoords.Y));
                        byteArray.AddRange(BitConverter.GetBytes(VehAimCoords.Z));
                    }

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

                // Check if player latency has value
                if (Latency.HasValue)
                {
                    // Write player latency
                    byteArray.AddRange(BitConverter.GetBytes(Latency.Value));
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

                // Read player netHandle
                NetHandle = reader.ReadLong();

                // Read player ped handle
                PedHandle = reader.ReadInt();

                // Read vehicle flags
                Flag = reader.ReadUShort();

                // Read player health
                Health = reader.ReadInt();

                // Read player vehicle handle
                VehicleHandle = reader.ReadInt();

                // Read player model hash
                ModelHash = reader.ReadInt();

                // Read player clothes
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

                // Read vehicle model hash
                VehModelHash = reader.ReadInt();

                // Read player seat index
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
                    if ((Flag.Value & (int)VehicleDataFlags.OnTurretSeat) != 0)
                    {
                        // Read vehicle aim coords
                        VehAimCoords = new LVector3()
                        {
                            X = reader.ReadFloat(),
                            Y = reader.ReadFloat(),
                            Z = reader.ReadFloat()
                        };
                    }

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

                // Try to read latency
                if (reader.CanRead(4))
                {
                    // Read player latency
                    Latency = reader.ReadFloat();
                }
                #endregion
            }
        }

        public class LightSyncPlayer : Packet
        {
            public long NetHandle { get; set; }

            public int Health { get; set; }

            public LVector3 Position { get; set; }

            public LVector3 Rotation { get; set; }

            public LVector3 Velocity { get; set; }

            public byte Speed { get; set; }

            public LVector3 AimCoords { get; set; }

            public uint CurrentWeaponHash { get; set; }

            public ushort? Flag { get; set; }

            public float? Latency { get; set; }

            public override void PacketToNetOutGoingMessage(NetOutgoingMessage message)
            {
                #region PacketToNetOutGoingMessage
                message.Write((byte)PacketTypes.LightSyncPlayer);

                List<byte> byteArray = new List<byte>();

                // Write player netHandle
                byteArray.AddRange(BitConverter.GetBytes(NetHandle));

                // Write player flags
                byteArray.AddRange(BitConverter.GetBytes(Flag.Value));

                // Write player health
                byteArray.AddRange(BitConverter.GetBytes(Health));

                // Write player position
                byteArray.AddRange(BitConverter.GetBytes(Position.X));
                byteArray.AddRange(BitConverter.GetBytes(Position.Y));
                byteArray.AddRange(BitConverter.GetBytes(Position.Z));

                // Write player rotation
                byteArray.AddRange(BitConverter.GetBytes(Rotation.X));
                byteArray.AddRange(BitConverter.GetBytes(Rotation.Y));
                byteArray.AddRange(BitConverter.GetBytes(Rotation.Z));

                // Write player velocity
                byteArray.AddRange(BitConverter.GetBytes(Velocity.X));
                byteArray.AddRange(BitConverter.GetBytes(Velocity.Y));
                byteArray.AddRange(BitConverter.GetBytes(Velocity.Z));

                // Write player speed
                byteArray.Add(Speed);

                // Write player weapon hash
                byteArray.AddRange(BitConverter.GetBytes(CurrentWeaponHash));

                if (Flag.HasValue && ((Flag.Value & (int)PedDataFlags.IsAiming) != 0 || (Flag.Value & (int)PedDataFlags.IsShooting) != 0))
                {
                    // Write player aim coords
                    byteArray.AddRange(BitConverter.GetBytes(AimCoords.X));
                    byteArray.AddRange(BitConverter.GetBytes(AimCoords.Y));
                    byteArray.AddRange(BitConverter.GetBytes(AimCoords.Z));
                }

                // Check if player latency has value
                if (Latency.HasValue)
                {
                    // Write player latency
                    byteArray.AddRange(BitConverter.GetBytes(Latency.Value));
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

                // Read player netHandle
                NetHandle = reader.ReadLong();

                // Read player flags
                Flag = reader.ReadUShort();

                // Read player health
                Health = reader.ReadInt();

                // Read player position
                Position = new LVector3()
                {
                    X = reader.ReadFloat(),
                    Y = reader.ReadFloat(),
                    Z = reader.ReadFloat()
                };

                // Read player rotation
                Rotation = new LVector3()
                {
                    X = reader.ReadFloat(),
                    Y = reader.ReadFloat(),
                    Z = reader.ReadFloat()
                };

                // Read player velocity
                Velocity = new LVector3()
                {
                    X = reader.ReadFloat(),
                    Y = reader.ReadFloat(),
                    Z = reader.ReadFloat()
                };

                // Read player speed
                Speed = reader.ReadByte();

                // Read player weapon hash
                CurrentWeaponHash = reader.ReadUInt();

                // Try to read aim coords
                if (Flag.HasValue && ((Flag.Value & (int)PedDataFlags.IsAiming) != 0 || (Flag.Value & (int)PedDataFlags.IsShooting) != 0))
                {
                    // Read player aim coords
                    AimCoords = new LVector3()
                    {
                        X = reader.ReadFloat(),
                        Y = reader.ReadFloat(),
                        Z = reader.ReadFloat()
                    };
                }

                // Try to read latency
                if (reader.CanRead(4))
                {
                    // Read player latency
                    Latency = reader.ReadFloat();
                }
                #endregion
            }
        }

        public class LightSyncPlayerVeh : Packet
        {
            public long NetHandle { get; set; }

            public int Health { get; set; }

            public int VehModelHash { get; set; }

            public short VehSeatIndex { get; set; }

            public LVector3 Position { get; set; }

            public LQuaternion VehRotation { get; set; }

            public LVector3 VehVelocity { get; set; }

            public float VehSpeed { get; set; }

            public float VehSteeringAngle { get; set; }

            public LVector3 AimCoords { get; set; }

            public ushort? Flag { get; set; }

            public float? Latency { get; set; }

            public override void PacketToNetOutGoingMessage(NetOutgoingMessage message)
            {
                #region PacketToNetOutGoingMessage
                message.Write((byte)PacketTypes.LightSyncPlayerVeh);

                List<byte> byteArray = new List<byte>();

                // Write player netHandle
                byteArray.AddRange(BitConverter.GetBytes(NetHandle));

                // Write vehicle flags
                byteArray.AddRange(BitConverter.GetBytes(Flag.Value));

                // Write player health
                byteArray.AddRange(BitConverter.GetBytes(Health));

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
                    if ((Flag.Value & (ushort)VehicleDataFlags.OnTurretSeat) != 0)
                    {
                        // Write player aim coords
                        byteArray.AddRange(BitConverter.GetBytes(AimCoords.X));
                        byteArray.AddRange(BitConverter.GetBytes(AimCoords.Y));
                        byteArray.AddRange(BitConverter.GetBytes(AimCoords.Z));
                    }
                }

                // Check if player latency has value
                if (Latency.HasValue)
                {
                    // Write player latency
                    byteArray.AddRange(BitConverter.GetBytes(Latency.Value));
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

                // Read player netHandle
                NetHandle = reader.ReadLong();

                // Read player flags
                Flag = reader.ReadUShort();

                // Read player health
                Health = reader.ReadInt();

                // Read vehicle model hash
                VehModelHash = reader.ReadInt();

                // Read player seat index
                VehSeatIndex = reader.ReadShort();

                // Read player position
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
                    if ((Flag.Value & (ushort)VehicleDataFlags.OnTurretSeat) != 0)
                    {
                        AimCoords = new LVector3()
                        {
                            X = reader.ReadFloat(),
                            Y = reader.ReadFloat(),
                            Z = reader.ReadFloat()
                        };
                    }
                }

                // Try to read latency
                if (reader.CanRead(4))
                {
                    // Read player latency
                    Latency = reader.ReadFloat();
                }
                #endregion
            }
        }

        public class SuperLightSync : Packet
        {
            public long NetHandle { get; set; }

            public LVector3 Position { get; set; }

            public float? Latency { get; set; }

            public override void PacketToNetOutGoingMessage(NetOutgoingMessage message)
            {
                #region PacketToNetOutGoingMessage
                message.Write((byte)PacketTypes.SuperLightSync);

                List<byte> byteArray = new List<byte>();

                // Write player netHandle
                byteArray.AddRange(BitConverter.GetBytes(NetHandle));

                // Write player position
                byteArray.AddRange(BitConverter.GetBytes(Position.X));
                byteArray.AddRange(BitConverter.GetBytes(Position.Y));
                byteArray.AddRange(BitConverter.GetBytes(Position.Z));

                // Write player latency
                if (Latency.HasValue)
                {
                    byteArray.AddRange(BitConverter.GetBytes(Latency.Value));
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

                // Read player netHandle
                NetHandle = reader.ReadLong();

                // Read player position
                Position = new LVector3
                {
                    X = reader.ReadFloat(),
                    Y = reader.ReadFloat(),
                    Z = reader.ReadFloat()
                };

                // Read player latency
                if (reader.CanRead(4))
                {
                    Latency = reader.ReadFloat();
                }
                #endregion
            }
        }
    }
}
