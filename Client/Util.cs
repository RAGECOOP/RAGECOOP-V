using System;
using System.IO;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using RageCoop.Core;
using GTA;
using GTA.Native;
using GTA.Math;
using System.Linq;
using System.Diagnostics;

namespace RageCoop.Client
{
    public enum ETasks
    {
        CLIMB_LADDER = 47
    }

    internal static partial class Util
    {
        #region -- POINTER --
        private static int _steeringAngleOffset { get; set; }

        public static unsafe void NativeMemory()
        {
            IntPtr address;

            address = Game.FindPattern("\x74\x0A\xF3\x0F\x11\xB3\x1C\x09\x00\x00\xEB\x25", "xxxxxx????xx");
            if (address != IntPtr.Zero)
            {
                _steeringAngleOffset = *(int*)(address + 6) + 8;
            }

            // breaks some stuff.
            /*
            address = Game.FindPattern("\x32\xc0\xf3\x0f\x11\x09", "xxxxxx"); // Weapon / Radio slowdown
            if (address != IntPtr.Zero)
            {
                for (int i = 0; i < 6; i++)
                {
                    *(byte*)(address + i).ToPointer() = 0x90;
                }
            }
            */
        }

        public static unsafe void CustomSteeringAngle(this Vehicle veh, float value)
        {
            IntPtr address = new IntPtr((long)veh.MemoryAddress);
            if (address == IntPtr.Zero || _steeringAngleOffset == 0)
            {
                return;
            }

            *(float*)(address + _steeringAngleOffset).ToPointer() = value;
        }
        #endregion

        public static Settings ReadSettings()
        {
            XmlSerializer ser = new XmlSerializer(typeof(Settings));

            string path = Directory.GetCurrentDirectory() + "\\scripts\\CoopSettings.xml";
            Settings settings = null;

            if (File.Exists(path))
            {
                using (FileStream stream = File.OpenRead(path))
                {
                    settings = (RageCoop.Client.Settings)ser.Deserialize(stream);
                }

                using (FileStream stream = new FileStream(path, FileMode.Truncate, FileAccess.ReadWrite))
                {
                    ser.Serialize(stream, settings);
                }
            }
            else
            {
                using (FileStream stream = File.OpenWrite(path))
                {
                    ser.Serialize(stream, settings = new Settings());
                }
            }

            return settings;
        }
        public static void SaveSettings()
        {
            try
            {
                string path = Directory.GetCurrentDirectory() + "\\scripts\\CoopSettings.xml";

                using (FileStream stream = new FileStream(path, File.Exists(path) ? FileMode.Truncate : FileMode.Create, FileAccess.ReadWrite))
                {
                    XmlSerializer ser = new XmlSerializer(typeof(Settings));
                    ser.Serialize(stream, Main.Settings);
                }
            }
            catch (Exception ex)
            {
                GTA.UI.Notification.Show("Error saving player settings: " + ex.Message);
            }
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
                    GTA.UI.Notification.Show($"~r~Reloading failed! Weapon ~g~[{ped.Weapons.Current.Hash}]~r~ could not be found!");
                    return null;
            }
        }

        public static Model ModelRequest(this int hash)
        {
            Model model = new Model(hash);

            if (!model.IsValid)
            {
                //GTA.UI.Notification.Show("~y~Not valid!");
                return null;
            }

            if (!model.IsLoaded)
            {
                return model.Request(1000) ? model : null;
            }

            return model;
        }

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

        public static Vector3 LinearVectorLerp(Vector3 start, Vector3 end, ulong currentTime, int duration)
        {
            return new Vector3()
            {
                X = LinearFloatLerp(start.X, end.X, currentTime, duration),
                Y = LinearFloatLerp(start.Y, end.Y, currentTime, duration),
                Z = LinearFloatLerp(start.Z, end.Z, currentTime, duration),
            };
        }

        public static float LinearFloatLerp(float start, float end, ulong currentTime, int duration)
        {
            return (end - start) * currentTime / duration + start;
        }

        public static float Lerp(float from, float to, float fAlpha)
        {
            return (from * (1.0f - fAlpha)) + (to * fAlpha); //from + (to - from) * fAlpha
        }

        public static byte GetPedSpeed(this Ped ped)
        {
            if (ped.IsSprinting)
            {
                return 3;
            }
            if (ped.IsRunning)
            {
                return 2;
            }
            if (ped.IsWalking)
            {
                return 1;
            }
            
            return 0;
        }


        public static VehicleDataFlags GetVehicleFlags(this Vehicle veh)
        {
            VehicleDataFlags flags = 0;

            if (veh.IsEngineRunning)
            {
                flags |= VehicleDataFlags.IsEngineRunning;
            }

            if (veh.AreLightsOn)
            {
                flags |= VehicleDataFlags.AreLightsOn;
            }

            if (veh.BrakePower >= 0.01f)
            {
                flags |= VehicleDataFlags.AreBrakeLightsOn;
            }

            if (veh.AreHighBeamsOn)
            {
                flags |= VehicleDataFlags.AreHighBeamsOn;
            }

            if (veh.IsSirenActive)
            {
                flags |= VehicleDataFlags.IsSirenActive;
            }

            if (veh.IsDead)
            {
                flags |= VehicleDataFlags.IsDead;
            }

            if (Function.Call<bool>(Hash.IS_HORN_ACTIVE, veh.Handle))
            {
                flags |= VehicleDataFlags.IsHornActive;
            }

            if (veh.IsSubmarineCar && Function.Call<bool>(Hash._GET_IS_SUBMARINE_VEHICLE_TRANSFORMED, veh.Handle))
            {
                flags |= VehicleDataFlags.IsTransformed;
            }

            if (veh.HasRoof && (veh.RoofState == VehicleRoofState.Opened || veh.RoofState == VehicleRoofState.Opening))
            {
                flags |= VehicleDataFlags.RoofOpened;
            }


            if (veh.IsAircraft)
            {
                flags |= VehicleDataFlags.IsAircraft;
            }


            return flags;
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

            if (ped.IsRagdoll)
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

            if (Function.Call<bool>(Hash.GET_IS_TASK_ACTIVE, ped.Handle, ETasks.CLIMB_LADDER)) // USING_LADDER
            {
                flags |= PedDataFlags.IsOnLadder;
            }

            if (ped.IsVaulting && !Function.Call<bool>(Hash.GET_IS_TASK_ACTIVE, ped.Handle, ETasks.CLIMB_LADDER))
            {
                flags |= PedDataFlags.IsVaulting;
            }

            if (ped.IsInCover || ped.IsGoingIntoCover)
            {
                flags |=PedDataFlags.IsInCover;
            }

            return flags;
        }

        public static bool HasFlag(this PedDataFlags flagToCheck,PedDataFlags flag)
        {
            return (flagToCheck & flag)!=0;
        }

        public static bool HasFlag(this VehicleDataFlags flagToCheck, VehicleDataFlags flag)
        {
            return (flagToCheck & flag)!=0;
        }

        public static Dictionary<byte, short> GetPedClothes(this Ped ped)
        {
            Dictionary<byte, short> result = new Dictionary<byte, short>();
            for (byte i = 0; i < 11; i++)
            {
                short mod = Function.Call<short>(Hash.GET_PED_DRAWABLE_VARIATION, ped.Handle, i);
                result.Add(i, mod);
            }
            return result;
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

        public static Dictionary<int, int> GetVehicleMods(this VehicleModCollection mods)
        {
            Dictionary<int, int> result = new Dictionary<int, int>();
            foreach (VehicleMod mod in mods.ToArray())
            {
                result.Add((int)mod.Type, mod.Index);
            }
            return result;
        }

        public static VehicleDamageModel GetVehicleDamageModel(this Vehicle veh)
        {
            // Broken windows
            byte brokenWindows = 0;
            for (int i = 0; i < 8; i++)
            {
                if (!veh.Windows[(VehicleWindowIndex)i].IsIntact)
                {
                    brokenWindows |= (byte)(1 << i);
                }
            }

            // Broken doors
            byte brokenDoors = 0;
            byte openedDoors = 0;
            foreach (VehicleDoor door in veh.Doors)
            {
                if (door.IsBroken)
                {
                    brokenDoors |= (byte)(1 << (byte)door.Index);
                }
                else if (door.IsOpen)
                {
                    openedDoors |= (byte)(1 << (byte)door.Index);
                }
            }


            // Bursted tires
            short burstedTires = 0;
            foreach (VehicleWheel wheel in veh.Wheels.GetAllWheels())
            {
                if (wheel.IsBursted)
                {
                    burstedTires |= (short)(1 << (int)wheel.BoneId);
                }
            }

            return new VehicleDamageModel()
            {
                BrokenDoors = brokenDoors,
                OpenedDoors = openedDoors,
                BrokenWindows = brokenWindows,
                BurstedTires = burstedTires,
                LeftHeadLightBroken = (byte)(veh.IsLeftHeadLightBroken ? 1 : 0),
                RightHeadLightBroken = (byte)(veh.IsRightHeadLightBroken ? 1 : 0)
            };
        }

        public static Dictionary<int,int> GetPassengers(this Vehicle veh)
        {
            Dictionary<int,int> ps=new Dictionary<int, int>();
            var d = veh.Driver;
            if (d!=null&&d.IsSittingInVehicle())
            {
                ps.Add(-1, d.GetSyncEntity().ID);
            }
            foreach(Ped p in veh.Passengers)
            {
                if (p.IsSittingInVehicle())
                {
                    ps.Add((int)p.SeatIndex, (int)p.GetSyncEntity().ID);
                }
            }
            return ps;
        }
        public static void SetOnFire(this Entity e,bool toggle)
        {
            if (toggle)
            {
                Function.Call(Hash.START_ENTITY_FIRE, e.Handle);
            }
            else
            {
                Function.Call(Hash.STOP_ENTITY_FIRE, e.Handle);
            }
        }
        public static SyncedPed GetSyncEntity(this Ped p)
        {
            if(p == null) { return null; }
            var c = EntityPool.GetPedByHandle(p.Handle);
            if(c==null) { EntityPool.Add(c=new SyncedPed(p)); }
            return c;
        }
        public static SyncedVehicle GetSyncEntity(this Vehicle veh)
        {
            if(veh == null) { return null; }
            var v=EntityPool.GetVehicleByHandle(veh.Handle);
            if (v==null) { EntityPool.Add(v=new SyncedVehicle(veh)); }
            return v;
        }
        public static void SetVehicleDamageModel(this Vehicle veh, VehicleDamageModel model, bool leavedoors = true)
        {
            for (int i = 0; i < 8; i++)
            {
                var door = veh.Doors[(VehicleDoorIndex)i];
                if ((model.BrokenDoors & (byte)(1 << i)) != 0)
                {
                    door.Break(leavedoors);
                }
                else if (door.IsBroken)
                {
                    // The vehicle can only fix a door if the vehicle was completely fixed
                    veh.Repair();
                    return;
                }
                if ((model.OpenedDoors & (byte)(1 << i)) != 0)
                {
                    if ((!door.IsOpen)&&(!door.IsBroken))
                    {
                        door.Open();
                    }
                }
                else if (door.IsOpen)
                {
                    if (!door.IsBroken) { door.Close(); }
                }

                if ((model.BrokenWindows & (byte)(1 << i)) != 0)
                {
                    veh.Windows[(VehicleWindowIndex)i].Smash();
                }
                else if (!veh.Windows[(VehicleWindowIndex)i].IsIntact)
                {
                    veh.Windows[(VehicleWindowIndex)i].Repair();
                }
            }

            foreach (VehicleWheel wheel in veh.Wheels)
            {
                if ((model.BurstedTires & (short)(1 << (int)wheel.BoneId)) != 0)
                {
                    if (!wheel.IsBursted)
                    {
                        wheel.Puncture();
                        wheel.Burst();
                    }
                }
                else if (wheel.IsBursted)
                {
                    wheel.Fix();
                }
            }

            veh.IsLeftHeadLightBroken = model.LeftHeadLightBroken > 0;
            veh.IsRightHeadLightBroken = model.RightHeadLightBroken > 0;
        }

        public static double DegToRad(double deg)
        {
            return deg * Math.PI / 180.0;
        }

        public static Vector3 RotationToDirection(Vector3 rotation)
        {
            double z = DegToRad(rotation.Z);
            double x = DegToRad(rotation.X);
            double num = Math.Abs(Math.Cos(x));

            return new Vector3
            {
                X = (float)(-Math.Sin(z) * num),
                Y = (float)(Math.Cos(z) * num),
                Z = (float)Math.Sin(x)
            };
        }


        public static VehicleSeat getNearestSeat(Ped ped, Vehicle veh, float distanceToignoreDoors=50f)
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
                    bool flag2 = (num2 < distanceToignoreDoors) && (num2 < num )&& IsSeatUsableByPed(ped, veh, dictionary[text]);
                    if (flag2)
                    {
                        num = num2;
                        result = dictionary[text];
                    }
                }
            }
            return (VehicleSeat)result;
        }
        public static bool IsSeatUsableByPed(Ped ped, Vehicle veh, int _seat, bool allowJacking=false)
        {
            /*
            
            */
            if (!allowJacking)
            {
                return veh.IsSeatFree((VehicleSeat)_seat);
            }
            else
            {
                VehicleSeat seat = (VehicleSeat)_seat;
                bool result = false;
                bool flag = veh.IsSeatFree(seat);
                if (flag)
                {
                    result = true;
                }
                else
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
        }
        public static Vector3 GetAimCoord(this Ped p)
        {
            var weapon = p.Weapons.CurrentWeaponObject;
            if (p.IsOnTurretSeat()) { return p.GetLookingCoord(); }
            if (weapon!=null)
            {
                Vector3 dir = weapon.RightVector;
                return weapon.Position+dir*20;

                
                RaycastResult result = World.Raycast(weapon.Position+dir, weapon.Position+dir*10000, IntersectFlags.Everything, p.IsInVehicle() ? (Entity)p : p.CurrentVehicle);
                
                if (result.DidHit)
                {
                    return result.HitPosition;
                }
                else
                {
                    return weapon.Position+dir*20;
                }
            }
            return GetLookingCoord(p);
        }
        public static Vector3 GetLookingCoord(this Ped p)
        {
            EntityBone b = p.Bones[Bone.FacialForehead];
            Vector3 v = b.UpVector.Normalized;
            return b.Position+200*v;
        }
        public static bool IsWeapon(this Entity e)
        {
            return WeaponModels.Contains(e.Model);
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
                    return (VehicleHash)veh.Model.Hash == VehicleHash.Apc;
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
        public static void StayInCover(this Ped p)
        {
            Function.Call(Hash.TASK_STAY_IN_COVER, p);
        }
        public static VehicleSeat GetSeatTryingToEnter(this Ped p)
        {
            return (VehicleSeat)Function.Call<int>(Hash.GET_SEAT_PED_IS_TRYING_TO_ENTER, p);
        }


        public static readonly Model[] WeaponModels = Weapon.GetAllModels();
        
        [DllImport("kernel32.dll")]
        public static extern ulong GetTickCount64();
        public static int GetDamage(this Weapon w)
        {
            int damage=0;
            switch (w.Group)
            {
                case WeaponGroup.AssaultRifle: damage=30;break;
                case WeaponGroup.Heavy: damage=30;break;
                case WeaponGroup.MG: damage=30;break;
                case WeaponGroup.PetrolCan: damage=0;break;
                case WeaponGroup.Pistol: damage=30;break;
                case WeaponGroup.Shotgun: damage=30; break;
                case WeaponGroup.SMG: damage=20; break;
                case WeaponGroup.Sniper: damage=100; break;
                case WeaponGroup.Thrown: damage=0; break;
                case WeaponGroup.Unarmed: damage=0; break;
            }
            return damage;
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

    }
    /// <summary>
    /// 
    /// </summary>
    public static class VectorExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        public static Vector3 ToVector(this Quaternion vec)
        {
            return new Vector3()
            {
                X = vec.X,
                Y = vec.Y,
                Z = vec.Z
            };
        }

        /// <summary>
        /// 
        /// </summary>
        public static Quaternion ToQuaternion(this Vector3 vec, float vW = 0.0f)
        {
            return new Quaternion()
            {
                X = vec.X,
                Y = vec.Y,
                Z = vec.Z,
                W = vW
            };
        }

        public static float Denormalize(this float h)
        {
            return h < 0f ? h + 360f : h;
        }

        public static float ToRadians(this float val)
        {
            return (float)(Math.PI / 180) * val;
        }

        public static Vector3 ToRadians(this Vector3 i)
        {
            return new Vector3()
            {
                X = ToRadians(i.X),
                Y = ToRadians(i.Y),
                Z = ToRadians(i.Z),
            };
        }

        public static Quaternion ToQuaternion(this Vector3 vect)
        {
            vect = new Vector3()
            {
                X = vect.X.Denormalize() * -1,
                Y = vect.Y.Denormalize() - 180f,
                Z = vect.Z.Denormalize() - 180f,
            };

            vect = vect.ToRadians();

            float rollOver2 = vect.Z * 0.5f;
            float sinRollOver2 = (float)Math.Sin((double)rollOver2);
            float cosRollOver2 = (float)Math.Cos((double)rollOver2);
            float pitchOver2 = vect.Y * 0.5f;
            float sinPitchOver2 = (float)Math.Sin((double)pitchOver2);
            float cosPitchOver2 = (float)Math.Cos((double)pitchOver2);
            float yawOver2 = vect.X * 0.5f; // pitch
            float sinYawOver2 = (float)Math.Sin((double)yawOver2);
            float cosYawOver2 = (float)Math.Cos((double)yawOver2);
            Quaternion result = new Quaternion()
            {
                X = cosYawOver2 * cosPitchOver2 * cosRollOver2 + sinYawOver2 * sinPitchOver2 * sinRollOver2,
                Y = cosYawOver2 * cosPitchOver2 * sinRollOver2 - sinYawOver2 * sinPitchOver2 * cosRollOver2,
                Z = cosYawOver2 * sinPitchOver2 * cosRollOver2 + sinYawOver2 * cosPitchOver2 * sinRollOver2,
                W = sinYawOver2 * cosPitchOver2 * cosRollOver2 - cosYawOver2 * sinPitchOver2 * sinRollOver2
            };
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        public static LVector3 ToLVector(this Vector3 vec)
        {
            return new LVector3()
            {
                X = vec.X,
                Y = vec.Y,
                Z = vec.Z
            };
        }

        /// <summary>
        /// 
        /// </summary>
        public static LQuaternion ToLQuaternion(this Quaternion vec)
        {
            return new LQuaternion()
            {
                X = vec.X,
                Y = vec.Y,
                Z = vec.Z,
                W = vec.W
            };
        }
    }
    
}
