using GTA;
using GTA.Math;
using GTA.Native;
using RageCoop.Core;
using System;
using System.Collections.Generic;

namespace RageCoop.Client
{
    internal static partial class PedExtensions
    {

        public static bool IsBetween<T>(this T item, T start, T end)
        {
            return Comparer<T>.Default.Compare(item, start) >= 0 && Comparer<T>.Default.Compare(item, end) <= 0;
        }

        public static bool Compare<T, Y>(this Dictionary<T, Y> item, Dictionary<T, Y> item2)
        {
            if (item == null || item2 == null || item.Count != item2.Count)
            {
                return false;
            }

            foreach (KeyValuePair<T, Y> pair in item)
            {
                if (item2.TryGetValue(pair.Key, out Y value) && Equals(value, pair.Value))
                {
                    continue;
                }

                // TryGetValue() or Equals failed
                return false;
            }

            // No difference between item and item2
            return true;
        }




        #region PED

        public static byte GetPedSpeed(this Ped ped)
        {

            if (ped.IsSittingInVehicle())
            {
                return 4;
            }
            if (ped.IsTaskActive(TaskType.CTaskEnterVehicle))
            {
                return 5;
            }
            if (ped.IsTaskActive(TaskType.CTaskExitVehicle))
            {
                return 6;
            }
            if (ped.IsWalking)
            {
                return 1;
            }
            if (ped.IsRunning)
            {
                return 2;
            }
            if (ped.IsSprinting)
            {
                return 3;
            }


            return 0;
        }

        // Not sure whether component will always be lesser than 255, whatever...
        public static byte[] GetPedClothes(this Ped ped)
        {
            var result = new byte[36];
            for (byte i = 0; i < 12; i++)
            {
                result[i] = (byte)Function.Call<short>(Hash.GET_PED_DRAWABLE_VARIATION, ped.Handle, i);
                result[i + 12] = (byte)Function.Call<short>(Hash.GET_PED_TEXTURE_VARIATION, ped.Handle, i);
                result[i + 24] = (byte)Function.Call<short>(Hash.GET_PED_PALETTE_VARIATION, ped.Handle, i);
            }
            return result;
        }

        public static PedDataFlags GetPedFlags(this Ped ped)
        {
            PedDataFlags flags = PedDataFlags.None;

            if (ped.IsAiming || ped.IsOnTurretSeat())
            {
                flags |= PedDataFlags.IsAiming;
            }


            if (ped.IsReloading)
            {
                flags |= PedDataFlags.IsReloading;
            }

            if (ped.IsJumping)
            {
                flags |= PedDataFlags.IsJumping;
            }

            // Fake death
            if (ped.IsRagdoll || (ped.Health == 1 && ped.IsPlayer))
            {
                flags |= PedDataFlags.IsRagdoll;
            }

            if (ped.IsOnFire)
            {
                flags |= PedDataFlags.IsOnFire;
            }

            if (ped.IsInParachuteFreeFall)
            {
                flags |= PedDataFlags.IsInParachuteFreeFall;
            }

            if (ped.ParachuteState == ParachuteState.Gliding)
            {
                flags |= PedDataFlags.IsParachuteOpen;
            }

            bool climbingLadder = ped.IsTaskActive(TaskType.CTaskGoToAndClimbLadder);
            if (climbingLadder)
            {
                flags |= PedDataFlags.IsOnLadder;
            }

            if (ped.IsVaulting && !climbingLadder)
            {
                flags |= PedDataFlags.IsVaulting;
            }

            if (ped.IsInCover || ped.IsGoingIntoCover)
            {
                flags |= PedDataFlags.IsInCover;
                if (ped.IsInCoverFacingLeft)
                {
                    flags |= PedDataFlags.IsInCover;
                }
                if (!Function.Call<bool>(Hash.IS_PED_IN_HIGH_COVER, ped))
                {
                    flags |= PedDataFlags.IsInLowCover;
                }
                if (ped.IsTaskActive(TaskType.CTaskAimGunBlindFire))
                {
                    flags |= PedDataFlags.IsBlindFiring;
                }
            }

            if (ped.IsInvincible)
            {
                flags |= PedDataFlags.IsInvincible;
            }

            if (Function.Call<bool>(Hash.GET_PED_STEALTH_MOVEMENT, ped))
            {
                flags |= PedDataFlags.IsInStealthMode;
            }


            return flags;
        }

        public static string[] GetReloadingAnimation(this Ped ped)
        {
            switch (ped.Weapons.Current.Hash)
            {
                case WeaponHash.Revolver:
                case WeaponHash.RevolverMk2:
                case WeaponHash.DoubleActionRevolver:
                case WeaponHash.NavyRevolver:
                    return new string[2] { "anim@weapons@pistol@revolver_str", "reload_aim" };
                case WeaponHash.APPistol:
                    return new string[2] { "weapons@pistol@ap_pistol_str", "reload_aim" };
                case WeaponHash.Pistol50:
                    return new string[2] { "weapons@pistol@pistol_50_str", "reload_aim" };
                case WeaponHash.Pistol:
                case WeaponHash.PistolMk2:
                case WeaponHash.PericoPistol:
                case WeaponHash.SNSPistol:
                case WeaponHash.SNSPistolMk2:
                case WeaponHash.HeavyPistol:
                case WeaponHash.VintagePistol:
                case WeaponHash.CeramicPistol:
                case WeaponHash.MachinePistol:
                    return new string[2] { "weapons@pistol@pistol_str", "reload_aim" };
                case WeaponHash.AssaultRifle:
                case WeaponHash.AssaultrifleMk2:
                    return new string[2] { "weapons@rifle@hi@assault_rifle_str", "reload_aim" };
                case WeaponHash.SniperRifle:
                    return new string[2] { "weapons@rifle@hi@sniper_rifle_str", "reload_aim" };
                case WeaponHash.HeavySniper:
                case WeaponHash.HeavySniperMk2:
                    return new string[2] { "weapons@rifle@lo@sniper_heavy_str", "reload_aim" };
                case WeaponHash.PumpShotgun:
                case WeaponHash.PumpShotgunMk2:
                    return new string[2] { "weapons@rifle@pump_str", "reload_aim" };
                case WeaponHash.Railgun:
                    return new string[2] { "weapons@rifle@lo@rail_gun_str", "reload_aim" };
                case WeaponHash.SawnOffShotgun:
                    return new string[2] { "weapons@rifle@lo@sawnoff_str", "reload_aim" };
                case WeaponHash.AssaultShotgun:
                    return new string[2] { "weapons@rifle@lo@shotgun_assault_str", "reload_aim" };
                case WeaponHash.BullpupShotgun:
                    return new string[2] { "weapons@rifle@lo@shotgun_bullpup_str", "reload_aim" };
                case WeaponHash.AdvancedRifle:
                    return new string[2] { "weapons@submg@advanced_rifle_str", "reload_aim" };
                case WeaponHash.CarbineRifle:
                case WeaponHash.CarbineRifleMk2:
                case WeaponHash.CompactRifle:
                    return new string[2] { "weapons@rifle@lo@carbine_str", "reload_aim" };
                case WeaponHash.Gusenberg:
                    return new string[2] { "anim@weapons@machinegun@gusenberg_str", "reload_aim" };
                case WeaponHash.Musket:
                    return new string[2] { "anim@weapons@musket@musket_str", "reload_aim" };
                case WeaponHash.FlareGun:
                    return new string[2] { "anim@weapons@pistol@flare_str", "reload_aim" };
                case WeaponHash.SpecialCarbine:
                case WeaponHash.SpecialCarbineMk2:
                    return new string[2] { "anim@weapons@rifle@lo@spcarbine_str", "reload_aim" };
                case WeaponHash.CombatPDW:
                    return new string[2] { "anim@weapons@rifle@lo@pdw_str", "reload_aim" };
                case WeaponHash.BullpupRifle:
                case WeaponHash.BullpupRifleMk2:
                    return new string[2] { "anim@weapons@submg@bullpup_rifle_str", "reload_aim" };
                case WeaponHash.AssaultSMG:
                    return new string[2] { "weapons@submg@assault_smg_str", "reload_aim" };
                case WeaponHash.MicroSMG:
                case WeaponHash.MiniSMG:
                    return new string[2] { "weapons@submg@lo@micro_smg_str", "reload_aim" };
                case WeaponHash.SMG:
                case WeaponHash.SMGMk2:
                    return new string[2] { "weapons@rifle@smg_str", "reload_aim" };
                case WeaponHash.GrenadeLauncher:
                case WeaponHash.GrenadeLauncherSmoke:
                case WeaponHash.CompactGrenadeLauncher:
                    return new string[2] { "weapons@heavy@grenade_launcher_str", "reload_aim" };
                case WeaponHash.RPG:
                case WeaponHash.Firework:
                    return new string[2] { "weapons@heavy@rpg_str", "reload_aim" };
                case WeaponHash.CombatMG:
                case WeaponHash.CombatMGMk2:
                    return new string[2] { "weapons@machinegun@combat_mg_str", "reload_aim" };
                case WeaponHash.MG:
                    return new string[2] { "weapons@machinegun@mg_str", "reload_aim" };
                default:
                    Main.Logger.Warning($"~r~Reloading failed! Weapon ~g~[{ped.Weapons.Current.Hash}]~r~ could not be found!");
                    return null;
            }
        }

        public static VehicleSeat GetNearestSeat(this Ped ped, Vehicle veh, float distanceToignoreDoors = 50f)
        {
            float num = 99f;
            int result = -2;
            Dictionary<string, int> dictionary = new Dictionary<string, int>();
            dictionary.Add("door_dside_f", -1);
            dictionary.Add("door_pside_f", 0);
            dictionary.Add("door_dside_r", 1);
            dictionary.Add("door_pside_r", 2);
            foreach (string text in dictionary.Keys)
            {
                bool flag = veh.Bones[text].Position != Vector3.Zero;
                if (flag)
                {
                    float num2 = ped.Position.DistanceTo(Function.Call<Vector3>(Hash.GET_WORLD_POSITION_OF_ENTITY_BONE, new InputArgument[]
                    {
                        veh,
                        veh.Bones[text].Index
                    }));
                    bool flag2 = (num2 < distanceToignoreDoors) && (num2 < num) && IsSeatUsableByPed(ped, veh, dictionary[text]);
                    if (flag2)
                    {
                        num = num2;
                        result = dictionary[text];
                    }
                }
            }
            return (VehicleSeat)result;
        }
        public static bool IsSeatUsableByPed(Ped ped, Vehicle veh, int _seat)
        {
            VehicleSeat seat = (VehicleSeat)_seat;
            bool result = false;
            bool flag = veh.IsSeatFree(seat);
            if (flag)
            {
                result = true;
            }
            else if (veh.GetPedOnSeat(seat) != null)
            {

                bool isDead = veh.GetPedOnSeat(seat).IsDead;
                if (isDead)
                {
                    result = true;
                }
                else
                {
                    int num = Function.Call<int>(Hash.GET_RELATIONSHIP_BETWEEN_PEDS, new InputArgument[]
                    {
                        ped,
                        veh.GetPedOnSeat(seat)
                    });
                    bool flag2 = num > 2;
                    if (flag2)
                    {
                        result = true;
                    }
                }

            }
            return result;
        }
        public static bool IsTaskActive(this Ped p, TaskType task)
        {
            return Function.Call<bool>(Hash.GET_IS_TASK_ACTIVE, p.Handle, task);
        }
        public static Vector3 GetAimCoord(this Ped p)
        {
            var weapon = p.Weapons.CurrentWeaponObject;

            var v = p.CurrentVehicle;
            // Rhino
            if (v != null && v.Model.Hash == 782665360)
            {
                return v.Bones[35].Position + v.Bones[35].ForwardVector * 100;
            }
            if (p.IsOnTurretSeat()) { return p.GetLookingCoord(); }
            if (weapon != null)
            {
                // Not very accurate, but doesn't matter
                Vector3 dir = weapon.RightVector;
                return weapon.Position + dir * 20;

            }
            return GetLookingCoord(p);
        }
        public static Vector3 GetLookingCoord(this Ped p)
        {
            if (p == Main.P && Function.Call<int>(Hash.GET_FOLLOW_PED_CAM_VIEW_MODE) == 4)
            {
                return RaycastEverything(default);
            }
            EntityBone b = p.Bones[Bone.FacialForehead];
            Vector3 v = b.UpVector.Normalized;
            return b.Position + 200 * v;
        }
        public static VehicleSeat GetSeatTryingToEnter(this Ped p)
        {
            return (VehicleSeat)Function.Call<int>(Hash.GET_SEAT_PED_IS_TRYING_TO_ENTER, p);
        }


        #endregion







        public static Vector3 RaycastEverything(Vector2 screenCoord)
        {
            Vector3 camPos = GameplayCamera.Position;
            Vector3 camRot = GameplayCamera.Rotation;
            const float raycastToDist = 100.0f;
            const float raycastFromDist = 1f;

            Vector3 target3D = ScreenRelToWorld(camPos, camRot, screenCoord);
            Vector3 source3D = camPos;

            Entity ignoreEntity = Game.Player.Character;
            if (Game.Player.Character.IsInVehicle())
            {
                ignoreEntity = Game.Player.Character.CurrentVehicle;
            }

            Vector3 dir = target3D - source3D;
            dir.Normalize();
            RaycastResult raycastResults = World.Raycast(source3D + dir * raycastFromDist,
                source3D + dir * raycastToDist,
                IntersectFlags.Everything,
                ignoreEntity);

            if (raycastResults.DidHit)
            {
                return raycastResults.HitPosition;
            }

            return camPos + dir * raycastToDist;
        }
        public static Vector3 ScreenRelToWorld(Vector3 camPos, Vector3 camRot, Vector2 coord)
        {
            Vector3 camForward = camRot.ToDirection();
            Vector3 rotUp = camRot + new Vector3(10, 0, 0);
            Vector3 rotDown = camRot + new Vector3(-10, 0, 0);
            Vector3 rotLeft = camRot + new Vector3(0, 0, -10);
            Vector3 rotRight = camRot + new Vector3(0, 0, 10);

            Vector3 camRight = rotRight.ToDirection() - rotLeft.ToDirection();
            Vector3 camUp = rotUp.ToDirection() - rotDown.ToDirection();

            double rollRad = -camRot.Y.ToRadians();

            Vector3 camRightRoll = camRight * (float)Math.Cos(rollRad) - camUp * (float)Math.Sin(rollRad);
            Vector3 camUpRoll = camRight * (float)Math.Sin(rollRad) + camUp * (float)Math.Cos(rollRad);

            Vector3 point3D = camPos + camForward * 10.0f + camRightRoll + camUpRoll;
            if (!WorldToScreenRel(point3D, out Vector2 point2D))
            {
                return camPos + camForward * 10.0f;
            }

            Vector3 point3DZero = camPos + camForward * 10.0f;
            if (!WorldToScreenRel(point3DZero, out Vector2 point2DZero))
            {
                return camPos + camForward * 10.0f;
            }

            const double eps = 0.001;
            if (Math.Abs(point2D.X - point2DZero.X) < eps || Math.Abs(point2D.Y - point2DZero.Y) < eps)
            {
                return camPos + camForward * 10.0f;
            }

            float scaleX = (coord.X - point2DZero.X) / (point2D.X - point2DZero.X);
            float scaleY = (coord.Y - point2DZero.Y) / (point2D.Y - point2DZero.Y);

            return camPos + camForward * 10.0f + camRightRoll * scaleX + camUpRoll * scaleY;
        }
        public static bool WorldToScreenRel(Vector3 worldCoords, out Vector2 screenCoords)
        {
            OutputArgument num1 = new OutputArgument();
            OutputArgument num2 = new OutputArgument();

            if (!Function.Call<bool>(Hash.GET_SCREEN_COORD_FROM_WORLD_COORD, worldCoords.X, worldCoords.Y, worldCoords.Z, num1, num2))
            {
                screenCoords = new Vector2();
                return false;
            }

            screenCoords = new Vector2((num1.GetResult<float>() - 0.5f) * 2, (num2.GetResult<float>() - 0.5f) * 2);
            return true;
        }
        public static void StayInCover(this Ped p)
        {
            Function.Call(Hash.TASK_STAY_IN_COVER, p);
        }

        public static bool IsTurretSeat(this Vehicle veh, int seat)
        {
            if (!Function.Call<bool>(Hash.DOES_VEHICLE_HAVE_WEAPONS, veh.Handle))
            {
                return false;
            }

            switch (seat)
            {
                case -1:
                    return (VehicleHash)veh.Model.Hash == VehicleHash.Rhino
                        || (VehicleHash)veh.Model.Hash == VehicleHash.Khanjari
                        || (VehicleHash)veh.Model.Hash == VehicleHash.FireTruck
                        || (VehicleHash)veh.Model.Hash == VehicleHash.Riot2
                        || (VehicleHash)veh.Model.Hash == VehicleHash.Cerberus
                        || (VehicleHash)veh.Model.Hash == VehicleHash.Cerberus2
                        || (VehicleHash)veh.Model.Hash == VehicleHash.Cerberus3;
                case 0:
                    return (VehicleHash)veh.Model.Hash == VehicleHash.Apc
                        || (VehicleHash)veh.Model.Hash == VehicleHash.Dune3;
                case 1:
                    return (VehicleHash)veh.Model.Hash == VehicleHash.Valkyrie
                        || (VehicleHash)veh.Model.Hash == VehicleHash.Valkyrie2
                        || (VehicleHash)veh.Model.Hash == VehicleHash.Technical
                        || (VehicleHash)veh.Model.Hash == VehicleHash.Technical2
                        || (VehicleHash)veh.Model.Hash == VehicleHash.Technical3
                        || (VehicleHash)veh.Model.Hash == VehicleHash.HalfTrack
                        || (VehicleHash)veh.Model.Hash == VehicleHash.Barrage;
                case 2:
                    return (VehicleHash)veh.Model.Hash == VehicleHash.Valkyrie
                        || (VehicleHash)veh.Model.Hash == VehicleHash.Valkyrie2
                        || (VehicleHash)veh.Model.Hash == VehicleHash.Barrage;
                case 3:
                    return (VehicleHash)veh.Model.Hash == VehicleHash.Limo2
                        || (VehicleHash)veh.Model.Hash == VehicleHash.Dinghy5;
                case 7:
                    return (VehicleHash)veh.Model.Hash == VehicleHash.Insurgent;
            }

            return false;
        }
        public static bool IsOnTurretSeat(this Ped P)
        {
            if (P.CurrentVehicle == null) { return false; }
            return IsTurretSeat(P.CurrentVehicle, (int)P.SeatIndex);
        }




    }
}
