using System.Collections.Generic;
using GTA;
using GTA.Math;
using RageCoop.Core;

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

        internal bool EngineRunning => Flags.HasVehFlag(VehicleDataFlags.IsEngineRunning);
        internal bool Transformed => Flags.HasVehFlag(VehicleDataFlags.IsTransformed);
        internal bool HornActive => Flags.HasVehFlag(VehicleDataFlags.IsHornActive);
        internal bool LightsOn => Flags.HasVehFlag(VehicleDataFlags.AreLightsOn);
        internal bool BrakeLightsOn => Flags.HasVehFlag(VehicleDataFlags.AreBrakeLightsOn);
        internal bool HighBeamsOn => Flags.HasVehFlag(VehicleDataFlags.AreHighBeamsOn);
        internal bool SireneActive => Flags.HasVehFlag(VehicleDataFlags.IsSirenActive);
        internal bool IsDead => Flags.HasVehFlag(VehicleDataFlags.IsDead);
        internal bool IsDeluxoHovering => Flags.HasVehFlag(VehicleDataFlags.IsDeluxoHovering);

        #endregion

        #region FIXED-DATA

        internal bool IsFlipped => IsMotorcycle ||
                                   (Quaternion * Vector3.RelativeTop).Z - (Quaternion * Vector3.RelativeBottom).Z < 0.5;

        internal bool IsMotorcycle;
        internal bool IsAircraft;
        internal bool HasRocketBoost;
        internal bool HasParachute;
        internal bool HasRoof;
        internal bool IsSubmarineCar;
        internal bool IsDeluxo;
        internal bool IsTrain;
        internal Vector3 TopExtent;
        internal Vector3 FrontExtent;
        internal Vector3 LeftExtent;
        internal Vector3 RightExtent;
        #endregion

        #region PRIVATE

        private byte[] _lastVehicleColors = { 0, 0 };
        private Dictionary<int, int> _lastVehicleMods = new Dictionary<int, int>();
        private bool _lastHornActive;
        private bool _lastTransformed;
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