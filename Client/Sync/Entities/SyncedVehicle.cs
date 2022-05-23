using System;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA;
using GTA.Native;
using GTA.Math;
using RageCoop.Core;

namespace RageCoop.Client
{
    public class SyncedVehicle : SyncedEntity
    {

        #region -- CONSTRUCTORS --

        /// <summary>
        /// Create a local entity (outgoing sync)
        /// </summary>
        /// <param name="p"></param>
        public SyncedVehicle(Vehicle v)
        {
            while ((ID==0) || EntityPool.Exists(ID))
            {
                byte[] rngBytes = new byte[4];

                RandomNumberGenerator.Create().GetBytes(rngBytes);

                // Convert the bytes into an integer
                ID = BitConverter.ToInt32(rngBytes, 0);
            }
            MainVehicle=v;
            MainVehicle.CanPretendOccupants=false;
            OwnerID=Main.MyPlayerID;

        }

        /// <summary>
        /// Create an empty VehicleEntity
        /// </summary>
        public SyncedVehicle()
        {

        }
        public SyncedVehicle(int id)
        {
            ID=id;
            LastSynced=Main.Ticked;
        }
        #endregion
        /// <summary>
        /// VehicleSeat,ID
        /// </summary>
        public Vehicle MainVehicle { get; set; }


        #region LAST STATE STORE


        private ulong _vehicleStopTime { get; set; }
        private byte[] _lastVehicleColors = new byte[] { 0, 0 };
        private Dictionary<int, int> _lastVehicleMods = new Dictionary<int, int>();

        #endregion

        #region -- CRITICAL STUFF --
        public Vector3 Position { get; set; }
        public Vector3 Velocity { get; set; }
        public Vector3 RotationVelocity { get; set; }
        public Vector3 Rotation { get; set; }
        public float SteeringAngle { get; set; }
        public float ThrottlePower { get; set; }
        public float BrakePower { get; set; }
        #endregion

        #region -- VEHICLE STATE --
        public bool EngineRunning { get; set; }
        private bool _lastTransformed = false;
        public bool Transformed { get; set; }
        private bool _lastHornActive = false;
        public bool HornActive { get; set; }
        public bool LightsOn { get; set; }
        public bool BrakeLightsOn { get; set; } = false;
        public bool HighBeamsOn { get; set; }
        public byte LandingGear { get; set; }
        public bool RoofOpened { get; set; }
        public bool SireneActive { get; set; }
        public VehicleDamageModel DamageModel { get; set; }
        public int ModelHash { get; set; }
        public byte[] Colors { get; set; }
        public Dictionary<int, int> Mods { get; set; }
        public bool IsDead { get; set; }
        public float EngineHealth { get; set; }
        public VehicleLockStatus LockStatus{get;set;}
        /// <summary>
        /// VehicleSeat,PedID
        /// </summary>
        public Dictionary<VehicleSeat, SyncedPed> Passengers { get; set; }

        private long _lastPositionCalibrated { get; set; }

        #endregion
        public void Update()
        {

            #region -- INITIAL CHECK --

            if (IsMine) { return; }
            // Check if all data avalible
            if (LastStateSynced == 0) { return; }
            if (LastSynced==0) { return; }

            // Skip update if no new sync message has arrived.
            if (LastUpdated>=LastSynced) { return; }

            #endregion
            #region -- CHECK EXISTENCE --
            if ((MainVehicle == null) || (!MainVehicle.Exists()) || (MainVehicle.Model.Hash != ModelHash))
            {
                CreateVehicle();
                return;
            }
            #endregion
            
            #region -- SYNC CRITICAL --






            if (SteeringAngle != MainVehicle.SteeringAngle)
            {
                MainVehicle.CustomSteeringAngle((float)(Math.PI / 180) * SteeringAngle);
            }
            if (MainVehicle.ThrottlePower!=ThrottlePower)
            {
                MainVehicle.ThrottlePower=ThrottlePower;
            }
            if (MainVehicle.BrakePower!=BrakePower)
            {
                MainVehicle.BrakePower=BrakePower;
            }
            if (MainVehicle.Position.DistanceTo(Position)<5)
            {
                MainVehicle.Velocity = Velocity+5*(Position+Velocity*SyncParameters.PositioinPrediction - MainVehicle.Position);
                _lastPositionCalibrated=Main.Counter.ElapsedMilliseconds;
            }
            else
            {
                MainVehicle.Position=Position;
                MainVehicle.Velocity=Velocity;
            }
            #region OBSOLETE
            // Good enough for now, but we need to create a better sync
            /*
            float dist = Position.DistanceTo(MainVehicle.Position);
            Vector3 f = 5*dist * (Position+Velocity*0.06f - (MainVehicle.Position+MainVehicle.Velocity*delay));
            
            if (dist < 5f)
            {
                // Precised calibration
                if (Velocity.Length()<0.05) { f*=10f; }
                else if (dist<1)
                {
                    // Avoid vibration
                    f+=(Velocity-MainVehicle.Velocity);
                }
                MainVehicle.ApplyForce(f);
            }
            else
            {
                MainVehicle.PositionNoOffset = Position;
            }
            */
            #endregion
            Vector3 r = GetCalibrationRotation();
            if (r.Length() < 20f)
            {
                MainVehicle.RotationVelocity = r * 0.15f + RotationVelocity;
            }
            else
            {
                MainVehicle.Rotation = Rotation;
                MainVehicle.RotationVelocity = RotationVelocity;
            }
            _vehicleStopTime = Util.GetTickCount64();
            #endregion

            if (LastStateSynced>LastUpdated)
            {
                #region -- SYNC STATE --
                #region -- PASSENGER SYNC --

                // check passengers (and driver).

                var currentPassengers = MainVehicle.GetPassengers();

                lock (Passengers)
                {
                    for (int i = -1; i<MainVehicle.PassengerCapacity; i++)
                    {
                        VehicleSeat seat = (VehicleSeat)i;
                        if (Passengers.ContainsKey(seat))
                        {

                            SyncedPed c = Passengers[seat];
                            if ((c!=null)&&c.MainPed!=null&&(!currentPassengers.ContainsKey(i))) {
                                Passengers[seat].MainPed.SetIntoVehicle(MainVehicle, seat);
                            }
                        }
                        else if (!MainVehicle.IsSeatFree(seat))
                        {
                            if (seat==VehicleSeat.Driver &&MainVehicle.Driver.IsSittingInVehicle())
                            {
                                MainVehicle.Driver.Task.WarpOutOfVehicle(MainVehicle);
                            }
                            else
                            {
                                var p = MainVehicle.Passengers.Where(x => x.SeatIndex==seat).FirstOrDefault();
                                if ((p!=null)&&p.IsSittingInVehicle())
                                {
                                    p.Task.WarpOutOfVehicle(MainVehicle);
                                }
                            }
                        }
                    }
                }
                #endregion
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


                if (IsDead)
                {
                    if (MainVehicle.IsDead)
                    {
                        return;
                    }
                    else
                    {
                        MainVehicle.Explode();
                    }
                }
                else
                {
                    if (MainVehicle.IsDead)
                    {
                        MainVehicle.Repair();
                    }
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

                    if (MainVehicle.HasRoof)
                    {
                        bool roofOpened = MainVehicle.RoofState == VehicleRoofState.Opened || MainVehicle.RoofState == VehicleRoofState.Opening;
                        if (roofOpened != RoofOpened)
                        {
                            MainVehicle.RoofState = RoofOpened ? VehicleRoofState.Opening : VehicleRoofState.Closing;
                        }
                    }

                    Function.Call(Hash.SET_VEHICLE_BRAKE_LIGHTS, MainVehicle.Handle, BrakeLightsOn);
                    MainVehicle.SetDamageModel(DamageModel);

                }
                MainVehicle.LockStatus=LockStatus;
                

                #endregion
            }
            LastUpdated=Main.Ticked;
        }
        private Vector3 GetCalibrationRotation()
        {
            var r = Rotation-MainVehicle.Rotation;
            if (r.X>180) { r.X=r.X-360; }
            else if(r.X<-180) { r.X=360+r.X; }

            if (r.Y>180) { r.Y=r.Y-360; }
            else if (r.Y<-180) { r.Y=360+r.Y; }

            if (r.Z>180) { r.Z=r.Z-360; }
            else if (r.Z<-180) { r.Z=360+r.Z; }
            return r;
        }
        private void CreateVehicle()
        {
            MainVehicle?.Delete();
            Model vehicleModel = ModelHash.ModelRequest();
            if (vehicleModel == null)
            {
                //GTA.UI.Notification.Show($"~r~(Vehicle)Model ({CurrentVehicleModelHash}) cannot be loaded!");
                return;
            }
            MainVehicle = World.CreateVehicle(vehicleModel, Position);
            lock (EntityPool.VehiclesLock)
            {
                EntityPool.Add( this);
            }
            MainVehicle.Rotation = Rotation;

            if (MainVehicle.HasRoof)
            {
                bool roofOpened = MainVehicle.RoofState == VehicleRoofState.Opened || MainVehicle.RoofState == VehicleRoofState.Opening;
                if (roofOpened != RoofOpened)
                {
                    MainVehicle.RoofState = RoofOpened ? VehicleRoofState.Opened : VehicleRoofState.Closed;
                }
            }
            vehicleModel.MarkAsNoLongerNeeded();
        }
        #region -- PEDALING --
        /*
         * Thanks to @oldnapalm.
         */

        private string PedalingAnimDict()
        {
            switch ((VehicleHash)ModelHash)
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


    }

}
