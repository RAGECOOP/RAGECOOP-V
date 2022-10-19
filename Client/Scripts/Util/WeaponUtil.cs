using GTA;
using GTA.Math;
using GTA.Native;
using Newtonsoft.Json;
using RageCoop.Core;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System;
using Console = GTA.Console;
using System.Drawing;
using System.Runtime.InteropServices;

namespace RageCoop.Client
{
    #region DUMP

    class VehicleInfo
    {
        public string Name;
        public string[] Weapons;
        public uint Hash;
        public VehicleBone[] Bones;
    }
    class VehicleBone
    {
        public uint BoneID;
        public uint BoneIndex;
        public string BoneName;
    }
    class WeaponBones
    {
        public string Name;
        public VehicleBone[] Bones;
    }
    class VehicleWeaponInfo
    {
        public static void Dump(string input, string output)
        {
            Console.Info("Generating " + output);
            if (!File.Exists(input))
            {
                Console.Info("Downloading");
                HttpHelper.DownloadFile("https://raw.githubusercontent.com/DurtyFree/gta-v-data-dumps/master/vehicles.json", input);
            }
            Console.Info("Deserialising");
            var infos = JsonConvert.DeserializeObject<VehicleInfo[]>(File.ReadAllText(input));
            Console.Info("Serialising");
            File.WriteAllText(output,
                JsonConvert.SerializeObject(
                    infos.Select(x => FromVehicle(x)).Where(x => x != null),
                    Formatting.Indented));
        }
        public static VehicleWeaponInfo FromVehicle(VehicleInfo info)
        {
            if (info.Weapons.Length == 0)
            {
                return null;
            }
            var result = new VehicleWeaponInfo() { Hash = info.Hash, Name = info.Name };
            for (int i = 0; i < info.Weapons.Length; i++)
            {
                result.Weapons.Add((uint)Game.GenerateHash(info.Weapons[i])
                    , new WeaponBones
                    {
                        Name = info.Weapons[i],
                        Bones = info.Bones.Where(x => x.BoneName.StartsWith($"weapon_{i + 1}") && !x.BoneName.EndsWith("rot")).ToArray()
                    });

            }
            return result;
        }
        public uint Hash;
        public string Name;
        public Dictionary<uint, WeaponBones> Weapons = new Dictionary<uint, WeaponBones>();
    }
    class WeaponInfo
    {
        public string Name;
        public uint Hash;
    }

    [StructLayout(LayoutKind.Explicit, Size = 312)]
    public struct DlcWeaponData
    {
    }
    class WeaponFix
    {
        public Dictionary<uint,string> Bullet=new Dictionary<uint, string>();
        public Dictionary<uint, string> Lazer = new Dictionary<uint, string>();
    }
    #endregion

    internal static class WeaponUtil
    {
        public static Dictionary<uint, VehicleWeaponInfo> VehicleWeapons = new Dictionary<uint, VehicleWeaponInfo>();
        public static WeaponFix WeaponFix;
        public const string VehicleWeaponLocation= @"RageCoop\Data\VehicleWeapons.json";
        public const string WeaponFixLocation = @"RageCoop\Data\WeaponFixes.json";
        static WeaponUtil()
        {
            if (!File.Exists(VehicleWeaponLocation))
            {
                Directory.CreateDirectory(@"RageCoop\Data\tmp");
                var input = @"RageCoop\Data\tmp\vehicles.json";
                VehicleWeaponInfo.Dump(input, VehicleWeaponLocation);
            }

            // Parse and load to memory
            foreach (var w in JsonConvert.DeserializeObject<VehicleWeaponInfo[]>(File.ReadAllText(VehicleWeaponLocation)))
            {
                VehicleWeapons.Add(w.Hash, w);
            }
            WeaponFix = JsonConvert.DeserializeObject<WeaponFix>(File.ReadAllText(WeaponFixLocation));
        }
        public static void DumpWeaponFix(string path = WeaponFixLocation)
        {
            var P = Game.Player.Character;
            var pos = P.Position + Vector3.WorldUp * 3;
            var types = new HashSet<int>() { 3 };
            P.IsInvincible = true;
            var fix = new WeaponFix();
            foreach (VehicleWeaponHash v in Enum.GetValues(typeof(VehicleWeaponHash)))
            {
                Console.Info("Testing: " + v);
                if (types.Contains(v.GetWeaponDamageType()))
                {
                    var asset = new WeaponAsset((int)v);
                    asset.Request(1000);
                    World.ShootBullet(pos, pos + Vector3.WorldUp, P, asset, 0, 1000);
                    if (!Function.Call<bool>(Hash.IS_BULLET_IN_AREA, pos.X, pos.Y, pos.Z, 10f, true) &&
                        !Function.Call<bool>(Hash.IS_PROJECTILE_IN_AREA, pos.X - 10, pos.Y - 10, pos.Z - 10, pos.X + 10, pos.Y + 10, pos.Z + 10, true))
                    {
                        fix.Bullet.Add((uint)v,$"{nameof(VehicleWeaponHash)}.{v}");
                    }
                    foreach (var p in World.GetAllProjectiles())
                    {
                        p.Delete();
                    }
                    Script.Wait(50);
                }
            }
            foreach (WeaponHash w in Enum.GetValues(typeof(WeaponHash)))
            {
                if (types.Contains(w.GetWeaponDamageType()))
                {
                    Console.Info("Testing: " + w);
                    var asset = new WeaponAsset((int)w);
                    asset.Request(1000);
                    World.ShootBullet(pos, pos + Vector3.WorldUp, P, asset, 0, 1000);
                    if (!Function.Call<bool>(Hash.IS_BULLET_IN_AREA, pos.X, pos.Y, pos.Z, 10f, true) &&
                        !Function.Call<bool>(Hash.IS_PROJECTILE_IN_AREA, pos.X - 10, pos.Y - 10, pos.Z - 10, pos.X + 10, pos.Y + 10, pos.Z + 10, true))
                    {
                        fix.Bullet.Add((uint)w, $"{nameof(WeaponHash)}.{w}");
                    }
                    foreach (var p in World.GetAllProjectiles())
                    {
                        p.Delete();
                    }
                    Script.Wait(50);
                }
            }
            AddLazer(VehicleWeaponHash.PlayerSavage);
            AddLazer(VehicleWeaponHash.StrikeforceCannon);
            void AddLazer(dynamic hash)
            {
                fix.Lazer.Add((uint)hash, $"{hash.GetType().Name}.{hash.ToString()}");
            }
            File.WriteAllText(path, JsonConvert.SerializeObject(fix, Formatting.Indented));

            P.IsInvincible = false;
        }
        public static void DumpWeaponHashes(string path = VehicleWeaponLocation)
        {
            Dictionary<uint, string> hashes = new Dictionary<uint, string>();
            foreach (var wep in JsonConvert.DeserializeObject<WeaponInfo[]>(HttpHelper.DownloadString("https://raw.githubusercontent.com/DurtyFree/gta-v-data-dumps/master/weapons.json")))
            {
                if (!wep.Name.StartsWith("WEAPON")) { continue; }
                hashes.Add(wep.Hash, wep.Name);
            }
            var output = "public enum WeaponHash : uint\r\n{";
            List<string> lines = new List<string>();
            foreach (var hash in hashes)
            {
                lines.Add($"{CoreUtils.FormatToSharpStyle(hash.Value, 7)} = {hash.Key.ToHex()}");
            }
            lines.Sort();
            foreach (var l in lines)
            {
                output += $"\r\n\t{l},";
            }
            output += "\r\n}";
            File.WriteAllText(path, output);

        }

        public static void DumpVehicleWeaponHashes(string path = @"RageCoop\Data\VehicleWeaponHash.cs")
        {
            Dictionary<uint, string> hashes = new Dictionary<uint, string>();
            foreach (var veh in VehicleWeapons.Values)
            {
                foreach (var hash in veh.Weapons)
                {
                    if (!hashes.ContainsKey(hash.Key))
                    {
                        hashes.Add(hash.Key, hash.Value.Name);
                    }
                }
            }
            var output = "public enum VehicleWeaponHash : uint\r\n{\r\n\tInvalid = 0xFFFFFFFF,";
            List<string> lines = new List<string>();
            foreach (var hash in hashes)
            {
                lines.Add($"{CoreUtils.FormatToSharpStyle(hash.Value)} = {hash.Key.ToHex()}");
            }
            lines.Sort();
            foreach (var l in lines)
            {
                output += $"\r\n\t{l},";
            }
            output += "\r\n}";
            File.WriteAllText(path, output);

        }
        public static uint GetWeaponFix(uint hash)
        {
            if(WeaponFix.Bullet.TryGetValue(hash,out var _))
            {
                return (uint)VehicleWeaponHash.SubcarMg;
            }
            if (WeaponFix.Lazer.TryGetValue(hash, out var _))
            {
                return (uint)VehicleWeaponHash.PlayerLazer;
            }
            return hash;
        }
        public static Dictionary<uint, bool> GetWeaponComponents(this Weapon weapon)
        {
            Dictionary<uint, bool> result = null;

            if (weapon.Components.Count > 0)
            {
                result = new Dictionary<uint, bool>();

                foreach (var comp in weapon.Components)
                {
                    result.Add((uint)comp.ComponentHash, comp.Active);
                }
            }

            return result;
        }

        public static Vector3 GetMuzzlePosition(this Ped p)
        {
            if (p.IsOnTurretSeat())
            {
                return p.CurrentVehicle.GetMuzzleBone(p.VehicleWeapon).Position;
            }
            var wb = p.Weapons?.CurrentWeaponObject?.Bones["gun_muzzle"];
            if (wb?.IsValid == true)
            {
                return wb.Position;
            }
            return p.Bones[Bone.SkelRightHand].Position;
        }

        public static float GetWeaponDamage(this Ped P, uint hash)
        {
            var comp = P.Weapons.Current.Components.GetSuppressorComponent();
            return Function.Call<float>(Hash.GET_WEAPON_DAMAGE, hash, comp.Active ? comp.ComponentHash : WeaponComponentHash.Invalid);
        }

        public static int GetMuzzleIndex(this Vehicle v, VehicleWeaponHash hash)
        {
            if (VehicleWeapons.TryGetValue((uint)v.Model.Hash, out var veh) && veh.Weapons.TryGetValue((uint)hash, out var wp))
            {
                return (int)wp.Bones[CoreUtils.RandInt(0, wp.Bones.Length)].BoneIndex;
            }
            return -1;
        }
        public static EntityBone GetMuzzleBone(this Vehicle v, VehicleWeaponHash hash)
        {
            var i = v.GetMuzzleIndex(hash);
            if (i == -1) { return null; }
            return v.Bones[i];
        }
        public static bool IsUsingProjectileWeapon(this Ped p)
        {
            var vp = p.VehicleWeapon;
            var type = Function.Call<int>(Hash.GET_WEAPON_DAMAGE_TYPE, vp);
            if (vp != VehicleWeaponHash.Invalid)
            {
                return type == 3 ? false : VehicleProjectileWeapons.Contains(vp) || (type == 5 && !ExplosiveBullets.Contains((uint)vp));
            }

            var w = p.Weapons.Current;
            return w.Group == WeaponGroup.Thrown || ProjectileWeapons.Contains(w.Hash);
        }
        public static int GetWeaponDamageType<T>(this T hash) where T : Enum
        {
            return Function.Call<int>(Hash.GET_WEAPON_DAMAGE_TYPE, hash);
        }
        public static readonly HashSet<uint> ExplosiveBullets = new HashSet<uint>
        {
            (uint)VehicleWeaponHash.PlayerLazer,
            (uint)WeaponHash.Railgun,
            1638077257
        };
        public static readonly HashSet<WeaponHash> ProjectileWeapons = new HashSet<WeaponHash> {
            WeaponHash.HomingLauncher,
            WeaponHash.RPG,
            WeaponHash.Firework,
            WeaponHash.UpNAtomizer,
            WeaponHash.GrenadeLauncher,
            WeaponHash.GrenadeLauncherSmoke,
            WeaponHash.CompactGrenadeLauncher,
            WeaponHash.FlareGun,
        };
        public static readonly HashSet<VehicleWeaponHash> VehicleProjectileWeapons = new HashSet<VehicleWeaponHash> {
            VehicleWeaponHash.PlaneRocket,
            VehicleWeaponHash.SpaceRocket,
            VehicleWeaponHash.Tank,
            (VehicleWeaponHash)3565779982, // STROMBERG missiles
            (VehicleWeaponHash)3169388763, // SCRAMJET missiles
        };
        public static string GetFlashFX(this WeaponHash w,bool veh)
        {
            if (veh)
            {

                switch ((VehicleWeaponHash)w)
                {
                    case VehicleWeaponHash.Tank:
                        return "muz_tank";
                    default: return "muz_buzzard";
                }
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
    /*
    class WeaponInfo
    {
        public string Name;
        public string MuzzleFx;
    }
    public class AimingInfo
    {
        public string Name;
        public float HeadingLimit;
        public float SweepPitchMin;
        public float SweepPitchMax;
    }
    */
}
