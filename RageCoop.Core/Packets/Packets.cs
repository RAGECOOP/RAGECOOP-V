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
        PingPong = 8,
        HandshakeSuccess = 9,
        ChatMessage =10,
       
        FileTransferChunk=11,
        FileTransferRequest=12,
        FileTransferResponse = 13,
        FileTransferComplete =14,
        AllResourcesSent=15,
        
        CustomEvent = 16,
        CustomEventQueued = 17,

        ConnectionRequest=18,
        ConnectionEstablished = 19,
        #region Sync

        #region INTERVAL
        VehicleSync = 20,
        PedSync = 22,
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
        PingPong = 11,
        VehicleSync =20,
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
        IsInVehicle = 1 << 11,
        IsFullSync = 1<<12 ,
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
        IsFullSync = 1<<13,
        IsOnFire = 1<<14,
        Repaired = 1<<15,
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

    internal interface IPacket
    {
        PacketType Type { get; }
        byte[] Serialize();

        void Deserialize(byte[] data);
    }

    internal abstract class Packet : IPacket
    {
        public abstract PacketType Type { get; }
        public virtual byte[] Serialize()
        {
            return new byte[0];
        }
        public virtual void Deserialize(byte[] array) { }
        public void Pack(NetOutgoingMessage message)
        {
            var d=Serialize();
            message.Write((byte)Type);
            message.Write(d.Length);
            message.Write(d);
        }
    }
}
