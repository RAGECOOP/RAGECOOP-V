using GTA;
using GTA.Native;
using RageCoop.Core;
using System;
using System.Collections.Generic;

namespace RageCoop.Client
{
    internal static class VehicleExtensions
    {
        #region VEHICLE

        public static VehicleDataFlags GetVehicleFlags(this SyncedVehicle v)
        {
            var veh = v.MainVehicle;
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

            if (v.IsSubmarineCar && Function.Call<bool>(Hash.IS_VEHICLE_IN_SUBMARINE_MODE, veh.Handle))
            {
                flags |= VehicleDataFlags.IsTransformed;
            }

            if (v.IsAircraft)
            {
                flags |= VehicleDataFlags.IsAircraft;
            }

            if (v.IsDeluxo && veh.IsDeluxoHovering())
            {
                flags |= VehicleDataFlags.IsDeluxoHovering;
            }

            if (v.HasRoof)
            {
                flags |= VehicleDataFlags.HasRoof;
            }

            if (v.HasRocketBoost && veh.IsRocketBoostActive())
            {
                flags |= VehicleDataFlags.IsRocketBoostActive;
            }

            if (v.HasParachute && veh.IsParachuteActive())
            {
                flags |= VehicleDataFlags.IsParachuteActive;
            }

            if (veh.IsOnFire)
            {
                flags |= VehicleDataFlags.IsOnFire;
            }

            return flags;
        }
        public static bool IsRocketBoostActive(this Vehicle veh)
        {
            return Function.Call<bool>(Hash.IS_ROCKET_BOOST_ACTIVE, veh);
        }
        public static bool IsParachuteActive(this Vehicle veh)
        {
            return Function.Call<bool>((Hash)0x3DE51E9C80B116CF, veh);
        }
        public static void SetRocketBoostActive(this Vehicle veh, bool toggle)
        {
            Function.Call(Hash.SET_ROCKET_BOOST_ACTIVE, veh, toggle);
        }
        public static void SetParachuteActive(this Vehicle veh, bool toggle)
        {
            Function.Call((Hash)0x0BFFB028B3DD0A97, veh, toggle);
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

        public static void SetDamageModel(this Vehicle veh, VehicleDamageModel model, bool leavedoors = true)
        {
            for (int i = 0; i < 8; i++)
            {
                var door = veh.Doors[(VehicleDoorIndex)i];
                if ((model.BrokenDoors & (byte)(1 << i)) != 0)
                {
                    if (!door.IsBroken)
                    {
                        door.Break(leavedoors);
                    }
                    continue;
                }
                else if (door.IsBroken)
                {
                    // The vehicle can only fix a door if the vehicle was completely fixed
                    veh.Repair();
                    return;
                }
                if ((model.OpenedDoors & (byte)(1 << i)) != 0)
                {
                    if ((!door.IsOpen) && (!door.IsBroken))
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

        public static Dictionary<int, int> GetPassengers(this Vehicle veh)
        {
            Dictionary<int, int> ps = new Dictionary<int, int>();
            var d = veh.Driver;
            if (d != null && d.IsSittingInVehicle())
            {
                ps.Add(-1, d.GetSyncEntity().ID);
            }
            foreach (Ped p in veh.Passengers)
            {
                if (p.IsSittingInVehicle())
                {
                    ps.Add((int)p.SeatIndex, p.GetSyncEntity().ID);
                }
            }
            return ps;
        }

        public static void SetDeluxoHoverState(this Vehicle deluxo, bool hover)
        {
            Function.Call(Hash.SET_SPECIAL_FLIGHT_MODE_TARGET_RATIO, deluxo, hover ? 1f : 0f);
        }
        public static bool IsDeluxoHovering(this Vehicle deluxo)
        {
            return Math.Abs(deluxo.Bones[27].ForwardVector.GetCosTheta(deluxo.ForwardVector) - 1) > 0.05;
        }
        public static void SetDeluxoWingRatio(this Vehicle v, float ratio)
        {
            Function.Call(Hash.SET_HOVER_MODE_WING_RATIO, v, ratio);
        }
        public static float GetDeluxoWingRatio(this Vehicle v)
        {
            return v.Bones[99].Position.DistanceTo(v.Bones[92].Position) - 1.43f;
        }
        public static float GetNozzleAngel(this Vehicle plane)
        {
            return Function.Call<float>(Hash.GET_VEHICLE_FLIGHT_NOZZLE_POSITION, plane);
        }
        public static bool HasNozzle(this Vehicle v)
        {

            switch (v.Model.Hash)
            {
                // Hydra
                case 970385471:
                    return true;

                // Avenger
                case -2118308144:
                    return true;

                // Tula
                case 1043222410:
                    return true;

                // Avenger
                case 408970549:
                    return true;

            }
            return false;
        }
        public static void SetNozzleAngel(this Vehicle plane, float ratio)
        {
            Function.Call(Hash.SET_VEHICLE_FLIGHT_NOZZLE_POSITION, plane, ratio);
        }
        #endregion
    }
}
