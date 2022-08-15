using GTA;
using GTA.Math;
using GTA.Native;
using System.Collections.Generic;
using System.Xml;

namespace RageCoop.Client
{
    internal class MuzzleInfo
    {
        public MuzzleInfo(Vector3 pos, Vector3 forward)
        {
            Position = pos;
            ForawardVector=forward;
        }
        public Vector3 Position;
        public Vector3 ForawardVector;
    }
    internal static class WeaponUtil
    {
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
            var w = p.Weapons.CurrentWeaponObject;
            if (w!=null)
            {
                var hash = p.Weapons.Current.Hash;
                if (MuzzleBoneIndexes.ContainsKey(hash)) { return w.Bones[MuzzleBoneIndexes[hash]].Position; }
                return w.Position;
            }
            return p.Bones[Bone.SkelRightHand].Position;
        }
        static long BulletsShot = 0;

        public static float GetWeaponDamage(this Ped P, uint hash)
        {
            var comp = P.Weapons.Current.Components.GetSuppressorComponent();
            return Function.Call<float>(Hash.GET_WEAPON_DAMAGE, hash, comp.Active ? comp.ComponentHash : WeaponComponentHash.Invalid);

            /*
            if (P.IsInVehicle() && (hash!=(uint)P.Weapons.Current.Hash))
            {
                // This is a vehicle weapon
                P.VehicleWeapon=(VehicleWeaponHash)hash;
                return 100;
            }
            switch (P.Weapons.Current.Group)
            {
                case WeaponGroup.Pistol: return 30;
                case WeaponGroup.AssaultRifle: return 30;
                case WeaponGroup.SMG: return 20;
                case WeaponGroup.MG: return 40;
                case WeaponGroup.Shotgun: return 30;
                case WeaponGroup.Sniper: return 200;
                case WeaponGroup.Heavy: return 30;
            }
            return 0;
            */
        }

        public static int GetMuzzleIndex(this Vehicle v)
        {
            BulletsShot++;
            switch (v.Model.Hash)
            {
                // BRUISER3
                case -2042350822:
                    return BulletsShot%2==0 ? 52 : 50;

                // BRUTUS3
                case 2038858402:
                    return 84;

                // MONSTER5
                case -715746948:
                    return BulletsShot%2==0 ? 63 : 65;

                // JB7002
                case 394110044:
                    return BulletsShot%2==0 ? 54 : 53;

                // DOMINATOR5
                case -1375060657:
                    return BulletsShot%2==0 ? 35 : 36;

                // IMPALER3
                case -1924800695:
                    return BulletsShot%2==0 ? 75 : 76;


                // IMPERATOR2
                case 1637620610:
                    return BulletsShot%2==0 ? 97 : 99;


                // SLAMVAN5
                case 373261600:
                    return BulletsShot%2==0 ? 51 : 53;


                // RUINER2
                case 941494461:
                    return BulletsShot%2==0 ? 65 : 66;


                // TAMPA3
                case -1210451983:
                    return 87;

                // SCRAMJET
                case -638562243:
                    return BulletsShot%2==0 ? 44 : 45;


                // VIGILANTE
                case -1242608589:
                    return BulletsShot%2==0 ? 42 : 43;


                // ZR380
                case 540101442:
                    return BulletsShot%2==0 ? 57 : 63;


                // ZR3802
                case -1106120762:
                    return BulletsShot%2==0 ? 57 : 63;


                // ZR3803
                case -1478704292:
                    return BulletsShot%2==0 ? 53 : 59;


                // STROMBERG
                case 886810209:
                    return BulletsShot%2==0 ? 85 : 84;


                // SLAMVAN4
                case -2061049099:
                    return BulletsShot%2==0 ? 76 : 78;


                // IMPERATOR
                case 444994115:
                    return BulletsShot%2==0 ? 88 : 86;


                // IMPALER2
                case 1009171724:
                    return BulletsShot%2==0 ? 63 : 64;


                // DOMINATOR4
                case -688189648:
                    return BulletsShot%2==0 ? 59 : 60;


                // SAVAGE
                case -82626025:
                    return 30;

                // BUZZARD
                case 788747387:
                    return BulletsShot%2==0 ? 28 : 23;


                // ANNIHL
                case 837858166:
                    return (int)BulletsShot%4+35;


                // HYDRA
                case 970385471:
                    return BulletsShot%2==0 ? 29 : 28;


                // STARLING
                case -1700874274:
                    return BulletsShot%2==0 ? 24 : 12;


                // RHINO
                case 782665360:
                    return 30;

                default:
                    return -1;
            }
        }
        public static int GetMuzzleTurret(this Vehicle v)
        {
            switch (v.Model.Hash)
            {
                default:
                    return 0;
            }

        }
        public static bool IsUsingProjectileWeapon(this Ped p)
        {
            var vp = p.VehicleWeapon;
            var type = Function.Call<int>(Hash.GET_WEAPON_DAMAGE_TYPE, vp);
            if (vp!=VehicleWeaponHash.Invalid)
            {
                if (type==3)
                {
                    return false;
                }
                return VehicleProjectileWeapons.Contains(vp) || (type==5 && !ExplosiveBullets.Contains((uint)vp));
            }
            else
            {
                var w = p.Weapons.Current;
                return w.Group==WeaponGroup.Thrown || ProjectileWeapons.Contains(w.Hash);
            }
        }

        public static readonly HashSet<uint> ExplosiveBullets = new HashSet<uint>
        {
            (uint)VehicleWeaponHash.PlayerLazer,
            (uint)WeaponHash.Railgun,
            1638077257
        };

        public static readonly Dictionary<WeaponHash, int> MuzzleBoneIndexes = new Dictionary<WeaponHash, int>
        {
            {WeaponHash.HeavySniper,6},
            {WeaponHash.MarksmanRifle,9},
            {WeaponHash.SniperRifle,9},
            {WeaponHash.AdvancedRifle,5},
            {WeaponHash.SpecialCarbine,9},
            {WeaponHash.BullpupRifle,7},
            {WeaponHash.AssaultRifle,9},
            {WeaponHash.CarbineRifle,6},
            {WeaponHash.MachinePistol,5},
            {WeaponHash.SMG,5},
            {WeaponHash.AssaultSMG,6},
            {WeaponHash.CombatPDW,5},
            {WeaponHash.MG,6},
            {WeaponHash.CombatMG,7},
            {WeaponHash.Gusenberg,7},
            {WeaponHash.MicroSMG,10},
            {WeaponHash.APPistol,8},
            {WeaponHash.StunGun,4},
            {WeaponHash.Pistol,8},
            {WeaponHash.CombatPistol,8},
            {WeaponHash.Pistol50,7},
            {WeaponHash.SNSPistol,8},
            {WeaponHash.HeavyPistol,8},
            {WeaponHash.VintagePistol,8},
            {WeaponHash.Railgun,9},
            {WeaponHash.Minigun,5},
            {WeaponHash.Musket,3},
            {WeaponHash.HeavyShotgun,10},
            {WeaponHash.PumpShotgun,11},
            {WeaponHash.SawnOffShotgun,8},
            {WeaponHash.BullpupShotgun,8},
            {WeaponHash.AssaultShotgun,9},
            {WeaponHash.HeavySniperMk2,11},
            {WeaponHash.MarksmanRifleMk2,9},
            {WeaponHash.CarbineRifleMk2,13},
            {WeaponHash.SpecialCarbineMk2,16},
            {WeaponHash.BullpupRifleMk2,8},
            {WeaponHash.CompactRifle,7},
            {WeaponHash.MilitaryRifle,11},
            {WeaponHash.AssaultrifleMk2,17},
            {WeaponHash.MiniSMG,5},
            {WeaponHash.SMGMk2,6},
            {WeaponHash.CombatMGMk2,16},
            {WeaponHash.UnholyHellbringer,4},
            {WeaponHash.PistolMk2,12},
            {WeaponHash.SNSPistolMk2,15},
            {WeaponHash.CeramicPistol,10},
            {WeaponHash.MarksmanPistol,4},
            {WeaponHash.Revolver,7},
            {WeaponHash.RevolverMk2,7},
            {WeaponHash.DoubleActionRevolver,7},
            {WeaponHash.NavyRevolver,7},
            {WeaponHash.PericoPistol,4},
            {WeaponHash.FlareGun,4},
            {WeaponHash.UpNAtomizer,4},
            {WeaponHash.HomingLauncher,5},
            {WeaponHash.CompactGrenadeLauncher,8},
            {WeaponHash.Widowmaker,6},
            {WeaponHash.GrenadeLauncher,3},
            {WeaponHash.RPG,9},
            {WeaponHash.DoubleBarrelShotgun,8},
            {WeaponHash.SweeperShotgun,7},
            {WeaponHash.CombatShotgun,7},
            {WeaponHash.PumpShotgunMk2,7},

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
        public static string GetFlashFX(this WeaponHash w)
        {
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
            }
            switch ((VehicleWeaponHash)w)
            {
                case VehicleWeaponHash.Tank:
                    return "muz_tank";

                case VehicleWeaponHash.PlayerBuzzard:
                    return "muz_buzzard";
            }
            return "muz_assault_rifle";
        }
        public static WeaponGroup GetWeaponGroup(this WeaponHash hash)
        {
            return Function.Call<WeaponGroup>(Hash.GET_WEAPONTYPE_GROUP, hash);
        }
    }
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
}
