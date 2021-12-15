using System;
using System.IO;
using System.Collections.Generic;

using Lidgren.Network;
using ProtoBuf;
using Newtonsoft.Json;

using GTA.Math;

namespace CoopClient
{
    #region CLIENT-ONLY
    static class VectorExtensions
    {
        public static Vector3 ToVector(this Quaternion vec)
        {
            return new Vector3()
            {
                X = vec.X,
                Y = vec.Y,
                Z = vec.Z
            };
        }

        public static Quaternion ToQuaternion(this Vector3 vec, float vW = 0.0f)
        {
            return new Quaternion()
            {
                X = vec.X,
                Y = vec.Y,
                Z = vec.Z,
                W = vW
            };
        }

        public static LVector3 ToLVector(this Vector3 vec)
        {
            return new LVector3()
            {
                X = vec.X,
                Y = vec.Y,
                Z = vec.Z
            };
        }

        public static LQuaternion ToLQuaternion(this Quaternion vec)
        {
            return new LQuaternion()
            {
                X = vec.X,
                Y = vec.Y,
                Z = vec.Z,
                W = vec.W
            };
        }
    }
    #endregion

    [ProtoContract]
    struct LVector3
    {
        #region CLIENT-ONLY
        public Vector3 ToVector()
        {
            return new Vector3(X, Y, Z);
        }
        #endregion

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
    }

    [ProtoContract]
    struct LQuaternion
    {
        #region CLIENT-ONLY
        public Quaternion ToQuaternion()
        {
            return new Quaternion(X, Y, Z, W);
        }
        #endregion

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
        NativeResponsePacket,
        ModPacket
    }

    enum ConnectionChannel
    {
        Default = 0,
        Player = 1,
        NPC = 2,
        Chat = 3,
        Native = 4,
        Mod = 5
    }

    [Flags]
    enum PedDataFlags
    {
        IsAiming = 1 << 0,
        IsShooting = 1 << 1,
        IsReloading = 1 << 2,
        IsJumping = 1 << 3,
        IsRagdoll = 1 << 4,
        IsOnFire = 1 << 5
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
        IsTransformed = 1 << 6
    }

    /// <summary>
    /// ?
    /// </summary>
    [ProtoContract]
    public struct VehicleDoors
    {
        #region CLIENT-ONLY
        /// <summary>
        /// ?
        /// </summary>
        public VehicleDoors(float angleRatio = 0f, bool broken = false, bool open = false, bool fullyOpen = false)
        {
            AngleRatio = angleRatio;
            Broken = broken;
            Open = open;
            FullyOpen = fullyOpen;
        }
        #endregion

        /// <summary>
        /// ?
        /// </summary>
        [ProtoMember(1)]
        public float AngleRatio { get; set; }

        /// <summary>
        /// ?
        /// </summary>
        [ProtoMember(2)]
        public bool Broken { get; set; }

        /// <summary>
        /// ?
        /// </summary>
        [ProtoMember(3)]
        public bool Open { get; set; }

        /// <summary>
        /// ?
        /// </summary>
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
        public long NetHandle { get; set; }

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

            NetHandle = data.NetHandle;
            Target = data.Target;
            Mod = data.Mod;
            CustomPacketID =  data.CustomPacketID;
            Bytes = data.Bytes;
        }
    }

    #region -- PLAYER --
    [ProtoContract]
    class HandshakePacket : Packet
    {
        [ProtoMember(1)]
        public long NetHandle { get; set; }

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

            NetHandle = data.NetHandle;
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
        public long NetHandle { get; set; }

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

            NetHandle = data.NetHandle;
            SocialClubName = data.SocialClubName;
            Username = data.Username;
        }
    }

    [ProtoContract]
    class PlayerDisconnectPacket : Packet
    {
        [ProtoMember(1)]
        public long NetHandle { get; set; }

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

            NetHandle = data.NetHandle;
        }
    }

    [ProtoContract]
    struct PlayerPacket
    {
        [ProtoMember(1)]
        public long NetHandle { get; set; }

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
        public List<uint> WeaponComponents { get; set; }

        [ProtoMember(10)]
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
            WeaponComponents = data.WeaponComponents;
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
        public float VehRPM { get; set; }

        [ProtoMember(10)]
        public LVector3 VehVelocity { get; set; }

        [ProtoMember(11)]
        public float VehSpeed { get; set; }

        [ProtoMember(12)]
        public float VehSteeringAngle { get; set; }

        [ProtoMember(13)]
        public LVector3 VehAimCoords { get; set; }

        [ProtoMember(14)]
        public int[] VehColors { get; set; }

        [ProtoMember(15)]
        public Dictionary<int, int> VehMods { get; set; }

        [ProtoMember(16)]
        public VehicleDoors[] VehDoors { get; set; }

        [ProtoMember(17)]
        public int VehTires { get; set; }

        [ProtoMember(18)]
        public byte VehLandingGear { get; set; }

        [ProtoMember(19)]
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
            VehRPM = data.VehRPM;
            VehVelocity = data.VehVelocity;
            VehSpeed = data.VehSpeed;
            VehSteeringAngle = data.VehSteeringAngle;
            VehAimCoords = data.VehAimCoords;
            VehColors = data.VehColors;
            VehMods = data.VehMods;
            VehDoors = data.VehDoors;
            VehTires = data.VehTires;
            VehLandingGear = data.VehLandingGear;
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
    class NativeResponsePacket : Packet
    {
        [ProtoMember(1)]
        public ulong Hash { get; set; }

        [ProtoMember(2)]
        public List<NativeArgument> Args { get; set; }

        [ProtoMember(3)]
        public NativeArgument Type { get; set; }

        [ProtoMember(4)]
        public long NetHandle { get; set; }

        public override void PacketToNetOutGoingMessage(NetOutgoingMessage message)
        {
            message.Write((byte)PacketTypes.NativeResponsePacket);

            byte[] result = CoopSerializer.Serialize(this);

            message.Write(result.Length);
            message.Write(result);
        }

        public override void NetIncomingMessageToPacket(NetIncomingMessage message)
        {
            int len = message.ReadInt32();

            NativeResponsePacket data = message.ReadBytes(len).Deserialize<NativeResponsePacket>();

            Hash = data.Hash;
            Args = data.Args;
            Type = data.Type;
            NetHandle = data.NetHandle;
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
        public long NetHandle { get; set; }

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

            NetHandle = data.NetHandle;
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
        public long NetHandle { get; set; }

        [ProtoMember(2)]
        public long VehHandle { get; set; }

        [ProtoMember(3)]
        public int ModelHash { get; set; }

        [ProtoMember(4)]
        public Dictionary<int, int> Props { get; set; }

        [ProtoMember(5)]
        public int Health { get; set; }

        [ProtoMember(6)]
        public LVector3 Position { get; set; }

        [ProtoMember(7)]
        public int VehModelHash { get; set; }

        [ProtoMember(8)]
        public int VehSeatIndex { get; set; }

        [ProtoMember(9)]
        public LVector3 VehPosition { get; set; }

        [ProtoMember(10)]
        public LQuaternion VehRotation { get; set; }

        [ProtoMember(11)]
        public float VehEngineHealth { get; set; }

        [ProtoMember(12)]
        public float VehRPM { get; set; }

        [ProtoMember(13)]
        public LVector3 VehVelocity { get; set; }

        [ProtoMember(14)]
        public float VehSpeed { get; set; }

        [ProtoMember(15)]
        public float VehSteeringAngle { get; set; }

        [ProtoMember(16)]
        public int[] VehColors { get; set; }

        [ProtoMember(17)]
        public Dictionary<int, int> VehMods { get; set; }

        [ProtoMember(18)]
        public VehicleDoors[] VehDoors { get; set; }

        [ProtoMember(19)]
        public int VehTires { get; set; }

        [ProtoMember(20)]
        public byte VehLandingGear { get; set; }

        [ProtoMember(21)]
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

            NetHandle = data.NetHandle;
            VehHandle = data.VehHandle;
            ModelHash = data.ModelHash;
            Props = data.Props;
            Health = data.Health;
            Position = data.Position;
            VehModelHash = data.VehModelHash;
            VehSeatIndex = data.VehSeatIndex;
            VehPosition = data.VehPosition;
            VehRotation = data.VehRotation;
            VehEngineHealth = data.VehEngineHealth;
            VehRPM = data.VehRPM;
            VehVelocity = data.VehVelocity;
            VehSpeed = data.VehSpeed;
            VehSteeringAngle = data.VehSteeringAngle;
            VehColors = data.VehColors;
            VehMods = data.VehMods;
            VehDoors = data.VehDoors;
            VehTires = data.VehTires;
            VehLandingGear = data.VehLandingGear;
            Flag = data.Flag;
        }
    }
    #endregion

    /// <summary>
    /// ?
    /// </summary>
    public static class CoopSerializer
    {
        /// <summary>
        /// ?
        /// </summary>
        public static byte[] CSerialize(this object obj)
        {
            if (obj == null)
            {
                return null;
            }

            string jsonString = JsonConvert.SerializeObject(obj);
            return System.Text.Encoding.UTF8.GetBytes(jsonString);
        }

        /// <summary>
        /// ?
        /// </summary>
        public static T CDeserialize<T>(this byte[] bytes) where T : class
        {
            if (bytes == null)
            {
                return null;
            }

            var jsonString = System.Text.Encoding.UTF8.GetString(bytes);
            return JsonConvert.DeserializeObject<T>(jsonString);
        }

        internal static T Deserialize<T>(this byte[] data) where T : new()
        {
            try
            {
                using (MemoryStream stream = new MemoryStream(data))
                {
                    return Serializer.Deserialize<T>(stream);
                }
            }
            catch
            {
                throw new Exception(string.Format("The deserialization of the packet {0} failed!", typeof(T).Name));
            }
        }

        internal static byte[] Serialize<T>(this T packet)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                Serializer.Serialize(stream, packet);
                return stream.ToArray();
            }
        }
    }
}
