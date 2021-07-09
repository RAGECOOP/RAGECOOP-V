using System;
using System.Drawing;
using System.Collections.Generic;

using GTA;
using GTA.Native;
using GTA.Math;

using LemonUI.Elements;

namespace CoopClient
{
    public class EntitiesPed
    {
        private bool AllDataAvailable = false;
        public bool LastSyncWasFull { get; set; } = false;

        public Ped Character { get; set; }
        public int Health { get; set; }
        private int LastModelHash = 0;
        public int ModelHash { get; set; }
        private Dictionary<int, int> LastProps = new Dictionary<int, int>();
        public Dictionary<int, int> Props { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 Rotation { get; set; }
        public Vector3 Velocity { get; set; }
        public byte Speed { get; set; }
        private bool LastIsJumping = false;
        public bool IsJumping { get; set; }
        public bool IsRagdoll { get; set; }
        public bool IsOnFire { get; set; }
        public Vector3 AimCoords { get; set; }
        public bool IsAiming { get; set; }
        public bool IsShooting { get; set; }
        public bool IsReloading { get; set; }
        public int CurrentWeaponHash { get; set; }

        public Blip PedBlip;

        public void DisplayLocally(string username)
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
                    return;
                }

                AllDataAvailable = true;
            }

            #region NOT_IN_RANGE
            if (!Game.Player.Character.IsInRange(Position, 250f))
            {
                if (Character != null && Character.Exists())
                {
                    Character.Kill();
                    Character.Delete();
                }

                if (username != null)
                {
                    if (PedBlip == null || !PedBlip.Exists())
                    {
                        PedBlip = World.CreateBlip(Position);
                        PedBlip.Color = BlipColor.White;
                        PedBlip.Scale = 0.8f;
                        PedBlip.Name = username;
                    }
                    else
                    {
                        PedBlip.Position = Position;
                    }
                }

                return;
            }
            #endregion

            #region IS_IN_RANGE
            if (PedBlip != null && PedBlip.Exists())
            {
                PedBlip.Delete();
            }

            bool characterExist = Character != null && Character.Exists();

            if (!characterExist)
            {
                CreateCharacter(username);
            }
            else if (LastSyncWasFull)
            {
                if (ModelHash != LastModelHash)
                {
                    if (characterExist)
                    {
                        Character.Kill();
                        Character.Delete();
                    }

                    CreateCharacter(username);
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

            if (username != null && Character.IsInRange(Game.Player.Character.Position, 20f))
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

            if (IsReloading && !Character.IsReloading)
            {
                Character.Task.ClearAll();
                Character.Task.ReloadWeapon();
            }

            if (IsReloading)
            {
                return;
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
            #endregion
        }

        private void CreateCharacter(string username)
        {
            LastModelHash = ModelHash;
            LastProps = Props;

            Character = World.CreatePed(new Model(ModelHash), Position, Rotation.Z);
            Character.RelationshipGroup = Main.RelationshipGroup;
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
        }

        private bool LastMoving;
        private void WalkTo()
        {
            if (!Character.IsInRange(Position, 7.0f) && (LastMoving = true))
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
                            Character.Task.StandStill(2000);
                        }
                        break;
                }
            }
        }
    }
}
