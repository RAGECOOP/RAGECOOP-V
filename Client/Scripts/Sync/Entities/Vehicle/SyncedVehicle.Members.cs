using GTA;
using GTA.Math;
using RageCoop.Core;
using System.Collections.Generic;

namespace RageCoop.Client
{
    public partial class SyncedVehicle
    {
        public Vehicle MainVehicle { get; internal set; }


        #region -- SYNC DATA --
        internal Vector3 RotationVelocity { get; set; }
        internal float SteeringAngle { get; set; }
        internal float ThrottlePower { get; set; }
        internal float BrakePower { get; set; }
        internal float DeluxoWingRatio { get; set; } = -1;


        internal byte LandingGear { get; set; }
        internal VehicleRoofState RoofState { get; set; }
        internal VehicleDamageModel DamageModel { get; set; }
        internal byte[] Colors { get; set; }
        internal Dictionary<int, int> Mods { get; set; }
        internal float EngineHealth { get; set; }
        internal VehicleLockStatus LockStatus { get; set; }
        internal byte RadioStation = 255;
        internal string LicensePlate { get; set; }
        internal int Livery { get; set; } = -1;
        internal VehicleDataFlags Flags { get; set; }

        #endregion

        #region FLAGS

        internal bool EngineRunning { get => Flags.HasVehFlag(VehicleDataFlags.IsEngineRunning); }
        internal bool Transformed { get => Flags.HasVehFlag(VehicleDataFlags.IsTransformed); }
        internal bool HornActive { get => Flags.HasVehFlag(VehicleDataFlags.IsHornActive); }
        internal bool LightsOn { get => Flags.HasVehFlag(VehicleDataFlags.AreLightsOn); }
        internal bool BrakeLightsOn { get => Flags.HasVehFlag(VehicleDataFlags.AreBrakeLightsOn); }
        internal bool HighBeamsOn { get => Flags.HasVehFlag(VehicleDataFlags.AreHighBeamsOn); }
        internal bool SireneActive { get => Flags.HasVehFlag(VehicleDataFlags.IsSirenActive); }
        internal bool IsDead { get => Flags.HasVehFlag(VehicleDataFlags.IsDead); }
        internal bool IsDeluxoHovering { get => Flags.HasVehFlag(VehicleDataFlags.IsDeluxoHovering); }
        #endregion

        #region FIXED-DATA

        internal bool IsFlipped
        {
            get => IsMotorcycle || ((Quaternion * Vector3.RelativeTop).Z - (Quaternion * Vector3.RelativeBottom).Z) < 0.5;
        }
        internal bool IsMotorcycle;
        internal bool IsAircraft;
        internal bool HasRocketBoost;
        internal bool HasParachute;
        internal bool HasRoof;
        internal bool IsSubmarineCar;
        internal bool IsDeluxo;

        #endregion

        #region PRIVATE
        private byte[] _lastVehicleColors = new byte[] { 0, 0 };
        private Dictionary<int, int> _lastVehicleMods = new Dictionary<int, int>();
        private bool _lastHornActive = false;
        private bool _lastTransformed = false;
        internal int _lastLivery = -1;
        private readonly List<Vector3> _predictedTrace = new List<Vector3>();
        private readonly List<Vector3> _orgTrace = new List<Vector3>();
        private Vector3 _predictedPosition;
        #endregion

        #region OUTGOING
        internal float LastNozzleAngle { get; set; }

        internal float LastEngineHealth { get; set; }
        internal Vector3 LastVelocity { get; set; }
        #endregion
    }
}