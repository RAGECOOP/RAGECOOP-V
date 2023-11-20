using GTA;
using GTA.Native;
using System;

namespace RageCoop.Client
{
    /// <summary>
    /// Don't use it!
    /// </summary>
    public class WorldThread : Script
    {

        /// <summary>
        /// Don't use it!
        /// </summary>
        public WorldThread()
        {
            Tick += OnTick;
            Aborted += (sender, e) =>
            {
                ChangeTraffic(true);
            };
        }

        private static bool _trafficEnabled;
        private void OnTick(object sender, EventArgs e)
        {
            if (Game.IsLoading || !Networking.IsOnServer)
            {
                return;
            }

            Game.DisableControlThisFrame(Control.FrontendPause);

            if (Main.Settings.DisableAlternatePause)
            {
                Game.DisableControlThisFrame(Control.FrontendPauseAlternate);
            }
            // Sets a value that determines how aggressive the ocean waves will be.
            // Values of 2.0 or more make for very aggressive waves like you see during a thunderstorm.
            Function.Call(Hash.SET_DEEP_OCEAN_SCALER, 0.0f); // Works only ~200 meters around the player

            if (Main.Settings.ShowEntityOwnerName)
            {
                unsafe
                {
                    int handle;
                    if (Function.Call<bool>(Hash.GET_ENTITY_PLAYER_IS_FREE_AIMING_AT, 0, &handle))
                    {
                        var entity = Entity.FromHandle(handle);
                        if (entity != null)
                        {
                            var owner = "invalid";
                            if (entity.EntityType == EntityType.Vehicle)
                            {
                                owner = (entity as Vehicle).GetSyncEntity()?.Owner?.Username ?? "unknown";
                            }
                            if (entity.EntityType == EntityType.Ped)
                            {
                                owner = (entity as Ped).GetSyncEntity()?.Owner?.Username ?? "unknown";
                            }
                            GTA.UI.Screen.ShowHelpTextThisFrame("Entity owner: " + owner);
                        }

                    }
                }
            }
            if (!_trafficEnabled)
            {
                Function.Call(Hash.SET_VEHICLE_POPULATION_BUDGET, 0);
                Function.Call(Hash.SET_PED_POPULATION_BUDGET, 0);
                Function.Call(Hash.SET_VEHICLE_DENSITY_MULTIPLIER_THIS_FRAME, 0f);
                Function.Call(Hash.SET_RANDOM_VEHICLE_DENSITY_MULTIPLIER_THIS_FRAME, 0f);
                Function.Call(Hash.SET_PARKED_VEHICLE_DENSITY_MULTIPLIER_THIS_FRAME, 0f);
                Function.Call(Hash.SUPPRESS_SHOCKING_EVENTS_NEXT_FRAME);
                Function.Call(Hash.SUPPRESS_AGITATION_EVENTS_NEXT_FRAME);
            }
        }
        public static void Traffic(bool enable)
        {
            ChangeTraffic(enable);
            _trafficEnabled = enable;
        }
        private static void ChangeTraffic(bool enable)
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
                Function.Call(Hash.SET_DISTANT_CARS_ENABLED, true);
                Function.Call(Hash.DISABLE_VEHICLE_DISTANTLIGHTS, false);
            }
            else if (Networking.IsOnServer)
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
                Function.Call(Hash.SET_DISTANT_CARS_ENABLED, false);
                Function.Call(Hash.DISABLE_VEHICLE_DISTANTLIGHTS, true);
                foreach (Ped ped in World.GetAllPeds())
                {
                    if (ped == Game.Player.Character) { continue; }
                    SyncedPed c = EntityPool.GetPedByHandle(ped.Handle);
                    if ((c == null) || (c.IsLocal && (ped.Handle != Game.Player.Character.Handle) && ped.PopulationType != EntityPopulationType.Mission))
                    {

                        // Main.Logger.Trace($"Removing ped {ped.Handle}. Reason:RemoveTraffic");
                        ped.CurrentVehicle?.Delete();
                        ped.Kill();
                        ped.Delete();
                    }
                }

                foreach (Vehicle veh in World.GetAllVehicles())
                {
                    SyncedVehicle v = veh.GetSyncEntity();
                    if (v.MainVehicle == Game.Player.LastVehicle || v.MainVehicle == Game.Player.Character.CurrentVehicle)
                    {
                        // Don't delete player's vehicle
                        continue;
                    }
                    if ((v == null) || (v.IsLocal && veh.PopulationType != EntityPopulationType.Mission))
                    {
                        // Main.Logger.Debug($"Removing Vehicle {veh.Handle}. Reason:ClearTraffic");

                        veh.Delete();
                    }
                }
            }
        }
    }
}
