using System;
using System.Collections.Generic;

using GTA;
using GTA.Native;
using GTA.Math;

namespace CoopClient.Entities.Player
{
    public partial class EntitiesPlayer
    {
        #region -- ON FOOT --
        /// <summary>
        /// The latest character rotation (may not have been applied yet)
        /// </summary>
        public Vector3 Rotation { get; internal set; }
        /// <summary>
        /// The latest character velocity (may not have been applied yet)
        /// </summary>
        public Vector3 Velocity { get; internal set; }
        internal byte Speed { get; set; }
        private bool LastIsJumping = false;
        internal bool IsJumping { get; set; }
        internal bool IsOnLadder { get; set; }
        internal bool IsVaulting { get; set; }
        internal bool IsInParachuteFreeFall { get; set; }
        internal bool IsRagdoll { get; set; }
        internal bool IsOnFire { get; set; }
        internal bool IsAiming { get; set; }
        internal bool IsShooting { get; set; }
        internal bool IsReloading { get; set; }
        internal uint CurrentWeaponHash { get; set; }
        private Dictionary<uint, bool> LastWeaponComponents = null;
        internal Dictionary<uint, bool> WeaponComponents { get; set; } = null;
        private int LastWeaponObj = 0;
        #endregion

        private bool IsPlayingAnimation = false;
        private string[] CurrentAnimation = new string[2] { "", ""};
        private float AnimationStopTime = 0;

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

            if (IsInParachuteFreeFall)
            {
                if (!Character.IsInParachuteFreeFall)
                {
                    Function.Call(Hash.TASK_SKY_DIVE, Character.Handle);
                }
                UpdateOnFootPosition(true, true, false);

                return;
            }

            if (IsOnLadder)
            {
                if (!Function.Call<bool>(Hash.GET_IS_TASK_ACTIVE, Character.Handle, ETasks.CLIMB_LADDER))
                {
                    Character.Task.ClimbLadder();
                }
                
                UpdateOnFootPosition(true, true, false);

                return;
            }
            else if (!IsOnLadder && Function.Call<bool>(Hash.GET_IS_TASK_ACTIVE, Character.Handle, ETasks.CLIMB_LADDER))
            {
                Character.Task.ClearAllImmediately();
            }

            if (IsVaulting)
            {
                if (!Character.IsVaulting)
                {
                    Character.Task.Climb();
                }

                UpdateOnFootPosition(true, true, false);

                return;
            }
            else if (!IsVaulting && Character.IsVaulting)
            {
                Character.Task.ClearAllImmediately();
            }

            if (IsOnFire && !Character.IsOnFire)
            {
                Character.IsInvincible = false;

                Function.Call(Hash.START_ENTITY_FIRE, Character.Handle);
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

            if (IsJumping)
            {
                if (!LastIsJumping)
                {
                    LastIsJumping = true;
                    Character.Task.Jump();
                }

                UpdateOnFootPosition(true, true, false);
                return;
            }
            LastIsJumping = false;

            if (IsRagdoll)
            {
                if (!Character.IsRagdoll)
                {
                    // CanRagdoll = true, inside this function
                    //Character.Ragdoll();

                    Character.CanRagdoll = true;
                    Function.Call(Hash.SET_PED_TO_RAGDOLL, Character.Handle, 50000, 60000, 0, 1, 1, 1);
                }

                UpdateOnFootPosition(false, false, true);

                return;
            }
            else if (!IsRagdoll && Character.IsRagdoll)
            {
                Character.CanRagdoll = false;
                Character.Task.ClearAllImmediately();

                IsPlayingAnimation = true;
                CurrentAnimation = new string[2] { "anim@sports@ballgame@handball@", "ball_get_up" };
                AnimationStopTime = 0.7f;

                Function.Call(Hash.TASK_PLAY_ANIM, Character.Handle, LoadAnim("anim@sports@ballgame@handball@"), "ball_get_up", 12f, 12f, -1, 0, -10f, 1, 1, 1);
                return;
            }

            if (!StopAnimation())
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

                UpdateOnFootPosition();

                return;
            }

            CheckCurrentWeapon();

            if (IsShooting)
            {
                DisplayShooting();
            }
            else if (IsAiming)
            {
                DisplayAiming();
            }
            else
            {
                WalkTo();
            }
        }

        #region WEAPON
        private void CheckCurrentWeapon()
        {
            if (Character.Weapons.Current.Hash != (WeaponHash)CurrentWeaponHash || !WeaponComponents.Compare(LastWeaponComponents))
            {
                Character.Weapons.RemoveAll();

                if (CurrentWeaponHash != (uint)WeaponHash.Unarmed)
                {
                    if (WeaponComponents == null || WeaponComponents.Count == 0)
                    {
                        Character.Weapons.Give((WeaponHash)CurrentWeaponHash, -1, true, true);
                    }
                    else
                    {
                        LastWeaponObj = Function.Call<int>(Hash.CREATE_WEAPON_OBJECT, CurrentWeaponHash, -1, Position.X, Position.Y, Position.Z, true, 0, 0);

                        foreach (KeyValuePair<uint, bool> comp in WeaponComponents)
                        {
                            if (comp.Value)
                            {
                                Function.Call(Hash.GIVE_WEAPON_COMPONENT_TO_WEAPON_OBJECT, LastWeaponObj, comp.Key);
                            }
                        }

                        Function.Call(Hash.GIVE_WEAPON_OBJECT_TO_PED, LastWeaponObj, Character.Handle);
                    }
                }

                LastWeaponComponents = WeaponComponents;
            }
        }

        private void DisplayShooting()
        {
            if (!Character.IsInRange(Position, 0.5f))
            {
                Function.Call(Hash.TASK_GO_TO_COORD_WHILE_AIMING_AT_COORD, Character.Handle, Position.X, Position.Y,
                                Position.Z, AimCoords.X, AimCoords.Y, AimCoords.Z, 3f, true, 2.0f, 0.5f, false, 0, false,
                                unchecked((uint)FiringPattern.FullAuto));
                UpdateOnFootPosition();
            }
            else
            {
                Function.Call(Hash.TASK_SHOOT_AT_COORD, Character.Handle, AimCoords.X, AimCoords.Y, AimCoords.Z, 1500, unchecked((uint)FiringPattern.FullAuto));
            }
        }

        private void DisplayAiming()
        {
            if (!Character.IsInRange(Position, 0.5f))
            {
                Function.Call(Hash.TASK_GO_TO_COORD_WHILE_AIMING_AT_COORD, Character.Handle, Position.X, Position.Y,
                                Position.Z, AimCoords.X, AimCoords.Y, AimCoords.Z, 3f, false, 0x3F000000, 0x40800000, false, 512, false, 0);
                UpdateOnFootPosition();
            }
            else
            {
                Character.Task.AimAt(AimCoords, 100);
            }
        }
        #endregion

        private bool StopAnimation()
        {
            if (!IsPlayingAnimation)
            {
                return true;
            }

            switch (CurrentAnimation[0])
            {
                case "anim@sports@ballgame@handball@":
                    UpdateOnFootPosition(true, true, false);
                    float currentTime = Function.Call<float>(Hash.GET_ENTITY_ANIM_CURRENT_TIME, Character.Handle, "anim@sports@ballgame@handball@", CurrentAnimation[1]);

                    if (currentTime < AnimationStopTime)
                    {
                        return false;
                    }
                    break;
            }

            Character.Task.ClearAnimation(CurrentAnimation[0], CurrentAnimation[1]);
            Character.Task.ClearAll();
            IsPlayingAnimation = false;
            CurrentAnimation = new string[2] { "", "" };
            AnimationStopTime = 0;

            return true;
        }

        private bool LastMoving;
        private void WalkTo()
        {
            Vector3 predictPosition = Position + (Position - Character.Position) + Velocity;
            float range = predictPosition.DistanceToSquared(Character.Position);

            switch (Speed)
            {
                case 1:
                    if (!Character.IsWalking || range > 0.25f)
                    {
                        float nrange = range * 2;
                        if (nrange > 1.0f)
                        {
                            nrange = 1.0f;
                        }

                        Character.Task.GoStraightTo(predictPosition);
                        Function.Call(Hash.SET_PED_DESIRED_MOVE_BLEND_RATIO, Character.Handle, nrange);
                    }
                    LastMoving = true;
                    break;
                case 2:
                    if (!Character.IsRunning || range > 0.50f)
                    {
                        Character.Task.RunTo(predictPosition, true);
                        Function.Call(Hash.SET_PED_DESIRED_MOVE_BLEND_RATIO, Character.Handle, 1.0f);
                    }
                    LastMoving = true;
                    break;
                case 3:
                    if (!Character.IsSprinting || range > 0.75f)
                    {
                        Function.Call(Hash.TASK_GO_STRAIGHT_TO_COORD, Character.Handle, predictPosition.X, predictPosition.Y, predictPosition.Z, 3.0f, -1, 0.0f, 0.0f);
                        Function.Call(Hash.SET_RUN_SPRINT_MULTIPLIER_FOR_PLAYER, Character.Handle, 1.49f);
                        Function.Call(Hash.SET_PED_DESIRED_MOVE_BLEND_RATIO, Character.Handle, 1.0f);
                    }
                    LastMoving = true;
                    break;
                default:
                    if (LastMoving)
                    {
                        Character.Task.StandStill(2000);
                        LastMoving = false;
                    }
                    break;
            }
            UpdateOnFootPosition();
        }

        private byte LastStuckCount = 0;
        private ulong LastStuckTime;
        private void UpdateOnFootPosition(bool updatePosition = true, bool updateRotation = true, bool updateVelocity = true)
        {
            if (Character.Position.DistanceTo(Position) > 5f)
            {
                if (Util.GetTickCount64() - LastStuckTime < 1000)
                {
                    LastStuckCount = 0;
                }

                ++LastStuckCount;

                if (LastStuckCount >= 5)
                {
                    Character.Position = Position;
                    LastStuckCount = 0;
                }

                LastStuckTime = Util.GetTickCount64();
            }

            if (updatePosition)
            {
                float lerpValue = ((int)((Latency * 1000 / 2) + Main.MainNetworking.Latency * 1000 / 2)) * 2 / 50000f;

                Vector2 biDimensionalPos = Vector2.Lerp(new Vector2(Character.Position.X, Character.Position.Y), new Vector2(Position.X + (Velocity.X / 5), Position.Y + (Velocity.Y / 5)), lerpValue);
                float zPos = Util.Lerp(Character.Position.Z, Position.Z, 0.1f);
                Character.PositionNoOffset = new Vector3(biDimensionalPos.X, biDimensionalPos.Y, zPos);
            }

            if (updateRotation)
            {
                // You can find the ToQuaternion() for Rotation inside the VectorExtensions
                Character.Quaternion = Quaternion.Lerp(Character.Quaternion, Rotation.ToQuaternion(), 0.10f);
            }

            if (updateVelocity)
            {
                Character.Velocity = Velocity;
            }
        }

        private string LoadAnim(string anim)
        {
            ulong startTime = Util.GetTickCount64();

            while (!Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED, anim))
            {
                Script.Yield();
                Function.Call(Hash.REQUEST_ANIM_DICT, anim);
                if (Util.GetTickCount64() - startTime >= 1000)
                {
                    break;
                }
            }

            return anim;
        }
    }
}
