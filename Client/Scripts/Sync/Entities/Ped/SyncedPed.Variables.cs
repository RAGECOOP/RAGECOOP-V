using System.Collections.Generic;
using GTA;
using GTA.Math;
using RageCoop.Core;

namespace RageCoop.Client
{
    /// <summary>
    ///     ?
    /// </summary>
    public partial class SyncedPed : SyncedEntity
    {
        private readonly string[] _currentAnimation = new string[2] { "", "" };
        private byte[] _lastClothes;
        private bool _lastDriveBy;
        private bool _lastInCover;
        private bool _lastIsJumping;
        private bool _lastRagdoll;
        private ulong _lastRagdollTime;
        private Dictionary<uint, bool> _lastWeaponComponents;
        private Entity _weaponObj;
        internal BlipColor BlipColor = (BlipColor)255;
        internal float BlipScale = 1;
        internal BlipSprite BlipSprite = 0;
        internal PedDataFlags Flags;

        private bool _lastMoving;
        internal Blip PedBlip = null;
        internal VehicleSeat Seat;

        internal int VehicleID
        {
            get => CurrentVehicle?.ID ?? 0;
            set
            {
                if (CurrentVehicle == null || value != CurrentVehicle?.ID)
                    CurrentVehicle = EntityPool.GetVehicleByID(value);
            }
        }

        internal SyncedVehicle CurrentVehicle { get; private set; }
        public bool IsPlayer => OwnerID == ID && ID != 0;
        public Ped MainPed { get; internal set; }
        internal int Health { get; set; }

        internal Vector3 HeadPosition { get; set; }
        internal Vector3 RightFootPosition { get; set; }
        internal Vector3 LeftFootPosition { get; set; }

        internal byte WeaponTint { get; set; }
        internal byte[] Clothes { get; set; }

        internal float Heading { get; set; }

        internal ulong LastSpeakingTime { get; set; } = 0;
        internal bool IsSpeaking { get; set; } = false;
        public byte Speed { get; set; }

        internal bool IsAiming => Flags.HasPedFlag(PedDataFlags.IsAiming);
        internal bool IsReloading => Flags.HasPedFlag(PedDataFlags.IsReloading);
        internal bool IsJumping => Flags.HasPedFlag(PedDataFlags.IsJumping);
        internal bool IsRagdoll => Flags.HasPedFlag(PedDataFlags.IsRagdoll);
        internal bool IsOnFire => Flags.HasPedFlag(PedDataFlags.IsOnFire);
        internal bool IsInParachuteFreeFall => Flags.HasPedFlag(PedDataFlags.IsInParachuteFreeFall);
        internal bool IsParachuteOpen => Flags.HasPedFlag(PedDataFlags.IsParachuteOpen);
        internal bool IsOnLadder => Flags.HasPedFlag(PedDataFlags.IsOnLadder);
        internal bool IsVaulting => Flags.HasPedFlag(PedDataFlags.IsVaulting);
        internal bool IsInCover => Flags.HasPedFlag(PedDataFlags.IsInCover);
        internal bool IsInLowCover => Flags.HasPedFlag(PedDataFlags.IsInLowCover);
        internal bool IsInCoverFacingLeft => Flags.HasPedFlag(PedDataFlags.IsInCoverFacingLeft);
        internal bool IsBlindFiring => Flags.HasPedFlag(PedDataFlags.IsBlindFiring);
        internal bool IsInStealthMode => Flags.HasPedFlag(PedDataFlags.IsInStealthMode);
        internal Prop ParachuteProp { get; set; } = null;
        internal uint CurrentWeaponHash { get; set; }
        internal Dictionary<uint, bool> WeaponComponents { get; set; } = null;
        internal Vector3 AimCoords { get; set; }
    }
}