﻿using System;
using System.Linq;
using RageCoop.Core;
using GTA;
using GTA.Native;

namespace RageCoop.Client
{
    /// <summary>
    /// Don't use it!
    /// </summary>
    public class WorldThread : Script
    {
        private static bool _lastDisableTraffic = false;

        /// <summary>
        /// Don't use it!
        /// </summary>
        public WorldThread()
        {
            Tick += OnTick;
            Aborted += (sender, e) =>
            {
                if (_lastDisableTraffic)
                {
                    Traffic(true);
                }
            };
        }

        private void OnTick(object sender, EventArgs e)
        {
            if (Game.IsLoading)
            {
                return;
            }

            if (!Networking.IsOnServer)
            {
                return ;
            }
            Game.DisableControlThisFrame(Control.FrontendPause);
            if (Main.Settings.DisableAlternatePause)
            {
                Game.DisableControlThisFrame(Control.FrontendPauseAlternate);
            }

            // Sets a value that determines how aggressive the ocean waves will be.
            // Values of 2.0 or more make for very aggressive waves like you see during a thunderstorm.
            Function.Call(Hash.SET_DEEP_OCEAN_SCALER, 0.0f); // Works only ~200 meters around the player

            // Function.Call(Hash.SET_CAN_ATTACK_FRIENDLY, Game.Player.Character.Handle, true, false);
            if (Main.Settings==null) { return; }
            if (Main.Settings.DisableTraffic)
            {
                if (!_lastDisableTraffic)
                {
                    Traffic(false);
                }

                Function.Call(Hash.SET_VEHICLE_POPULATION_BUDGET, 0);
                Function.Call(Hash.SET_PED_POPULATION_BUDGET, 0);
                Function.Call(Hash.SET_VEHICLE_DENSITY_MULTIPLIER_THIS_FRAME, 0f);
                Function.Call(Hash.SET_RANDOM_VEHICLE_DENSITY_MULTIPLIER_THIS_FRAME, 0f);
                Function.Call(Hash.SET_PARKED_VEHICLE_DENSITY_MULTIPLIER_THIS_FRAME, 0f);
                Function.Call(Hash.SUPPRESS_SHOCKING_EVENTS_NEXT_FRAME);
                Function.Call(Hash.SUPPRESS_AGITATION_EVENTS_NEXT_FRAME);
            }
            else if (_lastDisableTraffic)
            {
                Traffic(true);
            }

            _lastDisableTraffic = Main.Settings.DisableTraffic;
        }

        private void Traffic(bool enable)
        {
            if (enable)
            {
                Function.Call(Hash.REMOVE_SCENARIO_BLOCKING_AREAS);
                Function.Call(Hash.SET_CREATE_RANDOM_COPS, true);
                Function.Call(Hash.SET_RANDOM_TRAINS, true);
                Function.Call(Hash.SET_RANDOM_BOATS, true);
                Function.Call(Hash.SET_GARBAGE_TRUCKS, true);
                Function.Call(Hash.SET_PED_POPULATION_BUDGET, 1); // 0 - 3
                Function.Call(Hash.SET_VEHICLE_POPULATION_BUDGET, 1); // 0 - 3
                Function.Call(Hash.SET_ALL_VEHICLE_GENERATORS_ACTIVE);
                Function.Call(Hash.SET_ALL_LOW_PRIORITY_VEHICLE_GENERATORS_ACTIVE, true);
                Function.Call(Hash.SET_NUMBER_OF_PARKED_VEHICLES, -1);
                Function.Call(Hash.SET_DISTANT_CARS_ENABLED, true);
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
                Function.Call(Hash.SET_PED_POPULATION_BUDGET, 3);
                Function.Call(Hash.SET_VEHICLE_POPULATION_BUDGET, 3);
                Function.Call(Hash.SET_ALL_LOW_PRIORITY_VEHICLE_GENERATORS_ACTIVE, false);
                Function.Call(Hash.SET_FAR_DRAW_VEHICLES, false);
                Function.Call(Hash.SET_NUMBER_OF_PARKED_VEHICLES, 0);
                Function.Call(Hash.SET_DISTANT_CARS_ENABLED, false);
                Function.Call(Hash.DISABLE_VEHICLE_DISTANTLIGHTS, true);


                foreach (Ped ped in World.GetAllPeds())
                {
                    SyncedPed c = EntityPool.GetPedByHandle(ped.Handle);
                    if ((c==null) || (c.IsLocal && (ped.Handle!=Game.Player.Character.Handle)&&ped.PopulationType!=EntityPopulationType.Mission))
                    {
                        if (ped.Handle==Game.Player.Character.Handle) { continue; }

                        Main.Logger.Trace($"Removing ped {ped.Handle}. Reason:RemoveTraffic");
                        ped.CurrentVehicle?.Delete();
                        ped.Kill();
                        ped.Delete();
                    }
                    
                }
                
                foreach (Vehicle veh in World.GetAllVehicles())
                {
                    SyncedVehicle v = veh.GetSyncEntity();
                    if (v.MainVehicle==Game.Player.LastVehicle)
                    {
                        // Don't delete player's vehicle
                        continue;
                    }
                    if((v== null) || (v.IsLocal&&veh.PopulationType!=EntityPopulationType.Mission))
                    {
                        Main.Logger.Debug($"Removing Vehicle {veh.Handle}. Reason:ClearTraffic");

                        veh.Delete();
                    }
                }
            }
        }
    }
}
