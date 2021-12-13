using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;

using GTA;
using GTA.Native;
using GTA.Math;

using LemonUI.Elements;

namespace CoopClient.Entities
{
    /// <summary>
    /// ?
    /// </summary>
    public class EntitiesPed
    {
        /// <summary>
        /// 0 = Nothing
        /// 1 = Character
        /// 2 = Vehicle
        /// </summary>
        private byte ModelNotFound = 0;
        private bool AllDataAvailable = false;
        /// <summary>
        /// ?
        /// </summary>
        public bool LastSyncWasFull { get; set; } = false;
        /// <summary>
        /// ?
        /// </summary>
        public ulong LastUpdateReceived { get; set; }
        /// <summary>
        /// ?
        /// </summary>
        public float Latency { get; set; }

        /// <summary>
        /// ?
        /// </summary>
        public Ped Character { get; set; }
        /// <summary>
        /// ?
        /// </summary>
        public int Health { get; set; }
        private int LastModelHash = 0;
        private int CurrentModelHash = 0;
        /// <summary>
        /// ?
        /// </summary>
        public int ModelHash
        {
            set
            {
                LastModelHash = LastModelHash == 0 ? value : CurrentModelHash;
                CurrentModelHash = value;
            }
        }
        private Dictionary<int, int> LastProps = new Dictionary<int, int>();
        /// <summary>
        /// ?
        /// </summary>
        public Dictionary<int, int> Props { get; set; }
        /// <summary>
        /// ?
        /// </summary>
        public Vector3 Position { get; set; }

        #region -- ON FOOT --
        /// <summary>
        /// ?
        /// </summary>
        public Vector3 Rotation { get; set; }
        /// <summary>
        /// ?
        /// </summary>
        public Vector3 Velocity { get; set; }
        /// <summary>
        /// ?
        /// </summary>
        public byte Speed { get; set; }
        private bool LastIsJumping = false;
        /// <summary>
        /// ?
        /// </summary>
        public bool IsJumping { get; set; }
        /// <summary>
        /// ?
        /// </summary>
        public bool IsRagdoll { get; set; }
        /// <summary>
        /// ?
        /// </summary>
        public bool IsOnFire { get; set; }
        /// <summary>
        /// ?
        /// </summary>
        public Vector3 AimCoords { get; set; }
        /// <summary>
        /// ?
        /// </summary>
        public bool IsAiming { get; set; }
        /// <summary>
        /// ?
        /// </summary>
        public bool IsShooting { get; set; }
        /// <summary>
        /// ?
        /// </summary>
        public bool IsReloading { get; set; }
        /// <summary>
        /// ?
        /// </summary>
        public int CurrentWeaponHash { get; set; }
        #endregion

        /// <summary>
        /// ?
        /// </summary>
        public Blip PedBlip;

        #region -- IN VEHICLE --
        private ulong VehicleStopTime { get; set; }

        /// <summary>
        /// ?
        /// </summary>
        public bool IsInVehicle { get; set; }
        private int LastVehicleModelHash = 0;
        private int CurrentVehicleModelHash = 0;
        /// <summary>
        /// ?
        /// </summary>
        public int VehicleModelHash
        {
            set
            {
                LastVehicleModelHash = CurrentVehicleModelHash == 0 ? value : CurrentVehicleModelHash;
                CurrentVehicleModelHash = value;
            }
        }
        private int[] LastVehicleColors = new int[] { 0, 0 };
        /// <summary>
        /// ?
        /// </summary>
        public int[] VehicleColors { get; set; }
        private Dictionary<int, int> LastVehicleMods = new Dictionary<int, int>();
        /// <summary>
        /// ?
        /// </summary>
        public Dictionary<int, int> VehicleMods { get; set; }
        /// <summary>
        /// ?
        /// </summary>
        public bool VehicleDead { get; set; }
        /// <summary>
        /// ?
        /// </summary>
        public float VehicleEngineHealth { get; set; }
        /// <summary>
        /// ?
        /// </summary>
        public int VehicleSeatIndex { get; set; }
        /// <summary>
        /// ?
        /// </summary>
        public Vehicle MainVehicle { get; set; }
        /// <summary>
        /// ?
        /// </summary>
        public Vector3 VehiclePosition { get; set; }
        /// <summary>
        /// ?
        /// </summary>
        public Quaternion VehicleRotation { get; set; }
        /// <summary>
        /// ?
        /// </summary>
        public Vector3 VehicleVelocity { get; set; }
        private float LastVehicleSpeed { get; set; }
        private float CurrentVehicleSpeed { get; set; }
        /// <summary>
        /// ?
        /// </summary>
        public float VehicleSpeed
        {
            set
            {
                LastVehicleSpeed = CurrentVehicleSpeed;
                CurrentVehicleSpeed = value;
            }
        }
        /// <summary>
        /// ?
        /// </summary>
        public float VehicleSteeringAngle { get; set; }
        private int LastVehicleAim;
        /// <summary>
        /// ?
        /// </summary>
        public bool VehIsEngineRunning { get; set; }
        /// <summary>
        /// ?
        /// </summary>
        public float VehRPM { get; set; }
        /// <summary>
        /// ?
        /// </summary>
        public bool VehAreLightsOn { get; set; }
        /// <summary>
        /// ?
        /// </summary>
        public bool VehAreHighBeamsOn { get; set; }
        /// <summary>
        /// ?
        /// </summary>
        public bool VehIsSireneActive { get; set; }
        private VehicleDoors[] LastVehDoors;
        /// <summary>
        /// ?
        /// </summary>
        public VehicleDoors[] VehDoors { get; set; }
        private int LastVehTires;
        /// <summary>
        /// ?
        /// </summary>
        public int VehTires { get; set; }
        #endregion

        internal void DisplayLocally(string username)
        {
            /*
             * username: string
             *   string: null
             *     ped: npc
             *   string: value
             *     ped: player
             */

            // Check beforehand whether ped has all the required data
            if (!AllDataAvailable)
            {
                if (!LastSyncWasFull)
                {
                    if (Position != null)
                    {
                        if (PedBlip != null && PedBlip.Exists())
                        {
                            PedBlip.Position = Position;
                        }
                        else
                        {
                            PedBlip = World.CreateBlip(Position);
                            PedBlip.Color = BlipColor.White;
                            PedBlip.Scale = 0.8f;
                            PedBlip.Name = username;
                        }
                    }

                    return;
                }

                AllDataAvailable = true;
            }

            if (ModelNotFound != 0)
            {
                if (ModelNotFound == 1)
                {
                    if (CurrentModelHash != LastModelHash)
                    {
                        ModelNotFound = 0;
                    }
                }
                else
                {
                    if (CurrentVehicleModelHash != LastVehicleModelHash)
                    {
                        ModelNotFound = 0;
                    }
                }
            }

            #region NOT_IN_RANGE
            if (ModelNotFound != 0 || !Game.Player.Character.IsInRange(Position, 500f))
            {
                if (Character != null && Character.Exists())
                {
                    Character.Kill();
                    Character.MarkAsNoLongerNeeded();
                    Character.Delete();
                    Character = null;
                }

                if (MainVehicle != null && MainVehicle.Exists() && MainVehicle.IsSeatFree(VehicleSeat.Driver) && MainVehicle.PassengerCount == 0)
                {
                    MainVehicle.MarkAsNoLongerNeeded();
                    MainVehicle.Delete();
                    MainVehicle = null;
                }

                if (username != null)
                {
                    if (PedBlip != null && PedBlip.Exists())
                    {
                        PedBlip.Position = Position;
                    }
                    else
                    {
                        PedBlip = World.CreateBlip(Position);
                        PedBlip.Color = BlipColor.White;
                        PedBlip.Scale = 0.8f;
                        PedBlip.Name = username;
                    }
                }

                return;
            }
            #endregion

            #region IS_IN_RANGE
            bool characterExist = Character != null && Character.Exists();

            if (!characterExist)
            {
                if (!CreateCharacter(username))
                {
                    return;
                }
            }
            else if (LastSyncWasFull)
            {
                if (CurrentModelHash != LastModelHash)
                {
                    Character.Kill();
                    Character.Delete();

                    if (!CreateCharacter(username))
                    {
                        return;
                    }
                }
                else if (Props != LastProps)
                {
                    foreach (KeyValuePair<int, int> prop in Props)
                    {
                        Function.Call(Hash.SET_PED_COMPONENT_VARIATION, Character.Handle, prop.Key, prop.Value, 0, 0);
                    }

                    LastProps = Props;
                }
            }

            if (username != null && Character.IsVisible && Character.IsInRange(Game.Player.Character.Position, 20f))
            {
                float sizeOffset;
                if (GameplayCamera.IsFirstPersonAimCamActive)
                {
                    Vector3 targetPos = Character.Bones[Bone.IKHead].Position + new Vector3(0, 0, 0.10f) + (Character.Velocity / Game.FPS);

                    Function.Call(Hash.SET_DRAW_ORIGIN, targetPos.X, targetPos.Y, targetPos.Z, 0);

                    sizeOffset = Math.Max(1f - ((GameplayCamera.Position - Character.Position).Length() / 30f), 0.30f);
                }
                else
                {
                    Vector3 targetPos = Character.Bones[Bone.IKHead].Position + new Vector3(0, 0, 0.35f) + (Character.Velocity / Game.FPS);

                    Function.Call(Hash.SET_DRAW_ORIGIN, targetPos.X, targetPos.Y, targetPos.Z, 0);

                    sizeOffset = Math.Max(1f - ((GameplayCamera.Position - Character.Position).Length() / 25f), 0.25f);
                }

                new ScaledText(new PointF(0, 0), username, 0.4f * sizeOffset, GTA.UI.Font.ChaletLondon)
                {
                    Outline = true,
                    Alignment = GTA.UI.Alignment.Center
                }.Draw();

                Function.Call(Hash.CLEAR_DRAW_ORIGIN);
            }

            if (Character.IsDead)
            {
                if (Health <= 0)
                {
                    return;
                }

                Character.IsInvincible = true;
                Character.Resurrect();
            }
            else if (Character.Health != Health)
            {
                Character.Health = Health;

                if (Health <= 0 && !Character.IsDead)
                {
                    Character.IsInvincible = false;
                    Character.Kill();
                    return;
                }
            }

            if (IsInVehicle)
            {
                DisplayInVehicle();
            }
            else
            {
                DisplayOnFoot();
            }
            #endregion
        }

        private void DisplayInVehicle()
        {
            if (MainVehicle == null || !MainVehicle.Exists() || MainVehicle.Model.Hash != CurrentVehicleModelHash)
            {
                Vehicle targetVehicle = World.GetClosestVehicle(Position, 7f, new Model[] { CurrentVehicleModelHash });

                bool vehFound = false;

                if (targetVehicle != null)
                {
                    if (targetVehicle.IsSeatFree((VehicleSeat)VehicleSeatIndex))
                    {
                        MainVehicle = targetVehicle;
                        vehFound = true;
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

                    MainVehicle = World.CreateVehicle(vehicleModel, VehiclePosition, VehicleRotation.W);
                    vehicleModel.MarkAsNoLongerNeeded();
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

                if (VehIsSireneActive != MainVehicle.IsSirenActive)
                {
                    MainVehicle.IsSirenActive = VehIsSireneActive;
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

        private void DisplayOnFoot()
        {
            if (Character.IsInVehicle())
            {
                Character.Task.LeaveVehicle();
            }

            if (MainVehicle != null)
            {
                MainVehicle = null;
            }

            if (IsOnFire && !Character.IsOnFire)
            {
                Character.IsInvincible = false;

                Function.Call(Hash.START_ENTITY_FIRE, Character.Handle);

                return;
            }
            else if (!IsOnFire && Character.IsOnFire)
            {
                Function.Call(Hash.STOP_ENTITY_FIRE, Character.Handle);

                Character.IsInvincible = true;

                if (Character.IsDead)
                {
                    Character.Resurrect();
                }
            }

            if (IsJumping && !LastIsJumping)
            {
                Character.Task.Jump();
            }

            LastIsJumping = IsJumping;

            if (IsRagdoll && !Character.IsRagdoll)
            {
                Character.CanRagdoll = true;
                Character.Ragdoll();

                return;
            }
            else if (!IsRagdoll && Character.IsRagdoll)
            {
                Character.CancelRagdoll();
                Character.CanRagdoll = false;
            }

            if (IsJumping || IsOnFire)
            {
                return;
            }

            if (IsReloading)
            {
                if (!Character.IsReloading)
                {
                    Character.Task.ClearAll();
                    Character.Task.ReloadWeapon();
                }

                if (Character.IsInRange(Position, 0.5f))
                {
                    return;
                }
            }

            if (Character.Weapons.Current.Hash != (WeaponHash)CurrentWeaponHash)
            {
                Character.Weapons.RemoveAll();
                Character.Weapons.Give((WeaponHash)CurrentWeaponHash, -1, true, true);
            }

            if (IsShooting)
            {
                if (!Character.IsInRange(Position, 0.5f))
                {
                    Function.Call(Hash.TASK_GO_TO_COORD_WHILE_AIMING_AT_COORD, Character.Handle, Position.X, Position.Y,
                                    Position.Z, AimCoords.X, AimCoords.Y, AimCoords.Z, Speed == 3 ? 3f : 2.5f, true, 0x3F000000, 0x40800000, false, 0, false,
                                    unchecked((int)FiringPattern.FullAuto));
                }
                else
                {
                    Function.Call(Hash.TASK_SHOOT_AT_COORD, Character.Handle, AimCoords.X, AimCoords.Y, AimCoords.Z, 1500, unchecked((int)FiringPattern.FullAuto));
                }
            }
            else if (IsAiming)
            {
                if (!Character.IsInRange(Position, 0.5f))
                {
                    Function.Call(Hash.TASK_GO_TO_COORD_WHILE_AIMING_AT_COORD, Character.Handle, Position.X, Position.Y,
                                    Position.Z, AimCoords.X, AimCoords.Y, AimCoords.Z, Speed == 3 ? 3f : 2.5f, false, 0x3F000000, 0x40800000, false, 512, false,
                                    unchecked((int)FiringPattern.FullAuto));
                }
                else
                {
                    Character.Task.AimAt(AimCoords, 100);
                }
            }
            else
            {
                WalkTo();
            }
        }

        private bool CreateCharacter(string username)
        {
            if (PedBlip != null && PedBlip.Exists())
            {
                PedBlip.Delete();
                PedBlip = null;
            }

            LastProps = Props;

            Model characterModel = CurrentModelHash.ModelRequest();

            if (characterModel == null)
            {
                //GTA.UI.Notification.Show($"~r~(Character)Model ({CurrentModelHash}) cannot be loaded!");
                ModelNotFound = 1;
                return false;
            }

            Character = World.CreatePed(characterModel, Position, Rotation.Z);
            characterModel.MarkAsNoLongerNeeded();
            Character.RelationshipGroup = Main.RelationshipGroup;
            if (IsInVehicle)
            {
                Character.IsVisible = false;
            }
            Character.BlockPermanentEvents = true;
            Character.CanRagdoll = false;
            Character.IsInvincible = true;
            Character.Health = Health;

            if (username != null)
            {
                // Add a new blip for the ped
                Character.AddBlip();
                Character.AttachedBlip.Color = BlipColor.White;
                Character.AttachedBlip.Scale = 0.8f;
                Character.AttachedBlip.Name = username;

                Function.Call(Hash.SET_PED_CAN_EVASIVE_DIVE, Character.Handle, false);
                Function.Call(Hash.SET_PED_GET_OUT_UPSIDE_DOWN_VEHICLE, Character.Handle, false);
            }

            foreach (KeyValuePair<int, int> prop in Props)
            {
                Function.Call(Hash.SET_PED_COMPONENT_VARIATION, Character.Handle, prop.Key, prop.Value, 0, 0);
            }

            Function.Call(Hash.SET_PED_CAN_BE_TARGETTED, Character, Game.Player, true);

            return true;
        }

        private bool LastMoving;
        private void WalkTo()
        {
            if (!Character.IsInRange(Position, 6.0f) && (LastMoving = true))
            {
                Character.Position = Position;
                Character.Rotation = Rotation;
            }
            else
            {
                Vector3 predictPosition = Position + (Position - Character.Position) + Velocity;
                float range = predictPosition.DistanceToSquared(Character.Position);

                switch (Speed)
                {
                    case 1:
                        if ((!Character.IsWalking || range > 0.25f) && (LastMoving = true))
                        {
                            float nrange = range * 2;
                            if (nrange > 1.0f)
                            {
                                nrange = 1.0f;
                            }

                            Character.Task.GoStraightTo(predictPosition);
                            Function.Call(Hash.SET_PED_DESIRED_MOVE_BLEND_RATIO, Character.Handle, nrange);
                        }
                        break;
                    case 2:
                        if ((!Character.IsRunning || range > 0.50f) && (LastMoving = true))
                        {
                            Character.Task.RunTo(predictPosition, true);
                            Function.Call(Hash.SET_PED_DESIRED_MOVE_BLEND_RATIO, Character.Handle, 1.0f);
                        }
                        break;
                    case 3:
                        if ((!Character.IsSprinting || range > 0.75f) && (LastMoving = true))
                        {
                            Function.Call(Hash.TASK_GO_STRAIGHT_TO_COORD, Character.Handle, predictPosition.X, predictPosition.Y, predictPosition.Z, 3.0f, -1, 0.0f, 0.0f);
                            Function.Call(Hash.SET_RUN_SPRINT_MULTIPLIER_FOR_PLAYER, Character.Handle, 1.49f);
                            Function.Call(Hash.SET_PED_DESIRED_MOVE_BLEND_RATIO, Character.Handle, 1.0f);
                        }
                        break;
                    default:
                        if (!Character.IsInRange(Position, 0.5f))
                        {
                            Character.Task.RunTo(Position, true, 500);
                        }
                        else if (LastMoving && (LastMoving = false))
                        {
                            Character.Task.StandStill(1000);
                        }
                        break;
                }
            }
        }
    }
}
