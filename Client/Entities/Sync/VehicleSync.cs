using System;
using System.Collections.Generic;
using System.Linq;

using GTA;
using GTA.Native;
using GTA.Math;

namespace CoopClient.Entities
{
    public partial class EntitiesPed
    {
        #region -- VARIABLES --
        private ulong VehicleStopTime { get; set; }

        internal bool IsInVehicle { get; set; }
        private int LastVehicleModelHash = 0;
        private int CurrentVehicleModelHash = 0;
        /// <summary>
        /// The latest vehicle model hash (may not have been applied yet)
        /// </summary>
        public int VehicleModelHash
        {
            get => CurrentVehicleModelHash;
            internal set
            {
                LastVehicleModelHash = CurrentVehicleModelHash == 0 ? value : CurrentVehicleModelHash;
                CurrentVehicleModelHash = value;
            }
        }
        private byte[] LastVehicleColors = new byte[] { 0, 0 };
        internal byte[] VehicleColors { get; set; }
        private Dictionary<int, int> LastVehicleMods = new Dictionary<int, int>();
        internal Dictionary<int, int> VehicleMods { get; set; }
        internal bool VehicleDead { get; set; }
        internal float VehicleEngineHealth { get; set; }
        internal short VehicleSeatIndex { get; set; }
        /// <summary>
        /// ?
        /// </summary>
        public Vehicle MainVehicle { get; internal set; }
        /// <summary>
        /// The latest vehicle position (may not have been applied yet)
        /// </summary>
        public Vector3 VehiclePosition { get; internal set; }
        /// <summary>
        /// The latest vehicle rotation (may not have been applied yet)
        /// </summary>
        public Quaternion VehicleRotation { get; internal set; }
        internal Vector3 VehicleVelocity { get; set; }
        private float LastVehicleSpeed { get; set; }
        private float CurrentVehicleSpeed { get; set; }
        internal float VehicleSpeed
        {
            get => CurrentVehicleSpeed;
            set
            {
                LastVehicleSpeed = CurrentVehicleSpeed;
                CurrentVehicleSpeed = value;
            }
        }
        internal float VehicleSteeringAngle { get; set; }
        private int LastVehicleAim;
        internal bool VehIsEngineRunning { get; set; }
        internal float VehRPM { get; set; }
        private bool LastTransformed = false;
        internal bool Transformed { get; set; }
        private bool LastHornActive = false;
        internal bool IsHornActive { get; set; }
        internal bool VehAreLightsOn { get; set; }
        internal bool VehAreHighBeamsOn { get; set; }
        internal byte VehLandingGear { get; set; }

        internal bool VehIsSireneActive { get; set; }
        private VehicleDoors[] LastVehDoors;
        internal VehicleDoors[] VehDoors { get; set; }
        private int LastVehTires;
        internal int VehTires { get; set; }
        #endregion

        private void DisplayInVehicle()
        {
            if (MainVehicle == null || !MainVehicle.Exists() || MainVehicle.Model.Hash != CurrentVehicleModelHash)
            {
                bool vehFound = false;

                if (NPCVehHandle != 0)
                {
                    lock (Main.NPCsVehicles)
                    {
                        if (Main.NPCsVehicles.ContainsKey(NPCVehHandle))
                        {
                            Vehicle targetVehicle = World.GetAllVehicles().First(x => x.Handle == Main.NPCsVehicles[NPCVehHandle]);
                            if (targetVehicle == null)
                            {
                                return;
                            }

                            MainVehicle = targetVehicle;
                            vehFound = true;
                        }
                    }
                }
                else
                {
                    Vehicle targetVehicle = World.GetClosestVehicle(Position, 7f, new Model[] { CurrentVehicleModelHash });

                    if (targetVehicle != null)
                    {
                        if (targetVehicle.IsSeatFree((VehicleSeat)VehicleSeatIndex))
                        {
                            MainVehicle = targetVehicle;
                            vehFound = true;
                        }
                    }
                }

                if (!vehFound)
                {
                    Model vehicleModel = CurrentVehicleModelHash.ModelRequest();
                    if (vehicleModel == null)
                    {
                        //GTA.UI.Notification.Show($"~r~(Vehicle)Model ({CurrentVehicleModelHash}) cannot be loaded!");
                        ModelNotFound = 2;
                        return;
                    }

                    MainVehicle = World.CreateVehicle(vehicleModel, VehiclePosition);
                    vehicleModel.MarkAsNoLongerNeeded();
                    if (NPCVehHandle != 0)
                    {
                        Main.NPCsVehicles.Add(NPCVehHandle, MainVehicle.Handle);
                    }
                    MainVehicle.Quaternion = VehicleRotation;
                }
            }

            if (!Character.IsInVehicle() || (int)Character.SeatIndex != VehicleSeatIndex || Character.CurrentVehicle.Handle != MainVehicle.Handle)
            {
                if (VehicleSeatIndex == -1 &&
                    Game.Player.Character.IsInVehicle() &&
                    (int)Game.Player.Character.SeatIndex == -1 &&
                    Game.Player.Character.CurrentVehicle.Handle == MainVehicle.Handle)
                {
                    Game.Player.Character.Task.WarpOutOfVehicle(MainVehicle);
                    GTA.UI.Notification.Show("~r~Car jacked!");
                }

                Character.SetIntoVehicle(MainVehicle, (VehicleSeat)VehicleSeatIndex);
                Character.IsVisible = true;
            }

            #region -- VEHICLE SYNC --
            if (AimCoords != default)
            {
                if (MainVehicle.IsTurretSeat(VehicleSeatIndex))
                {
                    int gameTime = Game.GameTime;
                    if (gameTime - LastVehicleAim > 30)
                    {
                        Function.Call(Hash.TASK_VEHICLE_AIM_AT_COORD, Character.Handle, AimCoords.X, AimCoords.Y, AimCoords.Z);
                        LastVehicleAim = gameTime;
                    }
                }
            }

            if (MainVehicle.GetResponsiblePedHandle() != Character.Handle)
            {
                return;
            }

            if (VehicleColors != null && VehicleColors != LastVehicleColors)
            {
                Function.Call(Hash.SET_VEHICLE_COLOURS, MainVehicle, VehicleColors[0], VehicleColors[1]);

                LastVehicleColors = VehicleColors;
            }

            if (Character.IsOnBike && MainVehicle.ClassType == VehicleClass.Cycles)
            {
                bool isFastPedaling = Function.Call<bool>(Hash.IS_ENTITY_PLAYING_ANIM, Character.Handle, PedalingAnimDict(), "fast_pedal_char", 3);
                if (CurrentVehicleSpeed < 0.2f)
                {
                    StopPedalingAnim(isFastPedaling);
                }
                else if (CurrentVehicleSpeed < 11f && !Function.Call<bool>(Hash.IS_ENTITY_PLAYING_ANIM, Character.Handle, PedalingAnimDict(), "cruise_pedal_char", 3))
                {
                    StartPedalingAnim(false);
                }
                else if (CurrentVehicleSpeed >= 11f && !isFastPedaling)
                {
                    StartPedalingAnim(true);
                }
            }
            else
            {
                if (VehicleMods != null && VehicleMods != LastVehicleMods)
                {
                    Function.Call(Hash.SET_VEHICLE_MOD_KIT, MainVehicle, 0);

                    foreach (KeyValuePair<int, int> mod in VehicleMods)
                    {
                        MainVehicle.Mods[(VehicleModType)mod.Key].Index = mod.Value;
                    }

                    LastVehicleMods = VehicleMods;
                }

                MainVehicle.EngineHealth = VehicleEngineHealth;

                if (VehicleDead && !MainVehicle.IsDead)
                {
                    MainVehicle.Explode();
                }
                else if (!VehicleDead && MainVehicle.IsDead)
                {
                    MainVehicle.Repair();
                }

                if (VehIsEngineRunning != MainVehicle.IsEngineRunning)
                {
                    MainVehicle.IsEngineRunning = VehIsEngineRunning;
                }

                MainVehicle.CurrentRPM = VehRPM;

                if (VehAreLightsOn != MainVehicle.AreLightsOn)
                {
                    MainVehicle.AreLightsOn = VehAreLightsOn;
                }

                if (VehAreHighBeamsOn != MainVehicle.AreHighBeamsOn)
                {
                    MainVehicle.AreHighBeamsOn = VehAreHighBeamsOn;
                }

                if (MainVehicle.IsSubmarineCar)
                {
                    if (Transformed && !LastTransformed)
                    {
                        LastTransformed = true;
                        Function.Call(Hash._TRANSFORM_VEHICLE_TO_SUBMARINE, MainVehicle, false);
                    }
                    else if (!Transformed && LastTransformed)
                    {
                        LastTransformed = false;
                        Function.Call(Hash._TRANSFORM_SUBMARINE_TO_VEHICLE, MainVehicle, false);
                    }
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
                    if (MainVehicle.HasSiren && VehIsSireneActive != MainVehicle.IsSirenActive)
                    {
                        MainVehicle.IsSirenActive = VehIsSireneActive;
                    }

                    if (IsHornActive && !LastHornActive)
                    {
                        LastHornActive = true;
                        MainVehicle.SoundHorn(99999);
                    }
                    else if (!IsHornActive && LastHornActive)
                    {
                        LastHornActive = false;
                        MainVehicle.SoundHorn(1);
                    }

                    Function.Call(Hash.SET_VEHICLE_BRAKE_LIGHTS, MainVehicle, CurrentVehicleSpeed > 0.2f && LastVehicleSpeed > CurrentVehicleSpeed);

                    if (LastSyncWasFull)
                    {
                        if (VehDoors != null && VehDoors != LastVehDoors)
                        {
                            int doorLength = VehDoors.Length;
                            if (VehDoors.Length != 0)
                            {
                                for (int i = 0; i < (doorLength - 1); i++)
                                {
                                    VehicleDoor door = MainVehicle.Doors[(VehicleDoorIndex)i];
                                    VehicleDoors aDoor = VehDoors[i];

                                    if (aDoor.Broken)
                                    {
                                        if (!door.IsBroken)
                                        {
                                            door.Break();
                                        }
                                        continue;
                                    }
                                    else if (!aDoor.Broken && door.IsBroken)
                                    {
                                        // Repair?
                                        //MainVehicle.Repair();
                                    }

                                    if (aDoor.FullyOpen)
                                    {
                                        if (!door.IsFullyOpen)
                                        {
                                            door.Open(false, true);
                                        }
                                        continue;
                                    }
                                    else if (aDoor.Open)
                                    {
                                        if (!door.IsOpen)
                                        {
                                            door.Open();
                                        }

                                        door.AngleRatio = aDoor.AngleRatio;
                                        continue;
                                    }

                                    door.Close(true);
                                }
                            }

                            LastVehDoors = VehDoors;
                        }

                        if (VehTires != default && LastVehTires != VehTires)
                        {
                            foreach (var wheel in MainVehicle.Wheels.GetAllWheels())
                            {
                                if ((VehTires & 1 << (int)wheel.BoneId) != 0)
                                {
                                    wheel.Puncture();
                                    wheel.Burst();
                                }
                            }

                            LastVehTires = VehTires;
                        }
                    }
                }
            }

            if (VehicleSteeringAngle != MainVehicle.SteeringAngle)
            {
                MainVehicle.CustomSteeringAngle((float)(Math.PI / 180) * VehicleSteeringAngle);
            }

            // Good enough for now, but we need to create a better sync
            if (CurrentVehicleSpeed > 0.05f && MainVehicle.IsInRange(VehiclePosition, 7.0f))
            {
                int forceMultiplier = (Game.Player.Character.IsInVehicle() && MainVehicle.IsTouching(Game.Player.Character.CurrentVehicle)) ? 1 : 3;

                MainVehicle.Velocity = VehicleVelocity + forceMultiplier * (VehiclePosition - MainVehicle.Position);
                MainVehicle.Quaternion = Quaternion.Slerp(MainVehicle.Quaternion, VehicleRotation, 0.5f);

                VehicleStopTime = Util.GetTickCount64();
            }
            else if ((Util.GetTickCount64() - VehicleStopTime) <= 1000)
            {
                Vector3 posTarget = Util.LinearVectorLerp(MainVehicle.Position, VehiclePosition + (VehiclePosition - MainVehicle.Position), Util.GetTickCount64() - VehicleStopTime, 1000);

                MainVehicle.PositionNoOffset = posTarget;
                MainVehicle.Quaternion = Quaternion.Slerp(MainVehicle.Quaternion, VehicleRotation, 0.5f);
            }
            else
            {
                MainVehicle.PositionNoOffset = VehiclePosition;
                MainVehicle.Quaternion = VehicleRotation;
            }
            #endregion
        }

        #region -- PEDALING --
        /*
         * Thanks to @oldnapalm.
         */

        private string PedalingAnimDict()
        {
            switch ((VehicleHash)CurrentVehicleModelHash)
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
