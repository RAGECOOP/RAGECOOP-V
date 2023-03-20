﻿using System;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;
using GTA.UI;
using RageCoop.Core;
using static ICSharpCode.SharpZipLib.Zip.ExtendedUnixData;

namespace RageCoop.Client
{
    /// <summary>
    ///     A synchronized vehicle instance
    /// </summary>
    public partial class SyncedVehicle : SyncedEntity
    {
        internal override void Update()
        {
            // Check if all data available
            if (!IsReady || Owner == null) return;

            // Check existence
            if (MainVehicle == null || !MainVehicle.Exists() || MainVehicle.Model != Model)
                if (!CreateVehicle())
                    return;


            DisplayVehicle();
            // Skip update if no new sync message has arrived.
            if (!NeedUpdate) return;

            if (SteeringAngle != MainVehicle.SteeringAngle)
                MainVehicle.CustomSteeringAngle((float)(Math.PI / 180) * SteeringAngle);
            MainVehicle.ThrottlePower = ThrottlePower;
            MainVehicle.BrakePower = BrakePower;

            if (IsDead)
            {
                if (MainVehicle.IsDead) return;

                MainVehicle.Explode();
            }
            else
            {
                if (MainVehicle.IsDead)
                    WorldThread.Delay(() =>
                    {
                        if (MainVehicle.IsDead && !IsDead) MainVehicle.Repair();
                    }, 1000);
            }

            if (MainVehicle.IsOnFire)
            {
                if (!Flags.HasVehFlag(VehicleDataFlags.IsOnFire)) Call(STOP_ENTITY_FIRE, MainVehicle);
            }
            else if (Flags.HasVehFlag(VehicleDataFlags.IsOnFire))
            {
                Call(START_ENTITY_FIRE, MainVehicle);
            }

            if (EngineRunning != MainVehicle.IsEngineRunning) MainVehicle.IsEngineRunning = EngineRunning;

            if (LightsOn != MainVehicle.AreLightsOn) MainVehicle.AreLightsOn = LightsOn;

            if (HighBeamsOn != MainVehicle.AreHighBeamsOn) MainVehicle.AreHighBeamsOn = HighBeamsOn;

            if (IsAircraft)
            {
                if (LandingGear != (byte)MainVehicle.LandingGearState)
                    MainVehicle.LandingGearState = (VehicleLandingGearState)LandingGear;
            }
            else
            {
                if (MainVehicle.HasSiren && SireneActive != MainVehicle.IsSirenActive)
                    MainVehicle.IsSirenActive = SireneActive;

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

                if (HasRoof && MainVehicle.RoofState != RoofState) MainVehicle.RoofState = RoofState;

                if (HasRocketBoost && Flags.HasFlag(VehicleDataFlags.IsRocketBoostActive) !=
                    MainVehicle.IsRocketBoostActive)
                    MainVehicle.IsRocketBoostActive = Flags.HasFlag(VehicleDataFlags.IsRocketBoostActive);
                if (HasParachute && Flags.HasFlag(VehicleDataFlags.IsParachuteActive) &&
                    !MainVehicle.IsParachuteDeployed)
                    MainVehicle.StartParachuting(false);
                if (IsSubmarineCar)
                {
                    if (Transformed)
                    {
                        if (!_lastTransformed)
                        {
                            _lastTransformed = true;
                            Call(TRANSFORM_TO_SUBMARINE, MainVehicle.Handle, false);
                        }
                    }
                    else if (_lastTransformed)
                    {
                        _lastTransformed = false;
                        Call(TRANSFORM_TO_CAR, MainVehicle.Handle, false);
                    }
                }
                else if (IsDeluxo)
                {
                    MainVehicle.SetDeluxoHoverState(IsDeluxoHovering);
                    if (IsDeluxoHovering) MainVehicle.SetDeluxoWingRatio(DeluxoWingRatio);
                }

                Call(SET_VEHICLE_BRAKE_LIGHTS, MainVehicle.Handle, BrakeLightsOn);
            }

            MainVehicle.LockStatus = LockStatus;

            if (LastFullSynced >= LastUpdated)
            {
                if (Flags.HasVehFlag(VehicleDataFlags.Repaired)) MainVehicle.Repair();
                if (Colors != _lastVehicleColors)
                {
                    Call(SET_VEHICLE_COLOURS, MainVehicle, Colors.Item1, Colors.Item2);

                    _lastVehicleColors = Colors;
                }

                MainVehicle.EngineHealth = EngineHealth;
                if (Mods != null && !Mods.SequenceEqual(_lastVehicleMods))
                {
                    Call(SET_VEHICLE_MOD_KIT, MainVehicle, 0);

                    foreach (var mod in Mods) MainVehicle.Mods[(VehicleModType)mod.Item1].Index = mod.Item2;

                    _lastVehicleMods = Mods;
                }

                if (Call<string>(GET_VEHICLE_NUMBER_PLATE_TEXT, MainVehicle) != LicensePlate)
                    Call(SET_VEHICLE_NUMBER_PLATE_TEXT, MainVehicle, LicensePlate);

                if (_lastLivery != Livery)
                {
                    Call(SET_VEHICLE_LIVERY, MainVehicle, Livery);
                    _lastLivery = Livery;
                }

                if (_lastHeadlightColor != HeadlightColor)
                {
                    Call(SET_VEHICLE_XENON_LIGHT_COLOR_INDEX, MainVehicle.Handle, HeadlightColor);
                    _lastHeadlightColor = HeadlightColor;
                }
                MainVehicle.SetDamageModel(DamageModel);

                if (MainVehicle.Handle == V?.Handle && Util.GetPlayerRadioIndex() != RadioStation)
                    Util.SetPlayerRadioIndex(MainVehicle.Handle, RadioStation);

                if (_lastExtras != ExtrasMask)
                {
                    for (int i = 1; i < 15; i++)
                    {
                        var flag = (ushort)(1 << i);
                        var hasExtra = (AvalibleExtras & (ushort)(1 << i)) != 0;
                        if (!hasExtra)
                            continue;

                        var on = (ExtrasMask & flag) != 0;
                        Call(SET_VEHICLE_EXTRA, MainVehicle.Handle, i, !on);
                    }
                    _lastExtras = ExtrasMask;
                }
            }

            LastUpdated = Ticked;
        }

        private void DisplayVehicle()
        {
            _predictedPosition = Predict(Position);
            var current = MainVehicle.ReadPosition();
            var distSquared = current.DistanceToSquared(_predictedPosition);
            var cali = _predictedPosition - current;
            if (!IsTrain) cali += 0.5f * (Velocity - MainVehicle.Velocity);
            if (distSquared > 10 * 10)
            {
                MainVehicle.Position = _predictedPosition;
                MainVehicle.Velocity = Velocity;
                MainVehicle.Quaternion = Quaternion;
                return;
            }

            var stopped = Velocity == Vector3.Zero;

            // Calibrate position
            if (distSquared > 0.03 * 0.03)
            {
                if (IsTrain || distSquared > 20 * 20) MainVehicle.Velocity = Velocity + cali;
                else MainVehicle.ApplyWorldForceCenterOfMass(cali, ForceType.InternalImpulse, true);
            }

            if (NeedUpdate || stopped)
            {
                // Calibrate rotation
                var diff = Quaternion.Diff(MainVehicle.ReadQuaternion());
                MainVehicle.WorldRotationVelocity = diff.ToEulerAngles() * RotCalMult;
            }
        }

        private bool CreateVehicle()
        {
            MainVehicle?.Delete();
            MainVehicle = Util.CreateVehicle(Model, Position);
            if (!Model.IsInCdImage)
                // GTA.UI.Notification.Show($"~r~(Vehicle)Model ({CurrentVehicleModelHash}) cannot be loaded!");
                return false;
            if (MainVehicle == null)
            {
                Model.Request();
                return false;
            }

            lock (EntityPool.VehiclesLock)
            {
                EntityPool.Add(this);
            }

            MainVehicle.Quaternion = Quaternion;
            if (MainVehicle.HasRoof) MainVehicle.RoofState = RoofState;
            foreach (var w in MainVehicle.Wheels) w.Fix();
            if (IsInvincible) MainVehicle.IsInvincible = true;
            SetUpFixedData();
            Model.MarkAsNoLongerNeeded();
            return true;
        }

        #region -- CONSTRUCTORS --

        /// <summary>
        ///     Create a local entity (outgoing sync)
        /// </summary>
        /// <param name="v"></param>
        internal SyncedVehicle(Vehicle v)
        {
            ID = EntityPool.RequestNewID();
            MainVehicle = v;
            MainVehicle.CanPretendOccupants = false;
            OwnerID = LocalPlayerID;
            SetUpFixedData();
        }

        internal void SetUpFixedData()
        {
            if (MainVehicle == null) return;

            IsAircraft = MainVehicle.IsAircraft;
            IsMotorcycle = MainVehicle.IsMotorcycle;
            HasRocketBoost = MainVehicle.HasRocketBoost;
            HasParachute = MainVehicle.HasParachute;
            HasRoof = MainVehicle.HasRoof;
            IsSubmarineCar = MainVehicle.IsSubmarineCar;
            IsDeluxo = MainVehicle.Model == 1483171323;
            IsTrain = MainVehicle.IsTrain;
            AvalibleExtras = 0;
            for (int i = 1; i < 15; i++)
            {
                if (Call<bool>(DOES_EXTRA_EXIST, MainVehicle.Handle, i))
                    AvalibleExtras |= (ushort)(1 << i);
            }
        }

        /// <summary>
        ///     Create an empty VehicleEntity
        /// </summary>
        internal SyncedVehicle()
        {
        }

        internal SyncedVehicle(int id)
        {
            ID = id;
            LastSynced = Ticked;
        }

        #endregion

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
            MainVehicle.Driver?.Task.PlayAnimation(PedalingAnimDict(), PedalingAnimName(fast), 8.0f, -8.0f, -1,
                AnimationFlags.Loop | AnimationFlags.Secondary, 1.0f);
        }

        private void StopPedalingAnim(bool fast)
        {
            MainVehicle.Driver.Task.ClearAnimation(PedalingAnimDict(), PedalingAnimName(fast));
        }

        #endregion
    }
}