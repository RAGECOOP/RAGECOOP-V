using System;
using System.Collections.Generic;
using System.Text;
using Lidgren.Network;
using Newtonsoft.Json;
using GTA.Math;

namespace RageCoop.Core
{
    internal enum PacketType:byte
    {
        Handshake=0,
        PlayerConnect=1,
        PlayerDisconnect=2,
        PlayerInfoUpdate=3,
        PublicKeyRequest=4,
        PublicKeyResponse=5,
        Request=6,
        Response=7,
        
        ChatMessage=10,
        // NativeCall=11,
        // NativeResponse=12,
        // Mod=13,
        // CleanUpWorld=14,
       
        FileTransferChunk=11,
        FileTransferRequest=12,
        FileTransferResponse = 13,
        FileTransferComplete =14,
        AllResourcesSent=15,
        
        CustomEvent = 16,
        CustomEventQueued = 17,
        #region Sync

        #region INTERVAL
        VehicleSync = 20,
        VehicleStateSync = 21,
        PedSync = 22,
        PedStateSync = 23,
        ProjectileSync=24,
        #endregion

        #region EVENT

        PedKilled=30,
        BulletShot=31,
        EnteringVehicle=32,
        LeaveVehicle = 33,
        EnteredVehicle=34,
        OwnerChanged=35,
        VehicleBulletShot = 36,
        NozzleTransform=37,

        #endregion

        #endregion
        Unknown=255
    }
    internal static class PacketExtensions
    {
        internal static bool IsSyncEvent(this PacketType p)
        {
            return (30<=(byte)p)&&((byte)p<=40);
        }
    }

    internal enum ConnectionChannel
    {
        Default = 0,
        Chat = 5,
        Native = 6,
        Mod = 7,
        File = 8,
        Event = 9,
        RequestResponse=10,
        VehicleSync=20,
        PedSync=21,
        ProjectileSync = 22,
        SyncEvents =30,
    }

    [Flags]
    internal enum PedDataFlags:ushort
    {
        None=0,
        IsAiming = 1 << 0,
        IsInStealthMode = 1 << 1,
        IsReloading = 1 << 2,
        IsJumping = 1 << 3,
        IsRagdoll = 1 << 4,
        IsOnFire = 1 << 5,
        IsInParachuteFreeFall = 1 << 6,
        IsParachuteOpen = 1 << 7,
        IsOnLadder = 1 << 8,
        IsVaulting = 1 << 9,
        IsInCover = 1 << 10,
    }

    #region ===== VEHICLE DATA =====
    internal enum VehicleDataFlags:ushort
    {
        None=0,
        IsEngineRunning = 1 << 0,
        AreLightsOn = 1 << 1,
        AreBrakeLightsOn = 1 << 2,
        AreHighBeamsOn = 1 << 3,
        IsSirenActive = 1 << 4,
        IsDead = 1 << 5,
        IsHornActive = 1 << 6,
        IsTransformed = 1 << 7,
        RoofOpened = 1 << 8,
        OnTurretSeat = 1 << 9,
        IsAircraft = 1 << 10,
        IsDeluxoHovering=1 << 11, 
        HasRoof=1 << 12,
    }

    internal enum PlayerConfigFlags : byte
    {
        None = 0,
        ShowBlip= 1 << 0,
        ShowNameTag= 1 << 1
    }

    internal struct VehicleDamageModel
    {
        public byte BrokenDoors { get; set; }
        public byte OpenedDoors { get; set; }
        public byte BrokenWindows { get; set; }
        public short BurstedTires { get; set; }
        public byte LeftHeadLightBroken { get; set; }
        public byte RightHeadLightBroken { get; set; }
    }
    #endregion

    interface IPacket
    {
        void Pack(NetOutgoingMessage message);
        void Unpack(byte[] array);
    }

    internal abstract class Packet : IPacket
    {
        public abstract void Pack(NetOutgoingMessage message);
        public abstract void Unpack(byte[] array);
    }

    internal partial class Packets
    {

        internal class ChatMessage : Packet
        {
            public string Username { get; set; }

            public string Message { get; set; }

            public override void Pack(NetOutgoingMessage message)
            {
                #region PacketToNetOutGoingMessage
                message.Write((byte)PacketType.ChatMessage);

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

            public override void Unpack(byte[] array)
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
    }

    internal static class CoopSerializer
    {
        /// <summary>
        /// ?
        /// </summary>
        public static byte[] Serialize(this object obj)
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
