using GTA;
using GTA.Math;
using GTA.Native;
using RageCoop.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace RageCoop.Client
{
    /// <summary>
    /// A synchronized vehicle instance
    /// </summary>
    public class SyncedVehicle : SyncedEntity
    {

        #region -- CONSTRUCTORS --

        /// <summary>
        /// Create a local entity (outgoing sync)
        /// </summary>
        /// <param name="v"></param>
        internal SyncedVehicle(Vehicle v)
        {

            ID=EntityPool.RequestNewID();
            MainVehicle=v;
            MainVehicle.CanPretendOccupants=false;
            OwnerID=Main.LocalPlayerID;

        }

        /// <summary>
        /// Create an empty VehicleEntity
        /// </summary>
        internal SyncedVehicle()
        {

        }
        internal SyncedVehicle(int id)
        {
            ID=id;
            LastSynced=Main.Ticked;
        }
        #endregion
        /// <summary>
        /// VehicleSeat,ID
        /// </summary>
        public Vehicle MainVehicle { get; internal set; }
        public Stopwatch LastSyncedStopWatch = new Stopwatch();


        #region LAST STATE
        private byte[] _lastVehicleColors = new byte[] { 0, 0 };
        private Dictionary<int, int> _lastVehicleMods = new Dictionary<int, int>();
        #endregion

        #region -- CRITICAL STUFF --
        internal Vector3 RotationVelocity { get; set; }
        internal float SteeringAngle { get; set; }
        internal float ThrottlePower { get; set; }
        internal float BrakePower { get; set; }
        internal float DeluxoWingRatio { get; set; } = -1;
        internal bool IsFlipped
        {
            get => _isMotorcycle || ((Quaternion * Vector3.RelativeTop).Z - (Quaternion * Vector3.RelativeBottom).Z) < 0.5;
        }
        private bool _isMotorcycle;
        #endregion
        #region FLAGS
        internal bool EngineRunning { get => Flags.HasVehFlag(VehicleDataFlags.IsEngineRunning); }
        private bool _lastTransformed = false;
        internal bool Transformed { get => Flags.HasVehFlag(VehicleDataFlags.IsTransformed); }
        private bool _lastHornActive = false;
        internal bool HornActive { get => Flags.HasVehFlag(VehicleDataFlags.IsHornActive); }
        internal bool LightsOn { get => Flags.HasVehFlag(VehicleDataFlags.AreLightsOn); }
        internal bool BrakeLightsOn { get => Flags.HasVehFlag(VehicleDataFlags.AreBrakeLightsOn); }
        internal bool HighBeamsOn { get => Flags.HasVehFlag(VehicleDataFlags.AreHighBeamsOn); }
        internal bool SireneActive { get => Flags.HasVehFlag(VehicleDataFlags.IsSirenActive); }
        internal bool IsDead { get => Flags.HasVehFlag(VehicleDataFlags.IsDead); }
        internal bool IsDeluxoHovering { get => Flags.HasVehFlag(VehicleDataFlags.IsDeluxoHovering); }
        #endregion

        #region -- VEHICLE STATE --
        internal VehicleDataFlags Flags { get; set; }

        internal byte LandingGear { get; set; }
        internal VehicleRoofState RoofState { get; set; }
        internal VehicleDamageModel DamageModel { get; set; }
        internal byte[] Colors { get; set; }
        internal Dictionary<int, int> Mods { get; set; }
        internal float EngineHealth { get; set; }
        internal VehicleLockStatus LockStatus { get; set; }
        internal byte RadioStation = 255;
        internal string LicensePlate { get; set; }
        internal int _lastLivery = -1;
        internal int Livery { get; set; } = -1;

        #endregion
        internal override void Update()
        {

            #region -- INITIAL CHECK --

            // Check if all data avalible
            if (!IsReady || Owner==null ) { return; }
            #endregion
            #region -- CHECK EXISTENCE --
            if ((MainVehicle == null) || (!MainVehicle.Exists()) || (MainVehicle.Model != Model))
            {
                if (!CreateVehicle())
                {
                    return;
                }
            }
            // Skip update if no new sync message has arrived.
            if (!NeedUpdate) {
                return; 
            }
            #endregion
            #region -- SYNC CRITICAL --
            
            if (SteeringAngle != MainVehicle.SteeringAngle)
            {
                MainVehicle.CustomSteeringAngle((float)(Math.PI / 180) * SteeringAngle);
            }
            MainVehicle.ThrottlePower=ThrottlePower;
            MainVehicle.BrakePower=BrakePower;
            var v = Main.P.CurrentVehicle;
            DisplayVehicle(v != null && MainVehicle.IsTouching(v));
            #region FLAGS
            if (IsDead)
            {
                if (MainVehicle.IsDead)
                {
                    return;
                }

                MainVehicle.Explode();
            }
            else
            {
                if (MainVehicle.IsDead)
                {
                    Main.Delay(() =>
                    {
                        if (MainVehicle.IsDead && !IsDead)
                        {
                            MainVehicle.Repair();
                        }
                    },1000);
                }
            }
            if (MainVehicle.IsOnFire)
            {
                if (!Flags.HasVehFlag(VehicleDataFlags.IsOnFire))
                {
                    Function.Call(Hash.STOP_ENTITY_FIRE, MainVehicle);
                }
            }
            else if (Flags.HasVehFlag(VehicleDataFlags.IsOnFire))
            {
                Function.Call(Hash.START_ENTITY_FIRE, MainVehicle);
            }

            if (EngineRunning != MainVehicle.IsEngineRunning)
            {
                MainVehicle.IsEngineRunning = EngineRunning;
            }

            if (LightsOn != MainVehicle.AreLightsOn)
            {
                MainVehicle.AreLightsOn = LightsOn;
            }

            if (HighBeamsOn != MainVehicle.AreHighBeamsOn)
            {
                MainVehicle.AreHighBeamsOn = HighBeamsOn;
            }

            if (MainVehicle.IsSubmarineCar)
            {
                if (Transformed)
                {
                    if (!_lastTransformed)
                    {
                        _lastTransformed = true;
                        Function.Call(Hash._TRANSFORM_VEHICLE_TO_SUBMARINE, MainVehicle.Handle, false);
                    }
                }
                else if (_lastTransformed)
                {
                    _lastTransformed = false;
                    Function.Call(Hash._TRANSFORM_SUBMARINE_TO_VEHICLE, MainVehicle.Handle, false);
                }
            }

            if (MainVehicle.IsAircraft)
            {
                if (LandingGear != (byte)MainVehicle.LandingGearState)
                {
                    MainVehicle.LandingGearState = (VehicleLandingGearState)LandingGear;
                }
            }
            else
            {
                if (MainVehicle.HasSiren && SireneActive != MainVehicle.IsSirenActive)
                {
                    MainVehicle.IsSirenActive = SireneActive;
                }

                if (HornActive)
                {
                    if (!_lastHornActive)
                    {
                        _lastHornActive = true;
                        MainVehicle.SoundHorn(99999);
                    }
                }
                else if (_lastHornActive)
                {
                    _lastHornActive = false;
                    MainVehicle.SoundHorn(1);
                }

                if (MainVehicle.HasRoof && MainVehicle.RoofState!=RoofState)
                {
                    MainVehicle.RoofState=RoofState;
                }

                Function.Call(Hash.SET_VEHICLE_BRAKE_LIGHTS, MainVehicle.Handle, BrakeLightsOn);
                MainVehicle.SetDamageModel(DamageModel);

            }
            MainVehicle.LockStatus=LockStatus;
            if (IsDeluxoHovering)
            {
                if (!MainVehicle.IsDeluxoHovering())
                {
                    MainVehicle.SetDeluxoHoverState(true);
                }
                MainVehicle.SetDeluxoWingRatio(DeluxoWingRatio);
            }
            else if (Model==1483171323)
            {
                if (MainVehicle.IsDeluxoHovering())
                {
                    MainVehicle.SetDeluxoHoverState(false);
                }
            }
            #endregion

            #endregion
            if (LastFullSynced>=LastUpdated)
            {
                #region -- SYNC STATE --
                if (Flags.HasVehFlag(VehicleDataFlags.Repaired))
                {
                    MainVehicle.Repair();
                }
                if (Colors != null && Colors != _lastVehicleColors)
                {
                    Function.Call(Hash.SET_VEHICLE_COLOURS, MainVehicle, Colors[0], Colors[1]);

                    _lastVehicleColors = Colors;
                }
                MainVehicle.EngineHealth=EngineHealth;
                if (Mods != null && !Mods.Compare(_lastVehicleMods))
                {
                    Function.Call(Hash.SET_VEHICLE_MOD_KIT, MainVehicle, 0);

                    foreach (KeyValuePair<int, int> mod in Mods)
                    {
                        MainVehicle.Mods[(VehicleModType)mod.Key].Index = mod.Value;
                    }

                    _lastVehicleMods = Mods;
                }

                if (Function.Call<string>(Hash.GET_VEHICLE_NUMBER_PLATE_TEXT, MainVehicle)!=LicensePlate)
                {
                    Function.Call(Hash.SET_VEHICLE_NUMBER_PLATE_TEXT, MainVehicle, LicensePlate);
                }

                if (_lastLivery!=Livery)
                {
                    Function.Call(Hash.SET_VEHICLE_LIVERY, MainVehicle, Livery);
                    _lastLivery=Livery;
                }
                #endregion
            }
            LastUpdated=Main.Ticked;
        }
        float _elapsed;
        Vector3 _predictedPos;
        void DisplayVehicle(bool touching)
        {
            _elapsed = Owner.PacketTravelTime+0.001f*LastSyncedStopWatch.ElapsedMilliseconds;
            _predictedPos = Position+_elapsed*Velocity;
            var current = MainVehicle.ReadPosition();
            var dist = current.DistanceTo(Position);
            var cali = ((Velocity.Length()<0.1 && !touching)?dist*4:dist)*(_predictedPos - current);
            
            if (dist>=8)
            {
                MainVehicle.Position = _predictedPos;
                MainVehicle.Velocity = Velocity;
                MainVehicle.Quaternion = Quaternion;
                return;
            }

            MainVehicle.Velocity = Velocity;
            MainVehicle.ApplyForce(cali);
            if (IsFlipped)
            {
                MainVehicle.Quaternion = Quaternion.Slerp(MainVehicle.ReadQuaternion(), Quaternion, 0.5f);
                MainVehicle.RotationVelocity = RotationVelocity;
                return;
            }

            Vector3 calirot = GetCalibrationRotation();
            if (calirot.Length() < 50)
            {
                MainVehicle.RotationVelocity = RotationVelocity + calirot * 0.2f;
            }
            else
            {
                MainVehicle.Quaternion = Quaternion;
                MainVehicle.RotationVelocity = RotationVelocity;
            }
        }
        private Vector3 GetCalibrationRotation()
        {
            var rot = Quaternion.LookRotation(Quaternion*Vector3.RelativeFront, Quaternion*Vector3.RelativeTop).ToEulerAngles();
            var curRot = Quaternion.LookRotation(MainVehicle.ReadQuaternion()*Vector3.RelativeFront, MainVehicle.ReadQuaternion()*Vector3.RelativeTop).ToEulerAngles();

            var r = (rot-curRot).ToDegree();
            if (r.X>180) { r.X=r.X-360; }
            else if (r.X<-180) { r.X=360+r.X; }

            if (r.Y>180) { r.Y=r.Y-360; }
            else if (r.Y<-180) { r.Y=360+r.Y; }

            if (r.Z>180) { r.Z=r.Z-360; }
            else if (r.Z<-180) { r.Z=360+r.Z; }
            return r;
        }
        private bool CreateVehicle()
        {
            MainVehicle?.Delete();
            MainVehicle = Util.CreateVehicle(Model, Position);
            if (!Model.IsInCdImage)
            {
                // GTA.UI.Notification.Show($"~r~(Vehicle)Model ({CurrentVehicleModelHash}) cannot be loaded!");
                return false;
            }
            else if (MainVehicle==null)
            {
                Model.Request();
                return false;
            }
            lock (EntityPool.VehiclesLock)
            {
                EntityPool.Add(this);
            }
            MainVehicle.Quaternion = Quaternion;
            if (MainVehicle.HasRoof)
            {
                MainVehicle.RoofState=RoofState;
            }
            foreach(var w in MainVehicle.Wheels)
            {
                w.Fix();
            }
            if (IsInvincible) { MainVehicle.IsInvincible=true; }
            _isMotorcycle=Model.IsMotorcycle;
            Model.MarkAsNoLongerNeeded();
            return true;
        }
        #region -- PEDALING --
        /*
         * Thanks to @oldnapalm.
         */

        private string PedalingAnimDict()
        {
            switch ((VehicleHash)Model)
            {
                case VehicleHash.Bmx:
                    return "veh@bicycle@bmx@front@base";
                case VehicleHash.Cruiser:
                    return "veh@bicycle@cruiserfront@base";
                case VehicleHash.Scorcher:
                    return "veh@bicycle@mountainfront@base";
                default:
                    return "veh@bicycle@roadfront@base";
            }
        }

        private string PedalingAnimName(bool fast)
        {
            return fast ? "fast_pedal_char" : "cruise_pedal_char";
        }

        private void StartPedalingAnim(bool fast)
        {
            MainVehicle.Driver?.Task.PlayAnimation(PedalingAnimDict(), PedalingAnimName(fast), 8.0f, -8.0f, -1, AnimationFlags.Loop | AnimationFlags.AllowRotation, 1.0f);

        }

        private void StopPedalingAnim(bool fast)
        {
            MainVehicle.Driver.Task.ClearAnimation(PedalingAnimDict(), PedalingAnimName(fast));
        }
        #endregion

        #region OUTGOING
        internal float LastNozzleAngle { get; set; }

        internal float LastEngineHealth { get; set; }
        internal Vector3 LastVelocity { get; set; }
        #endregion
    }
}