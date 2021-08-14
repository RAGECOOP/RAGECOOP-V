using System;
using System.IO;
using System.Collections.Generic;

using Lidgren.Network;
using ProtoBuf;

using GTA.Math;

namespace CoopClient
{
    #region CLIENT-ONLY
    public static class VectorExtensions
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
    public struct LVector3
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
    public struct LQuaternion
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

    public enum PacketTypes
    {
        HandshakePacket,
        PlayerConnectPacket,
        PlayerDisconnectPacket,
        FullSyncPlayerPacket,
        FullSyncPlayerVehPacket,
        LightSyncPlayerPacket,
        LightSyncPlayerVehPacket,
        FullSyncNpcPacket,
        FullSyncNpcVehPacket,
        ChatMessagePacket
    }

    [Flags]
    public enum PedDataFlags
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
    public enum VehicleDataFlags
    {
        LastSyncWasFull = 1 << 0,
        IsInVehicle = 1 << 1,
        IsEngineRunning = 1 << 2,
        AreLightsOn = 1 << 3,
        AreHighBeamsOn = 1 << 4,
        IsInBurnout = 1 << 5,
        IsSirenActive = 1 << 6,
        IsDead = 1 << 7
    }

    [ProtoContract]
    public struct VehicleDoors
    {
        #region CLIENT-ONLY
        public VehicleDoors(float angleRatio = 0f, bool broken = false, bool open = false, bool fullyOpen = false)
        {
            AngleRatio = angleRatio;
            Broken = broken;
            Open = open;
            FullyOpen = fullyOpen;
        }
        #endregion

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

    public interface IPacket
    {
        void PacketToNetOutGoingMessage(NetOutgoingMessage message);
        void NetIncomingMessageToPacket(NetIncomingMessage message);
    }

    public abstract class Packet : IPacket
    {
        public abstract void PacketToNetOutGoingMessage(NetOutgoingMessage message);
        public abstract void NetIncomingMessageToPacket(NetIncomingMessage message);
    }

    #region -- PLAYER --
    [ProtoContract]
    public class HandshakePacket : Packet
    {
        [ProtoMember(1)]
        public string ID { get; set; }

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

            byte[] result = CoopSerializer.Serialize(this);

            message.Write(result.Length);
            message.Write(result);
        }

        public override void NetIncomingMessageToPacket(NetIncomingMessage message)
        {
            int len = message.ReadInt32();

            HandshakePacket data = CoopSerializer.Deserialize<HandshakePacket>(message.ReadBytes(len));

            ID = data.ID;
            SocialClubName = data.SocialClubName;
            Username = data.Username;
            ModVersion = data.ModVersion;
            NpcsAllowed = data.NpcsAllowed;
        }
    }

    [ProtoContract]
    public class PlayerConnectPacket : Packet
    {
        [ProtoMember(1)]
        public string Player { get; set; }

        [ProtoMember(2)]
        public string SocialClubName { get; set; }

        [ProtoMember(3)]
        public string Username { get; set; }

        public override void PacketToNetOutGoingMessage(NetOutgoingMessage message)
        {
            message.Write((byte)PacketTypes.PlayerConnectPacket);

            byte[] result = CoopSerializer.Serialize(this);

            message.Write(result.Length);
            message.Write(result);
        }

        public override void NetIncomingMessageToPacket(NetIncomingMessage message)
        {
            int len = message.ReadInt32();

            PlayerConnectPacket data = CoopSerializer.Deserialize<PlayerConnectPacket>(message.ReadBytes(len));

            Player = data.Player;
            SocialClubName = data.SocialClubName;
            Username = data.Username;
        }
    }

    [ProtoContract]
    public class PlayerDisconnectPacket : Packet
    {
        [ProtoMember(1)]
        public string Player { get; set; }

        public override void PacketToNetOutGoingMessage(NetOutgoingMessage message)
        {
            message.Write((byte)PacketTypes.PlayerDisconnectPacket);

            byte[] result = CoopSerializer.Serialize(this);

            message.Write(result.Length);
            message.Write(result);
        }

        public override void NetIncomingMessageToPacket(NetIncomingMessage message)
        {
            int len = message.ReadInt32();

            PlayerDisconnectPacket data = CoopSerializer.Deserialize<PlayerDisconnectPacket>(message.ReadBytes(len));

            Player = data.Player;
        }
    }

    [ProtoContract]
    public class FullSyncPlayerPacket : Packet
    {
        [ProtoMember(1)]
        public string Player { get; set; }

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
            message.Write((byte)PacketTypes.FullSyncPlayerPacket);

            byte[] result = CoopSerializer.Serialize(this);

            message.Write(result.Length);
            message.Write(result);
        }

        public override void NetIncomingMessageToPacket(NetIncomingMessage message)
        {
            int len = message.ReadInt32();

            FullSyncPlayerPacket data = CoopSerializer.Deserialize<FullSyncPlayerPacket>(message.ReadBytes(len));

            Player = data.Player;
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
    public class FullSyncPlayerVehPacket : Packet
    {
        [ProtoMember(1)]
        public string Player { get; set; }

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
        public LVector3 VehVelocity { get; set; }

        [ProtoMember(11)]
        public float VehSpeed { get; set; }

        [ProtoMember(12)]
        public float VehSteeringAngle { get; set; }

        [ProtoMember(13)]
        public int[] VehColors { get; set; }

        [ProtoMember(14)]
        public VehicleDoors[] VehDoors { get; set; }

        [ProtoMember(15)]
        public byte? Flag { get; set; } = 0;

        public override void PacketToNetOutGoingMessage(NetOutgoingMessage message)
        {
            message.Write((byte)PacketTypes.FullSyncPlayerVehPacket);

            byte[] result = CoopSerializer.Serialize(this);

            message.Write(result.Length);
            message.Write(result);
        }

        public override void NetIncomingMessageToPacket(NetIncomingMessage message)
        {
            int len = message.ReadInt32();

            FullSyncPlayerVehPacket data = CoopSerializer.Deserialize<FullSyncPlayerVehPacket>(message.ReadBytes(len));

            Player = data.Player;
            ModelHash = data.ModelHash;
            Props = data.Props;
            Health = data.Health;
            Position = data.Position;
            VehModelHash = data.VehModelHash;
            VehSeatIndex = data.VehSeatIndex;
            VehPosition = data.VehPosition;
            VehRotation = data.VehRotation;
            VehVelocity = data.VehVelocity;
            VehSpeed = data.VehSpeed;
            VehSteeringAngle = data.VehSteeringAngle;
            VehColors = data.VehColors;
            VehDoors = data.VehDoors;
            Flag = data.Flag;
        }
    }

    [ProtoContract]
    public class LightSyncPlayerPacket : Packet
    {
        [ProtoMember(1)]
        public string Player { get; set; }

        [ProtoMember(2)]
        public int Health { get; set; }

        [ProtoMember(3)]
        public LVector3 Position { get; set; }

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
            message.Write((byte)PacketTypes.LightSyncPlayerPacket);

            byte[] result = CoopSerializer.Serialize(this);

            message.Write(result.Length);
            message.Write(result);
        }

        public override void NetIncomingMessageToPacket(NetIncomingMessage message)
        {
            int len = message.ReadInt32();

            LightSyncPlayerPacket data = CoopSerializer.Deserialize<LightSyncPlayerPacket>(message.ReadBytes(len));

            Player = data.Player;
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
    public class LightSyncPlayerVehPacket : Packet
    {
        [ProtoMember(1)]
        public string Player { get; set; }

        [ProtoMember(2)]
        public int Health { get; set; }

        [ProtoMember(3)]
        public LVector3 Position { get; set; }

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

            byte[] result = CoopSerializer.Serialize(this);

            message.Write(result.Length);
            message.Write(result);
        }

        public override void NetIncomingMessageToPacket(NetIncomingMessage message)
        {
            int len = message.ReadInt32();

            LightSyncPlayerVehPacket data = CoopSerializer.Deserialize<LightSyncPlayerVehPacket>(message.ReadBytes(len));

            Player = data.Player;
            Health = data.Health;
            Position = data.Position;
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
    public class ChatMessagePacket : Packet
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

            ChatMessagePacket data = CoopSerializer.Deserialize<ChatMessagePacket>(message.ReadBytes(len));

            Username = data.Username;
            Message = data.Message;
        }
    }
    #endregion

    #region -- NPC --
    [ProtoContract]
    public class FullSyncNpcPacket : Packet
    {
        [ProtoMember(1)]
        public string ID { get; set; }

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

            FullSyncNpcPacket data = CoopSerializer.Deserialize<FullSyncNpcPacket>(message.ReadBytes(len));

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
    public class FullSyncNpcVehPacket : Packet
    {
        [ProtoMember(1)]
        public string ID { get; set; }

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
        public LVector3 VehVelocity { get; set; }

        [ProtoMember(11)]
        public float VehSpeed { get; set; }

        [ProtoMember(12)]
        public float VehSteeringAngle { get; set; }

        [ProtoMember(13)]
        public int[] VehColors { get; set; }

        [ProtoMember(14)]
        public VehicleDoors[] VehDoors { get; set; }

        [ProtoMember(15)]
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

            FullSyncNpcVehPacket data = CoopSerializer.Deserialize<FullSyncNpcVehPacket>(message.ReadBytes(len));

            ID = data.ID;
            ModelHash = data.ModelHash;
            Props = data.Props;
            Health = data.Health;
            Position = data.Position;
            VehModelHash = data.VehModelHash;
            VehSeatIndex = data.VehSeatIndex;
            VehPosition = data.VehPosition;
            VehRotation = data.VehRotation;
            VehVelocity = data.VehVelocity;
            VehSpeed = data.VehSpeed;
            VehSteeringAngle = data.VehSteeringAngle;
            VehColors = data.VehColors;
            VehDoors = data.VehDoors;
            Flag = data.Flag;
        }
    }
    #endregion

    class CoopSerializer
    {
        public static T Deserialize<T>(byte[] data) where T : new()
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

        public static byte[] Serialize<T>(T packet)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                Serializer.Serialize(stream, packet);
                return stream.ToArray();
            }
        }
    }
}
