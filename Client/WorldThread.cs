using System;
using System.Linq;

using GTA;
using GTA.Native;

namespace CoopClient
{
    internal class WorldThread : Script
    {
        private static bool LastDisableTraffic = false;

        public WorldThread()
        {
            Tick += OnTick;
            Interval = Util.GetGameMs<int>();
            Aborted += (sender, e) =>
            {
                if (LastDisableTraffic)
                {
                    Traffic(true);
                }
            };
        }

        public static void OnTick(object sender, EventArgs e)
        {
            if (Game.IsLoading)
            {
                return;
            }

            Function.Call((Hash)0xB96B00E976BE977F, 0.0f); // _SET_WAVES_INTENSITY

            Function.Call(Hash.SET_CAN_ATTACK_FRIENDLY, Game.Player.Character.Handle, true, false);

            if (Main.DisableTraffic)
            {
                if (!LastDisableTraffic)
                {
                    Traffic(false);
                }

                Function.Call(Hash.SET_VEHICLE_POPULATION_BUDGET, 0);
                Function.Call(Hash.SET_PED_POPULATION_BUDGET, 0);
                Function.Call(Hash.SET_VEHICLE_DENSITY_MULTIPLIER_THIS_FRAME, 0f);
                Function.Call(Hash.SET_RANDOM_VEHICLE_DENSITY_MULTIPLIER_THIS_FRAME, 0f);
                Function.Call(Hash.SET_PARKED_VEHICLE_DENSITY_MULTIPLIER_THIS_FRAME, 0f);
                Function.Call((Hash)0x2F9A292AD0A3BD89);
                Function.Call((Hash)0x5F3B7749C112D552);
            }
            else if (LastDisableTraffic)
            {
                Traffic(true);
            }

            LastDisableTraffic = Main.DisableTraffic;
        }

        private static void Traffic(bool enable)
        {
            if (enable)
            {
                Function.Call(Hash.REMOVE_SCENARIO_BLOCKING_AREAS);
                Function.Call(Hash.SET_CREATE_RANDOM_COPS, true);
                Function.Call(Hash.SET_RANDOM_TRAINS, true);
                Function.Call(Hash.SET_RANDOM_BOATS, true);
                Function.Call(Hash.SET_GARBAGE_TRUCKS, true);
                Function.Call(Hash.SET_PED_POPULATION_BUDGET, 3); // 0 - 3
                Function.Call(Hash.SET_VEHICLE_POPULATION_BUDGET, 3); // 0 - 3
                Function.Call(Hash.SET_ALL_VEHICLE_GENERATORS_ACTIVE);
                Function.Call(Hash.SET_ALL_LOW_PRIORITY_VEHICLE_GENERATORS_ACTIVE, true);
                Function.Call(Hash.SET_NUMBER_OF_PARKED_VEHICLES, -1);
                Function.Call((Hash)0xF796359A959DF65D, true); // Display distant vehicles
                Function.Call(Hash.DISABLE_VEHICLE_DISTANTLIGHTS, false);
            }
            else
            {
                Function.Call(Hash.ADD_SCENARIO_BLOCKING_AREA, -10000.0f, -10000.0f, -1000.0f, 10000.0f, 10000.0f, 1000.0f, 0, 1, 1, 1);
                Function.Call(Hash.SET_CREATE_RANDOM_COPS, false);
                Function.Call(Hash.SET_RANDOM_TRAINS, false);
                Function.Call(Hash.SET_RANDOM_BOATS, false);
                Function.Call(Hash.SET_GARBAGE_TRUCKS, false);
                Function.Call(Hash.DELETE_ALL_TRAINS);
                Function.Call(Hash.SET_PED_POPULATION_BUDGET, 0);
                Function.Call(Hash.SET_VEHICLE_POPULATION_BUDGET, 0);
                Function.Call(Hash.SET_ALL_LOW_PRIORITY_VEHICLE_GENERATORS_ACTIVE, false);
                Function.Call(Hash.SET_FAR_DRAW_VEHICLES, false);
                Function.Call(Hash.SET_NUMBER_OF_PARKED_VEHICLES, 0);
                Function.Call((Hash)0xF796359A959DF65D, false); //Display distant vehicles
                Function.Call(Hash.DISABLE_VEHICLE_DISTANTLIGHTS, true);

                foreach (Ped ped in World.GetAllPeds().Where(p => p.RelationshipGroup != "SYNCPED"))
                {
                    ped.CurrentVehicle?.Delete();
                    ped.Kill();
                    ped.Delete();
                }

                foreach (Vehicle veh in World.GetAllVehicles().Where(v => v.IsSeatFree(VehicleSeat.Driver) && v.PassengerCount == 0))
                {
                    veh.Delete();
                }
            }
        }
    }
}
