using GTA;
using GTA.Math;
using RageCoop.Core;
using System.Collections.Generic;

namespace RageCoop.Client
{
    /// <summary>
    /// ?
    /// </summary>
    public partial class SyncedPed : SyncedEntity
    {
        internal Blip PedBlip = null;
        internal BlipColor BlipColor = (BlipColor)255;
        internal BlipSprite BlipSprite = 0;
        internal float BlipScale = 1;
        internal int VehicleID
        {
            get => CurrentVehicle?.ID ?? 0;
            set
            {
                if (CurrentVehicle == null || value != CurrentVehicle?.ID)
                {
                    CurrentVehicle = EntityPool.GetVehicleByID(value);
                }
            }
        }
        internal SyncedVehicle CurrentVehicle { get; private set; }
        internal VehicleSeat Seat;
        public bool IsPlayer { get => OwnerID == ID && ID != 0; }
        public Ped MainPed { get; internal set; }
        internal int Health { get; set; }

        internal Vector3 HeadPosition { get; set; }
        internal Vector3 RightFootPosition { get; set; }
        internal Vector3 LeftFootPosition { get; set; }

        internal byte WeaponTint { get; set; }
        private bool _lastRagdoll = false;
        private ulong _lastRagdollTime = 0;
        private bool _lastInCover = false;
        private byte[] _lastClothes = null;
        internal byte[] Clothes { get; set; }

        internal float Heading { get; set; }

        internal ulong LastSpeakingTime { get; set; } = 0;
        internal bool IsSpeaking { get; set; } = false;
        public byte Speed { get; set; }
        private bool _lastIsJumping = false;
        internal PedDataFlags Flags;

        internal bool IsAiming => Flags.HasPedFlag(PedDataFlags.IsAiming);
        internal bool _lastDriveBy;
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
        private Dictionary<uint, bool> _lastWeaponComponents = null;
        internal Dictionary<uint, bool> WeaponComponents { get; set; } = null;
        private Entity _weaponObj;
        internal Vector3 AimCoords { get; set; }


        private readonly string[] _currentAnimation = new string[2] { "", "" };

        private bool LastMoving;

    }
}
