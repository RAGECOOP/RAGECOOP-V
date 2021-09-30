using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;

using Lidgren.Network;
using ProtoBuf;

namespace CoopServer
{
    [ProtoContract]
    public struct LVector3
    {
        public LVector3(float X, float Y, float Z)
        {
            this.X = X;
            this.Y = Y;
            this.Z = Z;
        }

        [ProtoMember(1)]
        public float X { get; set; }

        [ProtoMember(2)]
        public float Y { get; set; }

        [ProtoMember(3)]
        public float Z { get; set; }

        #region SERVER-ONLY
        public float Length() => (float)Math.Sqrt((X * X) + (Y * Y) + (Z * Z));
        public static LVector3 Subtract(LVector3 pos1, LVector3 pos2) => new(pos1.X - pos2.X, pos1.Y - pos2.Y, pos1.Z - pos2.Z);
        public static bool Equals(LVector3 value1, LVector3 value2) => value1.X == value2.X && value1.Y == value2.Y && value1.Z == value2.Z;
        #endregion
    }

    [ProtoContract]
    public struct LQuaternion
    {
        public LQuaternion(float X, float Y, float Z, float W)
        {
            this.X = X;
            this.Y = Y;
            this.Z = Z;
            this.W = W;
        }

        [ProtoMember(1)]
        public float X { get; set; }

        [ProtoMember(2)]
        public float Y { get; set; }

        [ProtoMember(3)]
        public float Z { get; set; }

        [ProtoMember(4)]
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
        SuperLightSyncPlayerPacket,
        FullSyncNpcPacket,
        FullSyncNpcVehPacket,
        ChatMessagePacket,
        NativeCallPacket,
        ModPacket
    }

    [Flags]
    enum PedDataFlags
    {
        LastSyncWasFull = 1 << 0,
        IsAiming = 1 << 1,
        IsShooting = 1 << 2,
        IsReloading = 1 << 3,
        IsJumping = 1 << 4,
        IsRagdoll = 1 << 5,
        IsOnFire = 1 << 6,
        IsInVehicle = 1 << 7
    }

    #region ===== VEHICLE DATA =====
    [Flags]
    enum VehicleDataFlags
    {
        LastSyncWasFull = 1 << 0,
        IsInVehicle = 1 << 1,
        IsEngineRunning = 1 << 2,
        AreLightsOn = 1 << 3,
        AreHighBeamsOn = 1 << 4,
        IsSirenActive = 1 << 5,
        IsDead = 1 << 6
    }

    [ProtoContract]
    struct VehicleDoors
    {
        [ProtoMember(1)]
        public float AngleRatio { get; set; }

        [ProtoMember(2)]
        public bool Broken { get; set; }

        [ProtoMember(3)]
        public bool Open { get; set; }

        [ProtoMember(4)]
        public bool FullyOpen { get; set; }
    }
    #endregion

    interface IPacket
    {
        void PacketToNetOutGoingMessage(NetOutgoingMessage message);
        void NetIncomingMessageToPacket(NetIncomingMessage message);
    }

    abstract class Packet : IPacket
    {
        public abstract void PacketToNetOutGoingMessage(NetOutgoingMessage message);
        public abstract void NetIncomingMessageToPacket(NetIncomingMessage message);
    }

    [ProtoContract]
    class ModPacket : Packet
    {
        [ProtoMember(1)]
        public long ID { get; set; }

        [ProtoMember(2)]
        public long Target { get; set; }

        [ProtoMember(3)]
        public string Mod { get; set; }

        [ProtoMember(4)]
        public byte CustomPacketID { get; set; }

        [ProtoMember(5)]
        public byte[] Bytes { get; set; }

        public override void PacketToNetOutGoingMessage(NetOutgoingMessage message)
        {
            message.Write((byte)PacketTypes.ModPacket);

            byte[] result = this.Serialize();

            message.Write(result.Length);
            message.Write(result);
        }

        public override void NetIncomingMessageToPacket(NetIncomingMessage message)
        {
            int len = message.ReadInt32();

            ModPacket data = message.ReadBytes(len).Deserialize<ModPacket>();

            ID = data.ID;
            Target = data.Target;
            Mod = data.Mod;
            CustomPacketID = data.CustomPacketID;
            Bytes = data.Bytes;
        }
    }

    #region -- PLAYER --
    [ProtoContract]
    class HandshakePacket : Packet
    {
        [ProtoMember(1)]
        public long ID { get; set; }

        [ProtoMember(2)]
        public string SocialClubName { get; set; }

        [ProtoMember(3)]
        public string Username { get; set; }

        [ProtoMember(4)]
        public string ModVersion { get; set; }

        [ProtoMember(5)]
        public bool NpcsAllowed { get; set; }

        public override void PacketToNetOutGoingMessage(NetOutgoingMessage message)
        {
            message.Write((byte)PacketTypes.HandshakePacket);

            byte[] result = this.Serialize();

            message.Write(result.Length);
            message.Write(result);
        }

        public override void NetIncomingMessageToPacket(NetIncomingMessage message)
        {
            int len = message.ReadInt32();

            HandshakePacket data = message.ReadBytes(len).Deserialize<HandshakePacket>();

            ID = data.ID;
            SocialClubName = data.SocialClubName;
            Username = data.Username;
            ModVersion = data.ModVersion;
            NpcsAllowed = data.NpcsAllowed;
        }
    }

    [ProtoContract]
    class PlayerConnectPacket : Packet
    {
        [ProtoMember(1)]
        public long ID { get; set; }

        [ProtoMember(2)]
        public string SocialClubName { get; set; }

        [ProtoMember(3)]
        public string Username { get; set; }

        public override void PacketToNetOutGoingMessage(NetOutgoingMessage message)
        {
            message.Write((byte)PacketTypes.PlayerConnectPacket);

            byte[] result = this.Serialize();

            message.Write(result.Length);
            message.Write(result);
        }

        public override void NetIncomingMessageToPacket(NetIncomingMessage message)
        {
            int len = message.ReadInt32();

            PlayerConnectPacket data = message.ReadBytes(len).Deserialize<PlayerConnectPacket>();

            ID = data.ID;
            SocialClubName = data.SocialClubName;
            Username = data.Username;
        }
    }

    [ProtoContract]
    class PlayerDisconnectPacket : Packet
    {
        [ProtoMember(1)]
        public long ID { get; set; }

        public override void PacketToNetOutGoingMessage(NetOutgoingMessage message)
        {
            message.Write((byte)PacketTypes.PlayerDisconnectPacket);

            byte[] result = this.Serialize();

            message.Write(result.Length);
            message.Write(result);
        }

        public override void NetIncomingMessageToPacket(NetIncomingMessage message)
        {
            int len = message.ReadInt32();

            PlayerDisconnectPacket data = message.ReadBytes(len).Deserialize<PlayerDisconnectPacket>();

            ID = data.ID;
        }
    }

    [ProtoContract]
    struct PlayerPacket
    {
        [ProtoMember(1)]
        public long ID { get; set; }

        [ProtoMember(2)]
        public int Health { get; set; }

        [ProtoMember(3)]
        public LVector3 Position { get; set; }

        [ProtoMember(4)]
        public float Latency { get; set; }
    }

    [ProtoContract]
    class FullSyncPlayerPacket : Packet
    {
        [ProtoMember(1)]
        public PlayerPacket Extra { get; set; }

        [ProtoMember(2)]
        public int ModelHash { get; set; }

        [ProtoMember(3)]
        public Dictionary<int, int> Props { get; set; }

        [ProtoMember(4)]
        public LVector3 Rotation { get; set; }

        [ProtoMember(5)]
        public LVector3 Velocity { get; set; }

        [ProtoMember(6)]
        public byte Speed { get; set; }

        [ProtoMember(7)]
        public LVector3 AimCoords { get; set; }

        [ProtoMember(8)]
        public int CurrentWeaponHash { get; set; }

        [ProtoMember(9)]
        public byte? Flag { get; set; } = 0;

        public override void PacketToNetOutGoingMessage(NetOutgoingMessage message)
        {
            message.Write((byte)PacketTypes.FullSyncPlayerPacket);

            byte[] result = this.Serialize();

            message.Write(result.Length);
            message.Write(result);
        }

        public override void NetIncomingMessageToPacket(NetIncomingMessage message)
        {
            int len = message.ReadInt32();

            FullSyncPlayerPacket data = message.ReadBytes(len).Deserialize<FullSyncPlayerPacket>();

            Extra = data.Extra;
            ModelHash = data.ModelHash;
            Props = data.Props;
            Rotation = data.Rotation;
            Velocity = data.Velocity;
            Speed = data.Speed;
            AimCoords = data.AimCoords;
            CurrentWeaponHash = data.CurrentWeaponHash;
            Flag = data.Flag;
        }
    }

    [ProtoContract]
    class FullSyncPlayerVehPacket : Packet
    {
        [ProtoMember(1)]
        public PlayerPacket Extra { get; set; }

        [ProtoMember(2)]
        public int ModelHash { get; set; }

        [ProtoMember(3)]
        public Dictionary<int, int> Props { get; set; }

        [ProtoMember(4)]
        public int VehModelHash { get; set; }

        [ProtoMember(5)]
        public int VehSeatIndex { get; set; }

        [ProtoMember(6)]
        public LVector3 VehPosition { get; set; }

        [ProtoMember(7)]
        public LQuaternion VehRotation { get; set; }

        [ProtoMember(8)]
        public float VehEngineHealth { get; set; }

        [ProtoMember(9)]
        public LVector3 VehVelocity { get; set; }

        [ProtoMember(10)]
        public float VehSpeed { get; set; }

        [ProtoMember(11)]
        public float VehSteeringAngle { get; set; }

        [ProtoMember(12)]
        public int[] VehColors { get; set; }

        [ProtoMember(13)]
        public Dictionary<int, int> VehMods { get; set; }

        [ProtoMember(14)]
        public VehicleDoors[] VehDoors { get; set; }

        [ProtoMember(15)]
        public byte? Flag { get; set; } = 0;

        public override void PacketToNetOutGoingMessage(NetOutgoingMessage message)
        {
            message.Write((byte)PacketTypes.FullSyncPlayerVehPacket);

            byte[] result = this.Serialize();

            message.Write(result.Length);
            message.Write(result);
        }

        public override void NetIncomingMessageToPacket(NetIncomingMessage message)
        {
            int len = message.ReadInt32();

            FullSyncPlayerVehPacket data = message.ReadBytes(len).Deserialize<FullSyncPlayerVehPacket>();

            Extra = data.Extra;
            ModelHash = data.ModelHash;
            Props = data.Props;
            VehModelHash = data.VehModelHash;
            VehSeatIndex = data.VehSeatIndex;
            VehPosition = data.VehPosition;
            VehRotation = data.VehRotation;
            VehEngineHealth = data.VehEngineHealth;
            VehVelocity = data.VehVelocity;
            VehSpeed = data.VehSpeed;
            VehSteeringAngle = data.VehSteeringAngle;
            VehColors = data.VehColors;
            VehMods = data.VehMods;
            VehDoors = data.VehDoors;
            Flag = data.Flag;
        }
    }

    [ProtoContract]
    class LightSyncPlayerPacket : Packet
    {
        [ProtoMember(1)]
        public PlayerPacket Extra { get; set; }

        [ProtoMember(2)]
        public LVector3 Rotation { get; set; }

        [ProtoMember(3)]
        public LVector3 Velocity { get; set; }

        [ProtoMember(4)]
        public byte Speed { get; set; }

        [ProtoMember(5)]
        public LVector3 AimCoords { get; set; }

        [ProtoMember(6)]
        public int CurrentWeaponHash { get; set; }

        [ProtoMember(7)]
        public byte? Flag { get; set; } = 0;

        public override void PacketToNetOutGoingMessage(NetOutgoingMessage message)
        {
            message.Write((byte)PacketTypes.LightSyncPlayerPacket);

            byte[] result = this.Serialize();

            message.Write(result.Length);
            message.Write(result);
        }

        public override void NetIncomingMessageToPacket(NetIncomingMessage message)
        {
            int len = message.ReadInt32();

            LightSyncPlayerPacket data = message.ReadBytes(len).Deserialize<LightSyncPlayerPacket>();

            Extra = data.Extra;
            Rotation = data.Rotation;
            Velocity = data.Velocity;
            Speed = data.Speed;
            AimCoords = data.AimCoords;
            CurrentWeaponHash = data.CurrentWeaponHash;
            Flag = data.Flag;
        }
    }

    [ProtoContract]
    class LightSyncPlayerVehPacket : Packet
    {
        [ProtoMember(1)]
        public PlayerPacket Extra { get; set; }

        [ProtoMember(4)]
        public int VehModelHash { get; set; }

        [ProtoMember(5)]
        public int VehSeatIndex { get; set; }

        [ProtoMember(6)]
        public LVector3 VehPosition { get; set; }

        [ProtoMember(7)]
        public LQuaternion VehRotation { get; set; }

        [ProtoMember(8)]
        public LVector3 VehVelocity { get; set; }

        [ProtoMember(9)]
        public float VehSpeed { get; set; }

        [ProtoMember(10)]
        public float VehSteeringAngle { get; set; }

        [ProtoMember(11)]
        public byte? Flag { get; set; } = 0;

        public override void PacketToNetOutGoingMessage(NetOutgoingMessage message)
        {
            message.Write((byte)PacketTypes.LightSyncPlayerVehPacket);

            byte[] result = this.Serialize();

            message.Write(result.Length);
            message.Write(result);
        }

        public override void NetIncomingMessageToPacket(NetIncomingMessage message)
        {
            int len = message.ReadInt32();

            LightSyncPlayerVehPacket data = message.ReadBytes(len).Deserialize<LightSyncPlayerVehPacket>();

            Extra = data.Extra;
            VehModelHash = data.VehModelHash;
            VehSeatIndex = data.VehSeatIndex;
            VehPosition = data.VehPosition;
            VehRotation = data.VehRotation;
            VehVelocity = data.VehVelocity;
            VehSpeed = data.VehSpeed;
            VehSteeringAngle = data.VehSteeringAngle;
            Flag = data.Flag;
        }
    }

    [ProtoContract]
    class SuperLightSyncPlayerPacket : Packet
    {
        [ProtoMember(1)]
        public PlayerPacket Extra { get; set; }

        public override void PacketToNetOutGoingMessage(NetOutgoingMessage message)
        {
            message.Write((byte)PacketTypes.SuperLightSyncPlayerPacket);

            byte[] result = this.Serialize();

            message.Write(result.Length);
            message.Write(result);
        }

        public override void NetIncomingMessageToPacket(NetIncomingMessage message)
        {
            int len = message.ReadInt32();

            SuperLightSyncPlayerPacket data = message.ReadBytes(len).Deserialize<SuperLightSyncPlayerPacket>();

            Extra = data.Extra;
        }
    }

    [ProtoContract]
    class ChatMessagePacket : Packet
    {
        [ProtoMember(1)]
        public string Username { get; set; }

        [ProtoMember(2)]
        public string Message { get; set; }

        public override void PacketToNetOutGoingMessage(NetOutgoingMessage message)
        {
            message.Write((byte)PacketTypes.ChatMessagePacket);

            byte[] result = CoopSerializer.Serialize(this);

            message.Write(result.Length);
            message.Write(result);
        }

        public override void NetIncomingMessageToPacket(NetIncomingMessage message)
        {
            int len = message.ReadInt32();

            ChatMessagePacket data = message.ReadBytes(len).Deserialize<ChatMessagePacket>();

            Username = data.Username;
            Message = data.Message;
        }
    }

    #region ===== NATIVECALL =====
    [ProtoContract]
    class NativeCallPacket : Packet
    {
        [ProtoMember(1)]
        public ulong Hash { get; set; }

        [ProtoMember(2)]
        public List<NativeArgument> Args { get; set; }

        public override void PacketToNetOutGoingMessage(NetOutgoingMessage message)
        {
            message.Write((byte)PacketTypes.NativeCallPacket);

            byte[] result = CoopSerializer.Serialize(this);

            message.Write(result.Length);
            message.Write(result);
        }

        public override void NetIncomingMessageToPacket(NetIncomingMessage message)
        {
            int len = message.ReadInt32();

            NativeCallPacket data = message.ReadBytes(len).Deserialize<NativeCallPacket>();

            Hash = data.Hash;
            Args = data.Args;
        }
    }

    [ProtoContract]
    [ProtoInclude(1, typeof(IntArgument))]
    [ProtoInclude(2, typeof(BoolArgument))]
    [ProtoInclude(3, typeof(FloatArgument))]
    [ProtoInclude(4, typeof(StringArgument))]
    [ProtoInclude(5, typeof(LVector3Argument))]
    class NativeArgument { }

    [ProtoContract]
    class IntArgument : NativeArgument
    {
        [ProtoMember(1)]
        public int Data { get; set; }
    }

    [ProtoContract]
    class BoolArgument : NativeArgument
    {
        [ProtoMember(1)]
        public bool Data { get; set; }
    }

    [ProtoContract]
    class FloatArgument : NativeArgument
    {
        [ProtoMember(1)]
        public float Data { get; set; }
    }

    [ProtoContract]
    class StringArgument : NativeArgument
    {
        [ProtoMember(1)]
        public string Data { get; set; }
    }

    [ProtoContract]
    class LVector3Argument : NativeArgument
    {
        [ProtoMember(1)]
        public LVector3 Data { get; set; }
    }
    #endregion // ===== NATIVECALL =====
    #endregion

    #region -- NPC --
    [ProtoContract]
    class FullSyncNpcPacket : Packet
    {
        [ProtoMember(1)]
        public long ID { get; set; }

        [ProtoMember(2)]
        public int ModelHash { get; set; }

        [ProtoMember(3)]
        public Dictionary<int, int> Props { get; set; }

        [ProtoMember(4)]
        public int Health { get; set; }

        [ProtoMember(5)]
        public LVector3 Position { get; set; }

        [ProtoMember(6)]
        public LVector3 Rotation { get; set; }

        [ProtoMember(7)]
        public LVector3 Velocity { get; set; }

        [ProtoMember(8)]
        public byte Speed { get; set; }

        [ProtoMember(9)]
        public LVector3 AimCoords { get; set; }

        [ProtoMember(10)]
        public int CurrentWeaponHash { get; set; }

        [ProtoMember(11)]
        public byte? Flag { get; set; } = 0;

        public override void PacketToNetOutGoingMessage(NetOutgoingMessage message)
        {
            message.Write((byte)PacketTypes.FullSyncNpcPacket);

            byte[] result = CoopSerializer.Serialize(this);

            message.Write(result.Length);
            message.Write(result);
        }

        public override void NetIncomingMessageToPacket(NetIncomingMessage message)
        {
            int len = message.ReadInt32();

            FullSyncNpcPacket data = message.ReadBytes(len).Deserialize<FullSyncNpcPacket>();

            ID = data.ID;
            ModelHash = data.ModelHash;
            Props = data.Props;
            Health = data.Health;
            Position = data.Position;
            Rotation = data.Rotation;
            Velocity = data.Velocity;
            Speed = data.Speed;
            AimCoords = data.AimCoords;
            CurrentWeaponHash = data.CurrentWeaponHash;
            Flag = data.Flag;
        }
    }

    [ProtoContract]
    class FullSyncNpcVehPacket : Packet
    {
        [ProtoMember(1)]
        public long ID { get; set; }

        [ProtoMember(2)]
        public int ModelHash { get; set; }

        [ProtoMember(3)]
        public Dictionary<int, int> Props { get; set; }

        [ProtoMember(4)]
        public int Health { get; set; }

        [ProtoMember(5)]
        public LVector3 Position { get; set; }

        [ProtoMember(6)]
        public int VehModelHash { get; set; }

        [ProtoMember(7)]
        public int VehSeatIndex { get; set; }

        [ProtoMember(8)]
        public LVector3 VehPosition { get; set; }

        [ProtoMember(9)]
        public LQuaternion VehRotation { get; set; }

        [ProtoMember(10)]
        public float VehEngineHealth { get; set; }

        [ProtoMember(11)]
        public LVector3 VehVelocity { get; set; }

        [ProtoMember(12)]
        public float VehSpeed { get; set; }

        [ProtoMember(13)]
        public float VehSteeringAngle { get; set; }

        [ProtoMember(14)]
        public int[] VehColors { get; set; }

        [ProtoMember(15)]
        public Dictionary<int, int> VehMods { get; set; }

        [ProtoMember(16)]
        public VehicleDoors[] VehDoors { get; set; }

        [ProtoMember(17)]
        public byte? Flag { get; set; } = 0;

        public override void PacketToNetOutGoingMessage(NetOutgoingMessage message)
        {
            message.Write((byte)PacketTypes.FullSyncNpcVehPacket);

            byte[] result = CoopSerializer.Serialize(this);

            message.Write(result.Length);
            message.Write(result);
        }

        public override void NetIncomingMessageToPacket(NetIncomingMessage message)
        {
            int len = message.ReadInt32();

            FullSyncNpcVehPacket data = message.ReadBytes(len).Deserialize<FullSyncNpcVehPacket>();

            ID = data.ID;
            ModelHash = data.ModelHash;
            Props = data.Props;
            Health = data.Health;
            Position = data.Position;
            VehModelHash = data.VehModelHash;
            VehSeatIndex = data.VehSeatIndex;
            VehPosition = data.VehPosition;
            VehRotation = data.VehRotation;
            VehEngineHealth = data.VehEngineHealth;
            VehVelocity = data.VehVelocity;
            VehSpeed = data.VehSpeed;
            VehSteeringAngle = data.VehSteeringAngle;
            VehColors = data.VehColors;
            VehMods = data.VehMods;
            VehDoors = data.VehDoors;
            Flag = data.Flag;
        }
    }
    #endregion

    public static class CoopSerializer
    {
        public static byte[] CSerialize(this object obj)
        {
            if (obj == null)
            {
                return null;
            }

            BinaryFormatter bf = new BinaryFormatter();
            using MemoryStream ms = new MemoryStream();
            bf.Serialize(ms, obj);
            return ms.ToArray();
        }

        public static T CDeserialize<T>(this byte[] bytes) where T : class
        {
            if (bytes == null)
            {
                return null;
            }

            using MemoryStream memStream = new MemoryStream();
            BinaryFormatter binForm = new BinaryFormatter();
            memStream.Write(bytes, 0, bytes.Length);
            memStream.Seek(0, SeekOrigin.Begin);
            T obj = (T)binForm.Deserialize(memStream);
            return obj;
        }

        internal static T Deserialize<T>(this byte[] data) where T : new()
        {
            try
            {
                using MemoryStream stream = new(data);
                return Serializer.Deserialize<T>(stream);
            }
            catch
            {
                throw new Exception(string.Format("The deserialization of the packet {0} failed!", typeof(T).Name));
            }
        }

        internal static byte[] Serialize<T>(this T packet)
        {
            using MemoryStream stream = new();
            Serializer.Serialize(stream, packet);
            return stream.ToArray();
        }
    }
}
