using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

using Lidgren.Network;
using Newtonsoft.Json;

namespace CoopServer
{
    public static class VectorExtensions
    {
        public static LVector3 Normalize(this LVector3 value)
        {
            float value2 = value.Length();
            return value / value2;
        }

        public static float Distance(this LVector3 value1, LVector3 value2)
        {
            LVector3 vector = value1 - value2;
            float num = Dot(vector, vector);
            return (float)Math.Sqrt(num);
        }

        public static float Dot(this LVector3 vector1, LVector3 vector2)
        {
            return vector1.X * vector2.X + vector1.Y * vector2.Y + vector1.Z * vector2.Z;
        }
    }

    public struct LVector3
    {
        public LVector3(float X, float Y, float Z)
        {
            this.X = X;
            this.Y = Y;
            this.Z = Z;
        }

        public float X { get; set; }

        public float Y { get; set; }

        public float Z { get; set; }

        #region SERVER-ONLY
        public float Length() => (float)Math.Sqrt((X * X) + (Y * Y) + (Z * Z));
        public static LVector3 Subtract(LVector3 pos1, LVector3 pos2) => new(pos1.X - pos2.X, pos1.Y - pos2.Y, pos1.Z - pos2.Z);
        public static bool Equals(LVector3 value1, LVector3 value2) => value1.X == value2.X && value1.Y == value2.Y && value1.Z == value2.Z;
        public static LVector3 operator /(LVector3 value1, float value2)
        {
            float num = 1f / value2;
            return new LVector3(value1.X * num, value1.Y * num, value1.Z * num);
        }
        public static LVector3 operator -(LVector3 left, LVector3 right)
        {
            return new LVector3(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
        }
        public static LVector3 operator -(LVector3 value)
        {
            return default(LVector3) - value;
        }
        #endregion
    }

    public struct LQuaternion
    {
        public LQuaternion(float X, float Y, float Z, float W)
        {
            this.X = X;
            this.Y = Y;
            this.Z = Z;
            this.W = W;
        }

        public float X { get; set; }

        public float Y { get; set; }

        public float Z { get; set; }

        public float W { get; set; }
    }

    enum PacketTypes
    {
        HandshakePacket,
        PlayerConnectPacket,
        PlayerDisconnectPacket,
        FullSyncPlayerPacket,
        FullSyncPlayerVehPacket,
        LightSyncPlayerPacket,
        LightSyncPlayerVehPacket,
        SuperLightSyncPacket,
        FullSyncNpcPacket,
        FullSyncNpcVehPacket,
        ChatMessagePacket,
        NativeCallPacket,
        NativeResponsePacket,
        ModPacket,
        CleanUpWorldPacket
    }

    enum ConnectionChannel
    {
        Default = 0,
        PlayerLight = 1,
        PlayerFull = 2,
        PlayerSuperLight = 3,
        NPCFull = 4,
        Chat = 5,
        Native = 6,
        Mod = 7
    }

    [Flags]
    enum PedDataFlags
    {
        IsAiming = 1 << 0,
        IsShooting = 1 << 1,
        IsReloading = 1 << 2,
        IsJumping = 1 << 3,
        IsRagdoll = 1 << 4,
        IsOnFire = 1 << 5,
        IsInParachuteFreeFall = 1 << 6,
        IsOnLadder = 1 << 7,
        IsVaulting = 1 << 8
    }

    #region ===== VEHICLE DATA =====
    [Flags]
    enum VehicleDataFlags
    {
        IsEngineRunning = 1 << 0,
        AreLightsOn = 1 << 1,
        AreHighBeamsOn = 1 << 2,
        IsSirenActive = 1 << 3,
        IsDead = 1 << 4,
        IsHornActive = 1 << 5,
        IsTransformed = 1 << 6,
        RoofOpened = 1 << 7,
        OnTurretSeat = 1 << 8,
        IsPlane = 1 << 9
    }

    struct VehicleDamageModel
    {
        public byte BrokenWindows { get; set; }

        public byte BrokenDoors { get; set; }

        public ushort BurstedTires { get; set; }

        public ushort PuncturedTires { get; set; }
    }
    #endregion

    interface IPacket
    {
        void PacketToNetOutGoingMessage(NetOutgoingMessage message);
        void NetIncomingMessageToPacket(NetIncomingMessage message);
        void NetIncomingMessageToPacket(byte[] array);
    }

    abstract class Packet : IPacket
    {
        public abstract void PacketToNetOutGoingMessage(NetOutgoingMessage message);
        public abstract void NetIncomingMessageToPacket(NetIncomingMessage message);
        public abstract void NetIncomingMessageToPacket(byte[] array);
    }

    class ModPacket : Packet
    {
        public long NetHandle { get; set; }

        public long Target { get; set; }

        public string Mod { get; set; }

        public byte CustomPacketID { get; set; }

        public byte[] Bytes { get; set; }

        public override void PacketToNetOutGoingMessage(NetOutgoingMessage message)
        {
            #region PacketToNetOutGoingMessage
            message.Write((byte)PacketTypes.ModPacket);

            List<byte> byteArray = new List<byte>();

            // Write NetHandle
            byteArray.AddRange(BitConverter.GetBytes(NetHandle));

            // Write Target
            byteArray.AddRange(BitConverter.GetBytes(Target));

            // Write Mod
            byte[] modBytes = Encoding.UTF8.GetBytes(Mod);
            byteArray.AddRange(BitConverter.GetBytes(modBytes.Length));
            byteArray.AddRange(modBytes);

            // Write CustomPacketID
            byteArray.Add(CustomPacketID);

            // Write Bytes
            byteArray.AddRange(BitConverter.GetBytes(Bytes.Length));
            byteArray.AddRange(Bytes);

            byte[] result = byteArray.ToArray();

            message.Write(result.Length);
            message.Write(result);
            #endregion
        }

        public override void NetIncomingMessageToPacket(NetIncomingMessage message)
        {
            throw new NotImplementedException();
        }

        public override void NetIncomingMessageToPacket(byte[] array)
        {
            #region NetIncomingMessageToPacket
            BitReader reader = new BitReader(array);

            // Read NetHandle
            NetHandle = reader.ReadLong();

            // Read Target
            NetHandle = reader.ReadLong();

            // Read Mod
            int modLength = reader.ReadInt();
            Mod = reader.ReadString(modLength);

            // Read CustomPacketID
            CustomPacketID = reader.ReadByte();

            // Read Bytes
            int bytesLength = reader.ReadInt();
            Bytes = reader.ReadByteArray(bytesLength);
            #endregion
        }
    }

    #region -- PLAYER --
    class HandshakePacket : Packet
    {
        public long NetHandle { get; set; }

        public string Username { get; set; }

        public string ModVersion { get; set; }

        public bool NPCsAllowed { get; set; }

        public override void PacketToNetOutGoingMessage(NetOutgoingMessage message)
        {
            #region PacketToNetOutGoingMessage
            message.Write((byte)PacketTypes.HandshakePacket);

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

        public override void NetIncomingMessageToPacket(NetIncomingMessage message)
        {
            throw new NotImplementedException();
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

    class PlayerConnectPacket : Packet
    {
        public long NetHandle { get; set; }

        public string Username { get; set; }

        public override void PacketToNetOutGoingMessage(NetOutgoingMessage message)
        {
            #region PacketToNetOutGoingMessage
            message.Write((byte)PacketTypes.PlayerConnectPacket);

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

        public override void NetIncomingMessageToPacket(NetIncomingMessage message)
        {
            throw new NotImplementedException();
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

    class PlayerDisconnectPacket : Packet
    {
        public long NetHandle { get; set; }

        public override void PacketToNetOutGoingMessage(NetOutgoingMessage message)
        {
            #region PacketToNetOutGoingMessage
            message.Write((byte)PacketTypes.PlayerDisconnectPacket);

            List<byte> byteArray = new List<byte>();

            byteArray.AddRange(BitConverter.GetBytes(NetHandle));

            byte[] result = byteArray.ToArray();

            message.Write(result.Length);
            message.Write(result);
            #endregion
        }

        public override void NetIncomingMessageToPacket(NetIncomingMessage message)
        {
            throw new NotImplementedException();
        }

        public override void NetIncomingMessageToPacket(byte[] array)
        {
            #region NetIncomingMessageToPacket
            BitReader reader = new BitReader(array);

            NetHandle = reader.ReadLong();
            #endregion
        }
    }

    class FullSyncPlayerPacket : Packet
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
            message.Write((byte)PacketTypes.FullSyncPlayerPacket);

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

        public override void NetIncomingMessageToPacket(NetIncomingMessage message)
        {
            throw new NotImplementedException();
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

    class FullSyncPlayerVehPacket : Packet
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
            message.Write((byte)PacketTypes.FullSyncPlayerVehPacket);

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

        public override void NetIncomingMessageToPacket(NetIncomingMessage message)
        {
            throw new NotImplementedException();
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

    class LightSyncPlayerPacket : Packet
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
            message.Write((byte)PacketTypes.LightSyncPlayerPacket);

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

        public override void NetIncomingMessageToPacket(NetIncomingMessage message)
        {
            throw new NotImplementedException();
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

    class LightSyncPlayerVehPacket : Packet
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
            message.Write((byte)PacketTypes.LightSyncPlayerVehPacket);

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

        public override void NetIncomingMessageToPacket(NetIncomingMessage message)
        {
            throw new NotImplementedException();
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

    class SuperLightSyncPacket : Packet
    {
        public long NetHandle { get; set; }

        public LVector3 Position { get; set; }

        public float? Latency { get; set; }

        public override void PacketToNetOutGoingMessage(NetOutgoingMessage message)
        {
            #region PacketToNetOutGoingMessage
            message.Write((byte)PacketTypes.SuperLightSyncPacket);

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

        public override void NetIncomingMessageToPacket(NetIncomingMessage message)
        {
            throw new NotImplementedException();
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

    class ChatMessagePacket : Packet
    {
        public string Username { get; set; }

        public string Message { get; set; }

        public override void PacketToNetOutGoingMessage(NetOutgoingMessage message)
        {
            #region PacketToNetOutGoingMessage
            message.Write((byte)PacketTypes.ChatMessagePacket);

            List<byte> byteArray = new List<byte>();

            byte[] usernameBytes = Encoding.UTF8.GetBytes(Username);
            byte[] messageBytes = Encoding.UTF8.GetBytes(Message);

            // Write UsernameLength
            byteArray.AddRange(BitConverter.GetBytes(usernameBytes.Length));

            // Write Username
            byteArray.AddRange(usernameBytes);

            // Write MessageLength
            byteArray.AddRange(BitConverter.GetBytes(messageBytes.Length));

            // Write Message
            byteArray.AddRange(messageBytes);

            byte[] result = byteArray.ToArray();

            message.Write(result.Length);
            message.Write(result);
            #endregion
        }

        public override void NetIncomingMessageToPacket(NetIncomingMessage message)
        {
            throw new NotImplementedException();
        }

        public override void NetIncomingMessageToPacket(byte[] array)
        {
            #region NetIncomingMessageToPacket
            BitReader reader = new BitReader(array);

            // Read username
            int usernameLength = reader.ReadInt();
            Username = reader.ReadString(usernameLength);

            // Read message
            int messageLength = reader.ReadInt();
            Message = reader.ReadString(messageLength);
            #endregion
        }
    }

    #region ===== NATIVECALL =====
    class NativeCallPacket : Packet
    {
        public ulong Hash { get; set; }

        public List<object> Args { get; set; }

        public override void PacketToNetOutGoingMessage(NetOutgoingMessage message)
        {
            #region PacketToNetOutGoingMessage
            message.Write((byte)PacketTypes.NativeCallPacket);

            List<byte> byteArray = new List<byte>();

            // Write Hash
            byteArray.AddRange(BitConverter.GetBytes(Hash));

            // Write Args
            byteArray.AddRange(BitConverter.GetBytes(Args.Count));
            Args.ForEach(x =>
            {
                Type type = x.GetType();

                if (type == typeof(int))
                {
                    byteArray.Add(0x00);
                    byteArray.AddRange(BitConverter.GetBytes((int)x));
                }
                else if (type == typeof(bool))
                {
                    byteArray.Add(0x01);
                    byteArray.AddRange(BitConverter.GetBytes((bool)x));
                }
                else if (type == typeof(float))
                {
                    byteArray.Add(0x02);
                    byteArray.AddRange(BitConverter.GetBytes((float)x));
                }
                else if (type == typeof(string))
                {
                    byteArray.Add(0x03);
                    byte[] stringBytes = Encoding.UTF8.GetBytes((string)x);
                    byteArray.AddRange(BitConverter.GetBytes(stringBytes.Length));
                    byteArray.AddRange(stringBytes);
                }
                else if (type == typeof(LVector3))
                {
                    byteArray.Add(0x04);
                    LVector3 vector = (LVector3)x;
                    byteArray.AddRange(BitConverter.GetBytes(vector.X));
                    byteArray.AddRange(BitConverter.GetBytes(vector.Y));
                    byteArray.AddRange(BitConverter.GetBytes(vector.Z));
                }
            });

            byte[] result = byteArray.ToArray();

            message.Write(result.Length);
            message.Write(result);
            #endregion
        }

        public override void NetIncomingMessageToPacket(NetIncomingMessage message)
        {
            throw new NotImplementedException();
        }

        public override void NetIncomingMessageToPacket(byte[] array)
        {
            #region NetIncomingMessageToPacket
            BitReader reader = new BitReader(array);

            // Read Hash
            Hash = reader.ReadULong();

            // Read Args
            Args = new List<object>();
            int argsLength = reader.ReadInt();
            for (int i = 0; i < argsLength; i++)
            {
                byte argType = reader.ReadByte();
                switch (argType)
                {
                    case 0x00:
                        Args.Add(reader.ReadInt());
                        break;
                    case 0x01:
                        Args.Add(reader.ReadBool());
                        break;
                    case 0x02:
                        Args.Add(reader.ReadFloat());
                        break;
                    case 0x03:
                        int stringLength = reader.ReadInt();
                        Args.Add(reader.ReadString(stringLength));
                        break;
                    case 0x04:
                        Args.Add(new LVector3()
                        {
                            X = reader.ReadFloat(),
                            Y = reader.ReadFloat(),
                            Z = reader.ReadFloat()
                        });
                        break;
                }
            }
            #endregion
        }
    }

    class NativeResponsePacket : Packet
    {
        public ulong Hash { get; set; }

        public List<object> Args { get; set; }

        public byte? ResultType { get; set; }

        public long ID { get; set; }

        public override void PacketToNetOutGoingMessage(NetOutgoingMessage message)
        {
            #region PacketToNetOutGoingMessage
            message.Write((byte)PacketTypes.NativeResponsePacket);

            List<byte> byteArray = new List<byte>();

            // Write Hash
            byteArray.AddRange(BitConverter.GetBytes(Hash));

            Type type;

            // Write Args
            byteArray.AddRange(BitConverter.GetBytes(Args.Count));
            Args.ForEach(x =>
            {
                type = x.GetType();

                if (type == typeof(int))
                {
                    byteArray.Add(0x00);
                    byteArray.AddRange(BitConverter.GetBytes((int)x));
                }
                else if (type == typeof(bool))
                {
                    byteArray.Add(0x01);
                    byteArray.AddRange(BitConverter.GetBytes((bool)x));
                }
                else if (type == typeof(float))
                {
                    byteArray.Add(0x02);
                    byteArray.AddRange(BitConverter.GetBytes((float)x));
                }
                else if (type == typeof(string))
                {
                    byteArray.Add(0x03);
                    byte[] stringBytes = Encoding.UTF8.GetBytes((string)x);
                    byteArray.AddRange(BitConverter.GetBytes(stringBytes.Length));
                    byteArray.AddRange(stringBytes);
                }
                else if (type == typeof(LVector3))
                {
                    byteArray.Add(0x04);
                    LVector3 vector = (LVector3)x;
                    byteArray.AddRange(BitConverter.GetBytes(vector.X));
                    byteArray.AddRange(BitConverter.GetBytes(vector.Y));
                    byteArray.AddRange(BitConverter.GetBytes(vector.Z));
                }
            });

            byteArray.AddRange(BitConverter.GetBytes(ID));

            // Write type of result
            if (ResultType.HasValue)
            {
                byteArray.Add(ResultType.Value);
            }

            byte[] result = byteArray.ToArray();

            message.Write(result.Length);
            message.Write(result);
            #endregion
        }

        public override void NetIncomingMessageToPacket(NetIncomingMessage message)
        {
            throw new NotImplementedException();
        }

        public override void NetIncomingMessageToPacket(byte[] array)
        {
            #region NetIncomingMessageToPacket
            BitReader reader = new BitReader(array);

            // Read Hash
            Hash = reader.ReadULong();

            // Read Args
            Args = new List<object>();
            int argsLength = reader.ReadInt();
            for (int i = 0; i < argsLength; i++)
            {
                byte argType = reader.ReadByte();
                switch (argType)
                {
                    case 0x00:
                        Args.Add(reader.ReadInt());
                        break;
                    case 0x01:
                        Args.Add(reader.ReadBool());
                        break;
                    case 0x02:
                        Args.Add(reader.ReadFloat());
                        break;
                    case 0x03:
                        int stringLength = reader.ReadInt();
                        Args.Add(reader.ReadString(stringLength));
                        break;
                    case 0x04:
                        Args.Add(new LVector3()
                        {
                            X = reader.ReadFloat(),
                            Y = reader.ReadFloat(),
                            Z = reader.ReadFloat()
                        });
                        break;
                }
            }

            ID = reader.ReadLong();

            // Read type of result
            if (reader.CanRead(1))
            {
                ResultType = reader.ReadByte();
            }
            #endregion
        }
    }
    #endregion // ===== NATIVECALL =====
    #endregion

    #region -- NPC --
    class FullSyncNpcPacket : Packet
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
            message.Write((byte)PacketTypes.FullSyncNpcPacket);

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

        public override void NetIncomingMessageToPacket(NetIncomingMessage message)
        {
            throw new NotImplementedException();
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

    class FullSyncNpcVehPacket : Packet
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
            message.Write((byte)PacketTypes.FullSyncNpcVehPacket);

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

        public override void NetIncomingMessageToPacket(NetIncomingMessage message)
        {
            throw new NotImplementedException();
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
    #endregion

    public static class CoopSerializer
    {
        public static byte[] Serialize(this object obj)
        {
            if (obj == null)
            {
                return null;
            }

            string jsonString = JsonConvert.SerializeObject(obj);
            return System.Text.Encoding.UTF8.GetBytes(jsonString);
        }

        public static T Deserialize<T>(this byte[] bytes) where T : class
        {
            if (bytes == null)
            {
                return null;
            }

            var jsonString = System.Text.Encoding.UTF8.GetString(bytes);
            return JsonConvert.DeserializeObject<T>(jsonString);
        }
    }
}
