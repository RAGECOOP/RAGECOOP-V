using System;
using System.IO;
using System.Collections.Generic;

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

    public enum ModVersion
    {
        V0_1_0
    }

    public enum PacketTypes
    {
        HandshakePacket,
        PlayerConnectPacket,
        PlayerDisconnectPacket,
        FullSyncPlayerPacket,
        FullSyncNpcPacket,
        LightSyncPlayerPacket,
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
