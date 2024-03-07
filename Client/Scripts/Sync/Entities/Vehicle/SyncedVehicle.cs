using System;
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

            if (VD.SteeringAngle != MainVehicle.SteeringAngle)
                MainVehicle.CustomSteeringAngle((float)(Math.PI / 180) * VD.SteeringAngle);
            MainVehicle.ThrottlePower = VD.ThrottlePower;
            MainVehicle.BrakePower = VD.BrakePower;

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
                if (!VD.Flags.HasVehFlag(VehicleDataFlags.IsOnFire)) Call(STOP_ENTITY_FIRE, MainVehicle);
            }
            else if (VD.Flags.HasVehFlag(VehicleDataFlags.IsOnFire))
            {
                Call(START_ENTITY_FIRE, MainVehicle);
            }

            if (EngineRunning != MainVehicle.IsEngineRunning) MainVehicle.IsEngineRunning = EngineRunning;

            if (LightsOn != MainVehicle.AreLightsOn) MainVehicle.AreLightsOn = LightsOn;

            if (HighBeamsOn != MainVehicle.AreHighBeamsOn) MainVehicle.AreHighBeamsOn = HighBeamsOn;

            if (!IsAircraft)
            {
                if (MainVehicle.HasSiren && SireneActive != MainVehicle.IsSirenActive)
                    MainVehicle.IsSirenActive = SireneActive;

                if (HornActive)
                {
                    if (!_lastHornActive)
                    {
                        MainVehicle.SoundHorn(99999);
                    }
                }
                else if (_lastVD.Flags.HasVehFlag(VehicleDataFlags.IsHornActive))
                {
                    MainVehicle.SoundHorn(1);
                }

                if (HasRocketBoost && VD.Flags.HasFlag(VehicleDataFlags.IsRocketBoostActive) !=
                    MainVehicle.IsRocketBoostActive)
                    MainVehicle.IsRocketBoostActive = VD.Flags.HasVehFlag(VehicleDataFlags.IsRocketBoostActive);
                if (HasParachute && VD.Flags.HasFlag(VehicleDataFlags.IsParachuteActive) &&
                    !MainVehicle.IsParachuteDeployed)
                    MainVehicle.StartParachuting(false);
                if (IsSubmarineCar)
                {
                    if (Transformed)
                    {
                        if (!_lastTransformed)
                        {
                            Call(TRANSFORM_TO_SUBMARINE, MainVehicle.Handle, false);
                        }
                    }
                    else if (_lastTransformed)
                    {
                        Call(TRANSFORM_TO_CAR, MainVehicle.Handle, false);
                    }
                }
                else if (IsDeluxo)
                {
                    MainVehicle.SetDeluxoHoverState(IsDeluxoHovering);
                    if (IsDeluxoHovering) MainVehicle.SetDeluxoWingRatio(VD.DeluxoWingRatio);
                }
                Call(SET_VEHICLE_BRAKE_LIGHTS, MainVehicle.Handle, BrakeLightsOn);
                MainVehicle.LockStatus = VD.LockStatus;
            }
            _lastVD = VD;

            if (LastFullSynced >= LastUpdated)
            {
                if (IsAircraft)
                {
                    if (VDF.LandingGear != (byte)MainVehicle.LandingGearState)
                        MainVehicle.LandingGearState = (VehicleLandingGearState)VDF.LandingGear;
                }
                if (HasRoof && MainVehicle.RoofState != (VehicleRoofState)VDF.RoofState)
                    MainVehicle.RoofState = (VehicleRoofState)VDF.RoofState;

                if (VD.Flags.HasVehFlag(VehicleDataFlags.Repaired)) MainVehicle.Repair();
                if (VDF.Colors != _lastVDF.Colors)
                {
                    Call(SET_VEHICLE_COLOURS, MainVehicle, VDF.Colors.Item1, VDF.Colors.Item2);
                }

                MainVehicle.EngineHealth = VDF.EngineHealth;

                if (VDF.ToggleModsMask != _lastVDF.ToggleModsMask)
                {
                    for (int i = 0; i < 7; i++)
                    {
                        Call(TOGGLE_VEHICLE_MOD, MainVehicle.Handle, i + 17, (VDF.ToggleModsMask & (1 << i)) != 0);
                    }
                }
                if (VDF.Livery != _lastVDF.Livery)
                {
                    Call(SET_VEHICLE_LIVERY, MainVehicle, VDF.Livery);
                }

                if (VDF.HeadlightColor != _lastVDF.HeadlightColor)
                {
                    Call(SET_VEHICLE_XENON_LIGHT_COLOR_INDEX, MainVehicle.Handle, VDF.HeadlightColor);
                }

                if (!CoreUtils.StructCmp(VDF.DamageModel, _lastVDF.DamageModel))
                {
                    MainVehicle.SetDamageModel(VDF.DamageModel);
                }

                if (MainVehicle.Handle == V?.Handle && Util.GetPlayerRadioIndex() != VDF.RadioStation)
                    Util.SetPlayerRadioIndex(MainVehicle.Handle, VDF.RadioStation);

                if (VDF.ExtrasMask != _lastVDF.ExtrasMask)
                {
                    for (int i = 1; i < 15; i++)
                    {
                        var flag = (ushort)(1 << i);
                        var hasExtra = (AvalibleExtras & (ushort)(1 << i)) != 0;
                        if (!hasExtra)
                            continue;

                        var on = (VDF.ExtrasMask & flag) != 0;
                        Call(SET_VEHICLE_EXTRA, MainVehicle.Handle, i, !on);
                    }
                }
                if (VDV.Mods != null && (_lastVDV.Mods == null || !VDV.Mods.SequenceEqual(_lastVDV.Mods)))
                {
                    Call(SET_VEHICLE_MOD_KIT, MainVehicle, 0);

                    foreach (var mod in VDV.Mods) MainVehicle.Mods[(VehicleModType)mod.Item1].Index = mod.Item2;
                }
                if (VDV.LicensePlate != _lastVDV.LicensePlate)
                    Call(SET_VEHICLE_NUMBER_PLATE_TEXT, MainVehicle, VDV.LicensePlate);

                _lastVDF = VDF;
                _lastVDV = VDV;
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
            if (MainVehicle.HasRoof) MainVehicle.RoofState = (VehicleRoofState)VDF.RoofState;
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