using System.Collections.Generic;

using GTA;
using GTA.Native;
using GTA.Math;

namespace CoopClient.Entities
{
    public partial class EntitiesPed
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

                        Function.Call(Hash.GIVE_WEAPON_OBJECT_TO_PED, LastWeaponObj, Character);
                    }
                }

                LastWeaponComponents = WeaponComponents;
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
