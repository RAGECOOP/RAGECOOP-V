using System;
using System.Collections.Generic;

using GTA;
using GTA.Native;
using GTA.Math;

namespace CoopClient.Entities.Player
{
    public partial class EntitiesPlayer
    {
        #region -- VARIABLES --
        internal bool IsInVehicle { get; set; }
        /// <summary>
        /// The latest vehicle model hash (may not have been applied yet)
        /// </summary>
        public int VehicleModelHash { get; internal set; }
        private byte[] _lastVehicleColors = new byte[] { 0, 0 };
        internal byte[] VehicleColors { get; set; }
        private Dictionary<int, int> _lastVehicleMods = new Dictionary<int, int>();
        internal Dictionary<int, int> VehicleMods { get; set; }
        internal bool VehicleDead { get; set; }
        internal float VehicleEngineHealth { get; set; }
        internal short VehicleSeatIndex { get; set; }
        /// <summary>
        /// ?
        /// </summary>
        public Vehicle MainVehicle { get; internal set; }
        /// <summary>
        /// The latest vehicle rotation (may not have been applied yet)
        /// </summary>
        public Quaternion VehicleRotation
        {
            get => _vehicleRotation;
            set
            {
                _lastVehicleRotation = _vehicleRotation;
                _vehicleRotation = value;
            }
        }
        private Quaternion _vehicleRotation;
        private Quaternion? _lastVehicleRotation;
        internal float VehicleSpeed { get; set; }
        private float _lastVehicleSpeed;
        internal float VehicleSteeringAngle { get; set; }
        private float _lastVehicleSteeringAngle;
        private int _lastVehicleAim = 0;
        internal bool VehIsEngineRunning { get; set; }
        internal float VehRPM { get; set; }
        private bool _lastTransformed = false;
        internal bool Transformed { get; set; }
        private bool _lastHornActive = false;
        internal bool IsHornActive { get; set; }
        internal bool VehAreLightsOn { get; set; }
        internal bool VehAreBrakeLightsOn = false;
        internal bool VehAreHighBeamsOn { get; set; }
        internal byte VehLandingGear { get; set; }
        internal bool VehRoofOpened { get; set; }
        internal bool VehIsSireneActive { get; set; }
        internal VehicleDamageModel VehDamageModel { get; set; }
        #endregion

        private ulong _lastVehicleEnter = 0;

        private void DisplayInVehicle()
        {
            if (MainVehicle == null || !MainVehicle.Exists() || MainVehicle.Model.Hash != VehicleModelHash)
            {
                Model vehicleModel = VehicleModelHash.ModelRequest();
                if (vehicleModel == null)
                {
                    return;
                }

                Vehicle targetVehicle = World.GetClosestVehicle(Position, 7f, vehicleModel);
                if (targetVehicle != null && targetVehicle.IsSeatFree((VehicleSeat)VehicleSeatIndex))
                {
                    MainVehicle = targetVehicle;
                }
                else
                {
                    MainVehicle = World.CreateVehicle(vehicleModel, Position);
                    MainVehicle.IsInvincible = true;
                    MainVehicle.Quaternion = _vehicleRotation;

                    if (MainVehicle.HasRoof)
                    {
                        bool roofOpened = MainVehicle.RoofState == VehicleRoofState.Opened || MainVehicle.RoofState == VehicleRoofState.Opening;
                        if (roofOpened != VehRoofOpened)
                        {
                            MainVehicle.RoofState = VehRoofOpened ? VehicleRoofState.Opened : VehicleRoofState.Closed;
                        }
                    }
                }

                vehicleModel.MarkAsNoLongerNeeded();
            }

            if (_lastVehicleEnter != 0)
            {
                if (Util.GetTickCount64() - _lastVehicleEnter < 1500)
                {
                    return;
                }

                _lastVehicleEnter = 0;

                Character.Task.ClearAllImmediately();
                Character.SetIntoVehicle(MainVehicle, (VehicleSeat)VehicleSeatIndex);
            }
            else if (!Character.IsInVehicle() || Character.CurrentVehicle.Handle != MainVehicle.Handle)
            {
                if (Game.Player.Character.CurrentVehicle?.Handle == MainVehicle.Handle &&
                    VehicleSeatIndex == (int)Game.Player.Character.SeatIndex)
                {
                    Game.Player.Character.Task.WarpOutOfVehicle(MainVehicle);
                    GTA.UI.Notification.Show("~r~Car jacked!");
                }

                if (VehicleSpeed > 0.2f || !Character.IsInRange(MainVehicle.Position, 4f))
                {
                    Character.SetIntoVehicle(MainVehicle, (VehicleSeat)VehicleSeatIndex);
                }
                else
                {
                    _lastVehicleEnter = Util.GetTickCount64();

                    Character.Task.ClearAllImmediately();
                    Character.Task.EnterVehicle(MainVehicle, (VehicleSeat)VehicleSeatIndex, -1, 2f, EnterVehicleFlags.WarpToDoor);
                }

                return;
            }

            if ((int)Character.SeatIndex != VehicleSeatIndex)
            {
                Character.Task.ShuffleToNextVehicleSeat(MainVehicle);
            }

            if (AimCoords != default)
            {
                if (MainVehicle.IsTurretSeat(VehicleSeatIndex))
                {
                    int gameTime = Game.GameTime;
                    if (gameTime - _lastVehicleAim > 30)
                    {
                        Function.Call(Hash.TASK_VEHICLE_AIM_AT_COORD, Character.Handle, AimCoords.X, AimCoords.Y, AimCoords.Z);
                        _lastVehicleAim = gameTime;
                    }
                }
            }

            if (MainVehicle.GetResponsiblePedHandle() != Character.Handle)
            {
                return;
            }

            UpdateVehicleInfo();
            UpdateVehiclePosition();
        }

        private void UpdateVehicleInfo()
        {
            if (LastSyncWasFull)
            {
                MainVehicle.SetVehicleDamageModel(VehDamageModel);
            }

            if (VehicleColors != null && VehicleColors != _lastVehicleColors)
            {
                Function.Call(Hash.SET_VEHICLE_COLOURS, MainVehicle, VehicleColors[0], VehicleColors[1]);

                _lastVehicleColors = VehicleColors;
            }

            if (Character.IsOnBike && MainVehicle.ClassType == VehicleClass.Cycles)
            {
                bool isFastPedaling = Function.Call<bool>(Hash.IS_ENTITY_PLAYING_ANIM, Character.Handle, PedalingAnimDict(), "fast_pedal_char", 3);
                if (VehicleSpeed < 0.2f)
                {
                    StopPedalingAnim(isFastPedaling);
                }
                else if (VehicleSpeed < 11f && !Function.Call<bool>(Hash.IS_ENTITY_PLAYING_ANIM, Character.Handle, PedalingAnimDict(), "cruise_pedal_char", 3))
                {
                    StartPedalingAnim(false);
                }
                else if (VehicleSpeed >= 11f && !isFastPedaling)
                {
                    StartPedalingAnim(true);
                }
            }
            else
            {
                if (VehicleMods != null && !VehicleMods.Compare(_lastVehicleMods))
                {
                    Function.Call(Hash.SET_VEHICLE_MOD_KIT, MainVehicle, 0);

                    foreach (KeyValuePair<int, int> mod in VehicleMods)
                    {
                        MainVehicle.Mods[(VehicleModType)mod.Key].Index = mod.Value;
                    }

                    _lastVehicleMods = VehicleMods;
                }

                MainVehicle.EngineHealth = VehicleEngineHealth;

                if (VehicleDead && !MainVehicle.IsDead)
                {
                    MainVehicle.IsInvincible = false;
                    MainVehicle.Explode();
                }
                else if (!VehicleDead && MainVehicle.IsDead)
                {
                    MainVehicle.IsInvincible = true;
                    MainVehicle.Repair();
                }

                if (VehIsEngineRunning != MainVehicle.IsEngineRunning)
                {
                    MainVehicle.IsEngineRunning = VehIsEngineRunning;
                }


                if (MainVehicle.IsPlane)
                {
                    if (VehLandingGear != (byte)MainVehicle.LandingGearState)
                    {
                        MainVehicle.LandingGearState = (VehicleLandingGearState)VehLandingGear;
                    }
                }
                else
                {
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

                    if (VehAreLightsOn != MainVehicle.AreLightsOn)
                    {
                        MainVehicle.AreLightsOn = VehAreLightsOn;
                    }

                    if (VehAreHighBeamsOn != MainVehicle.AreHighBeamsOn)
                    {
                        MainVehicle.AreHighBeamsOn = VehAreHighBeamsOn;
                    }

                    if (MainVehicle.HasSiren && VehIsSireneActive != MainVehicle.IsSirenActive)
                    {
                        MainVehicle.IsSirenActive = VehIsSireneActive;
                    }

                    MainVehicle.AreBrakeLightsOn = VehAreBrakeLightsOn;

                    if (IsHornActive)
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
                        if (roofOpened != VehRoofOpened)
                        {
                            MainVehicle.RoofState = VehRoofOpened ? VehicleRoofState.Opening : VehicleRoofState.Closing;
                        }
                    }
                }
            }
        }

        private void UpdateVehiclePosition()
        {
            MainVehicle.CurrentRPM = VehRPM;

            float avrLat = Math.Min(1.5f, (Util.GetTickCount64() - LastUpdateReceived) / AverageLatency);

            if (_lastVehicleSteeringAngle != VehicleSteeringAngle)
            {
                float steeringLerp = Util.Lerp(_lastVehicleSteeringAngle.ToRadians(), VehicleSteeringAngle.ToRadians(), avrLat);
                _lastVehicleSteeringAngle = VehicleSteeringAngle;

                MainVehicle.CustomSteeringAngle(steeringLerp);
            }

            if (VehicleSpeed != 0f && MainVehicle.IsInRange(Position, 7.0f))
            {
                dynamic speedLerp = Util.Lerp(_lastVehicleSpeed, VehicleSpeed, avrLat);
                _lastVehicleSpeed = Speed;

                dynamic force = 1.10f + (float)Math.Sqrt(AverageLatency / 2500) + (speedLerp / 250);
                dynamic forceVelo = 0.77f + (float)Math.Sqrt(AverageLatency / 5000) + (speedLerp / 750);

                MainVehicle.Velocity = Velocity * forceVelo + ((Position - MainVehicle.Position) * force);
                MainVehicle.Quaternion = _lastVehicleRotation != null ? Quaternion.Slerp(_lastVehicleRotation.Value, _vehicleRotation, avrLat) : _vehicleRotation;
            }
            else
            {
                MainVehicle.PositionNoOffset = Position;
                MainVehicle.Quaternion = _vehicleRotation;
            }
        }

        #region -- PEDALING --
        /*
         * Thanks to @oldnapalm.
         */

        private string PedalingAnimDict()
        {
            switch ((VehicleHash)VehicleModelHash)
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
            Character.Task.PlayAnimation(PedalingAnimDict(), PedalingAnimName(fast), 8.0f, -8.0f, -1, AnimationFlags.Loop | AnimationFlags.AllowRotation, 1.0f);
        }

        private void StopPedalingAnim(bool fast)
        {
            Character.Task.ClearAnimation(PedalingAnimDict(), PedalingAnimName(fast));
        }
        #endregion
    }
}
