﻿using System;
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
        public Vehicle MainVehicle { get;internal set; }


        #region LAST STATE


        private byte[] _lastVehicleColors = new byte[] { 0, 0 };
        private Dictionary<int, int> _lastVehicleMods = new Dictionary<int, int>();
        private byte _lastRadioIndex=255;
        #endregion

        #region -- CRITICAL STUFF --
        internal Vector3 RotationVelocity { get; set; }
        internal float SteeringAngle { get; set; }
        internal float ThrottlePower { get; set; }
        internal float BrakePower { get; set; }
        internal float DeluxoWingRatio { get; set; } = -1;
        #endregion

        #region -- VEHICLE STATE --
        internal VehicleDataFlags Flags { get; set; }
        internal bool EngineRunning { get; set; }
        private bool _lastTransformed = false;
        internal bool Transformed { get; set; }
        private bool _lastHornActive = false;
        internal bool HornActive { get; set; }
        internal bool LightsOn { get; set; }
        internal bool BrakeLightsOn { get; set; } = false;
        internal bool HighBeamsOn { get; set; }
        internal byte LandingGear { get; set; }
        internal bool RoofOpened { get; set; }
        internal bool SireneActive { get; set; }
        internal VehicleDamageModel DamageModel { get; set; }
        internal int ModelHash { get; set; }
        internal byte[] Colors { get; set; }
        internal Dictionary<int, int> Mods { get; set; }
        internal bool IsDead { get; set; }
        internal float EngineHealth { get; set; }
        internal VehicleLockStatus LockStatus{get;set;}
        /// <summary>
        /// VehicleSeat,PedID
        /// </summary>
        internal Dictionary<VehicleSeat, SyncedPed> Passengers { get; set; }
        internal byte RadioStation = 255;

        #endregion
        internal override void Update()
        {

            #region -- INITIAL CHECK --

            // Check if all data avalible
            if(!IsReady) { return; }
            // Skip update if no new sync message has arrived.
            if (!NeedUpdate) { return; }
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
                MainVehicle.Velocity = Velocity+5*(Position+Velocity*SyncParameters.PositioinPredictionDefault - MainVehicle.Position);
                MainVehicle.Quaternion=Quaternion.Slerp(MainVehicle.Quaternion, Quaternion, 0.5f);
            }
            else
            {
                MainVehicle.Position=Position;
                MainVehicle.Velocity=Velocity;
                MainVehicle.Quaternion=Quaternion;
            }
            // Vector3 r = GetCalibrationRotation();
            MainVehicle.RotationVelocity = RotationVelocity;
            if (DeluxoWingRatio!=-1)
            {
                MainVehicle.SetDeluxoWingRatio(DeluxoWingRatio);
            }
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
                            if (c?.ID==Main.LocalPlayerID && (RadioStation!=_lastRadioIndex))
                            {
                                Util.SetPlayerRadioIndex(RadioStation);
                            }
                            if (c?.MainPed!=null&&(!currentPassengers.ContainsKey(i))&&(!c.MainPed.IsBeingJacked)&&(!c.MainPed.IsTaskActive(TaskType.CTaskExitVehicleSeat))) {
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
                if (Flags.HasFlag(VehicleDataFlags.IsDeluxoHovering))
                {
                    if (!MainVehicle.IsDeluxoHovering())
                    {
                        MainVehicle.SetDeluxoHoverState(true);
                    }
                }
                else if(ModelHash==1483171323)
                {
                    if (MainVehicle.IsDeluxoHovering())
                    {
                        MainVehicle.SetDeluxoHoverState(false);
                    }
                }

                #endregion
            }
            LastUpdated=Main.Ticked;
        }
        private Vector3 GetCalibrationRotation()
        {
            return (Quaternion-MainVehicle.Quaternion).ToEulerAngles().ToDegree();
            /*
            var r = Rotation-MainVehicle.Rotation;
            if (r.X>180) { r.X=r.X-360; }
            else if(r.X<-180) { r.X=360+r.X; }

            if (r.Y>180) { r.Y=r.Y-360; }
            else if (r.Y<-180) { r.Y=360+r.Y; }

            if (r.Z>180) { r.Z=r.Z-360; }
            else if (r.Z<-180) { r.Z=360+r.Z; }
            return r;
            */
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
            MainVehicle.Quaternion = Quaternion;

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

        #region OUTGOING
        internal float LastNozzleAngle { get; set; }
        #endregion
    }

}