﻿using System.Collections.Generic;
using System.IO;
using GTA;
using GTA.Math;
using GTA.Native;
using Newtonsoft.Json;
using RageCoop.Client.Scripting;
using RageCoop.Core;
using static RageCoop.Client.Shared;

namespace RageCoop.Client
{
    internal class WeaponFix
    {
        public Dictionary<uint, string> Bullet = new Dictionary<uint, string>();
        public Dictionary<uint, string> Lazer = new Dictionary<uint, string>();
        public Dictionary<uint, string> Others = new Dictionary<uint, string>();
    }

    internal static class WeaponUtil
    {
        public static Dictionary<uint, VehicleWeaponInfo> VehicleWeapons = new Dictionary<uint, VehicleWeaponInfo>();
        public static WeaponFix WeaponFix;
        public static Dictionary<uint, WeaponInfo> Weapons;

        static WeaponUtil()
        {
            // Parse and load to memory
            foreach (var w in JsonConvert.DeserializeObject<VehicleWeaponInfo[]>(
                         File.ReadAllText(VehicleWeaponDataPath))) VehicleWeapons.Add(w.Hash, w);

            Weapons = JsonConvert.DeserializeObject<Dictionary<uint, WeaponInfo>>(
                File.ReadAllText(WeaponInfoDataPath));

            if (File.Exists(WeaponFixDataPath))
                WeaponFix = JsonConvert.DeserializeObject<WeaponFix>(File.ReadAllText(WeaponFixDataPath));
            else
                API.Logger.Warning("Weapon fix data not found");
        }

        public static void DumpWeaponFix(string path)
        {
            var P = Game.Player.Character;
            var pos = P.Position + Vector3.WorldUp * 3;
            var types = new HashSet<int> { 3 };
            P.IsInvincible = true;
            var fix = new WeaponFix();
            foreach (var w in Weapons)
            {
                Console.Info("Testing " + w.Value.Name);
                if (w.Value.FireType != "PROJECTILE")
                {
                    var asset = new WeaponAsset(w.Key);
                    asset.Request(1000);
                    World.ShootBullet(pos, pos + Vector3.WorldUp, P, asset, 0, 1000);
                    if (!Function.Call<bool>(Hash.IS_BULLET_IN_AREA, pos.X, pos.Y, pos.Z, 10f, true) &&
                        !Function.Call<bool>(Hash.IS_PROJECTILE_IN_AREA, pos.X - 10, pos.Y - 10, pos.Z - 10, pos.X + 10,
                            pos.Y + 10, pos.Z + 10, true))
                        switch (w.Value.DamageType)
                        {
                            case "BULLET":
                                fix.Bullet.Add(w.Key, w.Value.Name);
                                break;
                            case "EXPLOSIVE":
                                fix.Lazer.Add(w.Key, w.Value.Name);
                                break;
                            default:
                                fix.Others.Add(w.Key, w.Value.Name);
                                break;
                        }

                    foreach (var p in World.GetAllProjectiles()) p.Delete();
                    Script.Wait(50);
                }
            }

            File.WriteAllText(path, JsonConvert.SerializeObject(fix, Formatting.Indented));

            P.IsInvincible = false;
        }

        public static uint GetWeaponFix(uint hash)
        {
            if (WeaponFix.Bullet.TryGetValue(hash, out _)) return 0x461DDDB0;
            if (WeaponFix.Lazer.TryGetValue(hash, out _)) return 0xE2822A29;

            return hash;
        }


        public static Dictionary<uint, bool> GetWeaponComponents(this Weapon weapon)
        {
            Dictionary<uint, bool> result = null;

            if (weapon.Components.Count > 0)
            {
                result = new Dictionary<uint, bool>();

                foreach (var comp in weapon.Components) result.Add((uint)comp.ComponentHash, comp.Active);
            }

            return result;
        }

        public static Vector3 GetMuzzlePosition(this Ped p)
        {
            if (p.IsOnTurretSeat()) return p.CurrentVehicle.GetMuzzleBone(p.VehicleWeapon).Position;
            var wb = p.Weapons?.CurrentWeaponObject?.Bones["gun_muzzle"];
            if (wb?.IsValid == true) return wb.Position;
            return p.Bones[Bone.SkelRightHand].Position;
        }

        public static float GetWeaponDamage(this Ped P, uint hash)
        {
            var comp = P.Weapons.Current.Components.GetSuppressorComponent();
            return Function.Call<float>(Hash.GET_WEAPON_DAMAGE, hash,
                comp.Active ? comp.ComponentHash : WeaponComponentHash.Invalid);
        }

        public static int GetMuzzleIndex(this Vehicle v, VehicleWeaponHash hash)
        {
            if (VehicleWeapons.TryGetValue((uint)v.Model.Hash, out var veh) &&
                veh.Weapons.TryGetValue((uint)hash, out var wp))
                return (int)wp.Bones[CoreUtils.RandInt(0, wp.Bones.Length)].BoneIndex;
            return -1;
        }

        public static EntityBone GetMuzzleBone(this Vehicle v, VehicleWeaponHash hash)
        {
            if ((uint)hash == 1422046295) hash = VehicleWeaponHash.WaterCannon;
            var i = v.GetMuzzleIndex(hash);
            if (i == -1) return null;
            return v.Bones[i];
        }

        public static bool IsUsingProjectileWeapon(this Ped p)
        {
            var vp = p.VehicleWeapon;
            return Weapons.TryGetValue(vp != VehicleWeaponHash.Invalid ? (uint)vp : (uint)p.Weapons.Current.Hash,
                       out var info)
                   && info.FireType == "PROJECTILE";
        }

        public static string GetFlashFX(this WeaponHash w, bool veh)
        {
            if (veh)
                switch ((VehicleWeaponHash)w)
                {
                    case VehicleWeaponHash.Tank:
                        return "muz_tank";
                    default: return "muz_buzzard";
                }

            switch (w.GetWeaponGroup())
            {
                case WeaponGroup.SMG:
                    return "muz_smg";

                case WeaponGroup.Shotgun:
                    return "muz_smg";

                case WeaponGroup.AssaultRifle:
                    return "muz_assault_rifle";

                case WeaponGroup.Pistol:
                    return "muz_pistol";

                case WeaponGroup.Stungun:
                    return "muz_stungun";

                case WeaponGroup.Heavy:
                    switch (w)
                    {
                        case WeaponHash.Minigun:
                            return "muz_minigun";

                        case WeaponHash.RPG:
                            return "muz_rpg";

                        default:
                            return "muz_minigun";
                    }
                case WeaponGroup.Sniper:
                    return "muz_alternate_star";

                case WeaponGroup.PetrolCan:
                    return "weap_petrol_can";

                case WeaponGroup.FireExtinguisher:
                    return "weap_extinguisher";
                default:
                    return "muz_assault_rifle";
            }
        }

        public static WeaponGroup GetWeaponGroup(this WeaponHash hash)
        {
            return Function.Call<WeaponGroup>(Hash.GET_WEAPONTYPE_GROUP, hash);
        }
    }
}