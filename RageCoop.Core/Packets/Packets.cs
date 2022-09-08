using Lidgren.Network;
using System;

namespace RageCoop.Core
{
    internal enum PacketType : byte
    {
        Handshake = 0,
        PlayerConnect = 1,
        PlayerDisconnect = 2,
        PlayerInfoUpdate = 3,
        PublicKeyRequest = 4,
        PublicKeyResponse = 5,
        Request = 6,
        Response = 7,
        PingPong = 8,
        HandshakeSuccess = 9,
        ChatMessage = 10,

        FileTransferChunk = 11,
        FileTransferRequest = 12,
        FileTransferResponse = 13,
        FileTransferComplete = 14,
        AllResourcesSent = 15,

        CustomEvent = 16,
        CustomEventQueued = 17,

        ConnectionRequest = 18,
        P2PConnect = 19,
        HolePunchInit = 20,
        HolePunch = 21,

        Voice = 22,

        #region Sync
        PedSync = 23,
        VehicleSync = 24,
        ProjectileSync = 25,
        #endregion

        #region EVENT

        PedKilled = 30,
        BulletShot = 31,
        VehicleBulletShot = 32,
        OwnerChanged = 35,
        NozzleTransform = 37,

        #endregion

        Unknown = 255
    }
    internal enum ConnectionChannel
    {
        Default = 0,
        Chat = 1,
        Voice = 2,
        Native = 3,
        Mod = 4,
        File = 5,
        Event = 6,
        RequestResponse = 7,
        PingPong = 8,
        VehicleSync = 9,
        PedSync = 10,
        ProjectileSync = 11,
        SyncEvents = 12,
    }

    [Flags]
    internal enum PedDataFlags : ushort
    {
        None = 0,
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
        IsInLowCover = 1 << 11,
        IsInCoverFacingLeft = 1 << 12,
        IsBlindFiring = 1 << 13,
        IsInvincible = 1 << 14,
        IsFullSync = 1 << 15,
    }

    internal enum ProjectileDataFlags : byte
    {
        None = 0,
        Exploded = 1 << 0,
        IsAttached = 1 << 1,
        IsOrgin = 1 << 2,
        IsShotByVehicle = 1 << 3,
    }
    #region ===== VEHICLE DATA =====
    internal enum VehicleDataFlags : ushort
    {
        None = 0,
        IsEngineRunning = 1 << 0,
        AreLightsOn = 1 << 1,
        AreBrakeLightsOn = 1 << 2,
        AreHighBeamsOn = 1 << 3,
        IsSirenActive = 1 << 4,
        IsDead = 1 << 5,
        IsHornActive = 1 << 6,
        IsTransformed = 1 << 7,
        IsParachuteActive = 1 << 8,
        IsRocketBoostActive = 1 << 9,
        IsAircraft = 1 << 10,
        IsDeluxoHovering = 1 << 11,
        HasRoof = 1 << 12,
        IsFullSync = 1 << 13,
        IsOnFire = 1 << 14,
        Repaired = 1 << 15,
    }

    internal enum PlayerConfigFlags : byte
    {
        None = 0,
        ShowBlip = 1 << 0,
        ShowNameTag = 1 << 1
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

        void Deserialize(NetIncomingMessage m);
    }

    internal abstract class Packet : IPacket
    {
        public abstract PacketType Type { get; }
        public void Pack(NetOutgoingMessage m)
        {
            m.Write((byte)Type);
            Serialize(m);
        }
        protected virtual void Serialize(NetOutgoingMessage m) { }
        public virtual void Deserialize(NetIncomingMessage m) { }
    }
}
