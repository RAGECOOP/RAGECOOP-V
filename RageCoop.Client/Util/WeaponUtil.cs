using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA;
using GTA.Math;

namespace RageCoop.Client
{
    internal class MuzzleInfo
    {
        public MuzzleInfo(Vector3 pos,Vector3 forward)
        {
            Position = pos;
            ForawardVector=forward;
        }
        public Vector3 Position;
        public Vector3 ForawardVector;
    }
    internal static class WeaponUtil
    {
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
        static long BulletsShot=0;
        public static MuzzleInfo GetMuzzleInfo(this Vehicle v)
        {
            BulletsShot++;
            int i;
            switch (v.Model.Hash)
            {
                // SCRAMJET
                case -638562243:
                    i=BulletsShot%2==0 ? 44 : 45;
                    return new MuzzleInfo(v.Bones[i].Position, v.Bones[i].ForwardVector);

                // VIGILANTE
                case -1242608589:
                    i=BulletsShot%2==0 ? 42 : 43;
                    return new MuzzleInfo(v.Bones[i].Position, v.Bones[i].ForwardVector);

                // ZR380
                case 540101442:
                    i=BulletsShot%2==0 ? 57 : 63;
                    return new MuzzleInfo(v.Bones[i].Position, v.Bones[i].ForwardVector);

                // ZR3802
                case -1106120762:
                    i=BulletsShot%2==0 ? 57 : 63;
                    return new MuzzleInfo(v.Bones[i].Position, v.Bones[i].ForwardVector);

                // ZR3803
                case -1478704292:
                    i=BulletsShot%2==0 ? 53 : 59;
                    return new MuzzleInfo(v.Bones[i].Position, v.Bones[i].ForwardVector);

                // STROMBERG
                case 886810209:
                    i=BulletsShot%2==0 ? 85 : 84;
                    return new MuzzleInfo(v.Bones[i].Position, v.Bones[i].ForwardVector);

                // SLAMVAN4
                case -2061049099:
                    i=BulletsShot%2==0 ? 76 : 78;
                    return new MuzzleInfo(v.Bones[i].Position, v.Bones[i].ForwardVector);

                // IMPERATOR
                case 444994115:
                    i=BulletsShot%2==0 ? 88 : 86;
                    return new MuzzleInfo(v.Bones[i].Position, v.Bones[i].ForwardVector);

                // IMPALER2
                case 1009171724:
                    i=BulletsShot%2==0 ? 63 : 64;
                    return new MuzzleInfo(v.Bones[i].Position, v.Bones[i].ForwardVector);

                // DOMINATOR4
                case -688189648:
                    i=BulletsShot%2==0 ? 59 : 60;
                    return new MuzzleInfo(v.Bones[i].Position, v.Bones[i].ForwardVector);

                // SAVAGE
                case -82626025:
                    return new MuzzleInfo(v.Bones[30].Position, v.Bones[30].ForwardVector);

                // BUZZARD
                case 788747387:
                    i=BulletsShot%2==0 ? 28 : 23;
                    return new MuzzleInfo(v.Bones[i].Position, v.Bones[i].ForwardVector);

                // ANNIHL
                case 837858166:
                    i=(int)BulletsShot%4+35;
                    return new MuzzleInfo(v.Bones[i].Position, v.Bones[i].ForwardVector);

                // HYDRA
                case 970385471:
                    i=BulletsShot%2==0 ? 29 : 28;
                    return new MuzzleInfo(v.Bones[i].Position, v.Bones[i].ForwardVector);

                // STARLING
                case -1700874274:
                    i=BulletsShot%2==0 ? 24 : 12;
                    return new MuzzleInfo(v.Bones[i].Position, v.Bones[i].ForwardVector);

                // RHINO
                case 782665360:
                    return new MuzzleInfo(v.Bones[35].Position,v.Bones[35].ForwardVector);

                default:
                    return null;
            }
        }

        public static bool IsUsingProjectileWeapon(this Ped p)
        {
            var vp = p.VehicleWeapon;
            if (vp!=VehicleWeaponHash.Invalid)
            {
                return VehicleProjectileWeapons.Contains(vp);
            }
            else
            {
                var w = p.Weapons.Current;
                return w.Group==WeaponGroup.Thrown || ProjectileWeapons.Contains(w.Hash);
            }
        }
        
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
    }
}
