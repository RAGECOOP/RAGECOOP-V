using System;
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
        internal VehicleData VD;
        internal VehicleDataFull VDF;
        internal VehicleDataVar VDV;

        #endregion

        #region FLAGS

        internal bool EngineRunning => VD.Flags.HasVehFlag(VehicleDataFlags.IsEngineRunning);
        internal bool Transformed => VD.Flags.HasVehFlag(VehicleDataFlags.IsTransformed);
        internal bool HornActive => VD.Flags.HasVehFlag(VehicleDataFlags.IsHornActive);
        internal bool LightsOn => VD.Flags.HasVehFlag(VehicleDataFlags.AreLightsOn);
        internal bool BrakeLightsOn => VD.Flags.HasVehFlag(VehicleDataFlags.AreBrakeLightsOn);
        internal bool HighBeamsOn => VD.Flags.HasVehFlag(VehicleDataFlags.AreHighBeamsOn);
        internal bool SireneActive => VD.Flags.HasVehFlag(VehicleDataFlags.IsSirenActive);
        internal bool IsDead => VD.Flags.HasVehFlag(VehicleDataFlags.IsDead);
        internal bool IsDeluxoHovering => VD.Flags.HasVehFlag(VehicleDataFlags.IsDeluxoHovering);

        #endregion

        #region FIXED-DATA

        internal bool IsMotorcycle;
        internal bool IsAircraft;
        internal bool HasRocketBoost;
        internal bool HasParachute;
        internal bool HasRoof;
        internal bool IsSubmarineCar;
        internal bool IsDeluxo;
        internal bool IsTrain;
        internal ushort AvalibleExtras;

        [DebugTunable]
        static float RotCalMult = 10f;

        #endregion

        #region PRIVATE
        private VehicleData _lastVD;
        private VehicleDataFull _lastVDF;
        private VehicleDataVar _lastVDV;
        private Vector3 _predictedPosition;
        internal bool _lastTransformed => _lastVD.Flags.HasVehFlag(VehicleDataFlags.IsTransformed);
        internal bool _lastHornActive => _lastVD.Flags.HasVehFlag(VehicleDataFlags.IsHornActive);

        #endregion

        #region OUTGOING

        internal float LastNozzleAngle { get; set; }

        internal float LastEngineHealth { get; set; }
        internal Vector3 LastVelocity { get; set; }

        #endregion
    }
}