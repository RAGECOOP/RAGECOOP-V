using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GTA;
using GTA.Native;
using GTA.UI;
using RageCoop.Client.Menus;

namespace RageCoop.Client
{
    /// <summary>
    ///     Don't use it!
    /// </summary>
    [ScriptAttributes(Author = "RageCoop", SupportURL = "https://github.com/RAGECOOP/RAGECOOP-V")]
    internal class WorldThread : Script
    {
        public static Script Instance;
        private static readonly List<Func<bool>> QueuedActions = new List<Func<bool>>();

        private static bool _trafficEnabled;

        /// <summary>
        ///     Don't use it!
        /// </summary>
        public WorldThread()
        {
            Instance = this;
            Aborted += (e) => { DoQueuedActions(); ChangeTraffic(true); };
        }
        protected override void OnStart()
        {
            base.OnStart();
            while (Game.IsLoading)
                Yield();

            Notification.Show(NotificationIcon.AllPlayersConf, "RAGECOOP", "Welcome!",
                   $"Press ~g~{Settings.MenuKey}~s~ to open the menu.");
        }
        protected override void OnTick()
        {
            base.OnTick();

            if (_sleeping)
            {
                Game.Pause(true);
                while (_sleeping)
                {
                    // Don't wait longer than 5 seconds or the game will crash
                    Thread.Sleep(4500);
                    Yield();
                }
                Game.Pause(false);
            }

            if (Game.IsLoading) return;

            try
            {
                CoopMenu.MenuPool.Process();
                DoQueuedActions();
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }

            if (!Networking.IsOnServer) return;

            Game.DisableControlThisFrame(Control.FrontendPause);

            if (Settings.DisableAlternatePause) Game.DisableControlThisFrame(Control.FrontendPauseAlternate);
            // Sets a value that determines how aggressive the ocean waves will be.
            // Values of 2.0 or more make for very aggressive waves like you see during a thunderstorm.
            Call(SET_DEEP_OCEAN_SCALER, 0.0f); // Works only ~200 meters around the player

            if (Settings.ShowEntityOwnerName)
                unsafe
                {
                    int handle;
                    if (Call<bool>(GET_ENTITY_PLAYER_IS_FREE_AIMING_AT, 0, &handle))
                    {
                        var entity = Entity.FromHandle(handle);
                        if (entity != null)
                        {
                            var owner = "invalid";
                            if (entity.EntityType == EntityType.Vehicle)
                                owner = (entity as Vehicle).GetSyncEntity()?.Owner?.Username ?? "unknown";
                            if (entity.EntityType == EntityType.Ped)
                                owner = (entity as Ped).GetSyncEntity()?.Owner?.Username ?? "unknown";
                            Screen.ShowHelpTextThisFrame("Entity owner: " + owner);
                        }
                    }
                }

            if (!_trafficEnabled)
            {
                Call(SET_VEHICLE_POPULATION_BUDGET, 0);
                Call(SET_PED_POPULATION_BUDGET, 0);
                Call(SET_VEHICLE_DENSITY_MULTIPLIER_THIS_FRAME, 0f);
                Call(SET_RANDOM_VEHICLE_DENSITY_MULTIPLIER_THIS_FRAME, 0f);
                Call(SET_PARKED_VEHICLE_DENSITY_MULTIPLIER_THIS_FRAME, 0f);
                Call(SUPPRESS_SHOCKING_EVENTS_NEXT_FRAME);
                Call(SUPPRESS_AGITATION_EVENTS_NEXT_FRAME);
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
                Call(REMOVE_SCENARIO_BLOCKING_AREAS);
                Call(SET_CREATE_RANDOM_COPS, true);
                Call(SET_RANDOM_TRAINS, true);
                Call(SET_RANDOM_BOATS, true);
                Call(SET_GARBAGE_TRUCKS, true);
                Call(SET_PED_POPULATION_BUDGET, 3); // 0 - 3
                Call(SET_VEHICLE_POPULATION_BUDGET, 3); // 0 - 3
                Call(SET_ALL_VEHICLE_GENERATORS_ACTIVE);
                Call(SET_ALL_LOW_PRIORITY_VEHICLE_GENERATORS_ACTIVE, true);
                Call(SET_NUMBER_OF_PARKED_VEHICLES, -1);
                Call(SET_DISTANT_CARS_ENABLED, true);
                Call(DISABLE_VEHICLE_DISTANTLIGHTS, false);
            }
            else if (Networking.IsOnServer)
            {
                Call(ADD_SCENARIO_BLOCKING_AREA, -10000.0f, -10000.0f, -1000.0f, 10000.0f, 10000.0f,
                    1000.0f, 0, 1, 1, 1);
                Call(SET_CREATE_RANDOM_COPS, false);
                Call(SET_RANDOM_TRAINS, false);
                Call(SET_RANDOM_BOATS, false);
                Call(SET_GARBAGE_TRUCKS, false);
                Call(DELETE_ALL_TRAINS);
                Call(SET_PED_POPULATION_BUDGET, 0);
                Call(SET_VEHICLE_POPULATION_BUDGET, 0);
                Call(SET_ALL_LOW_PRIORITY_VEHICLE_GENERATORS_ACTIVE, false);
                Call(SET_FAR_DRAW_VEHICLES, false);
                Call(SET_NUMBER_OF_PARKED_VEHICLES, 0);
                Call(SET_DISTANT_CARS_ENABLED, false);
                Call(DISABLE_VEHICLE_DISTANTLIGHTS, true);
                foreach (var ped in World.GetAllPeds())
                {
                    if (ped == Game.Player.Character) continue;
                    var c = EntityPool.GetPedByHandle(ped.Handle);
                    if (c == null || (c.IsLocal && ped.Handle != Game.Player.Character.Handle &&
                                      ped.PopulationType != EntityPopulationType.Mission))
                    {
                        Log.Trace($"Removing ped {ped.Handle}. Reason:RemoveTraffic");
                        ped.CurrentVehicle?.Delete();
                        ped.Kill();
                        ped.Delete();
                    }
                }

                foreach (var veh in World.GetAllVehicles())
                {
                    var v = veh.GetSyncEntity();
                    if (v.MainVehicle == Game.Player.LastVehicle ||
                        v.MainVehicle == Game.Player.Character.CurrentVehicle)
                        // Don't delete player's vehicle
                        continue;
                    if (v == null || (v.IsLocal && veh.PopulationType != EntityPopulationType.Mission))
                        // Log.Debug($"Removing Vehicle {veh.Handle}. Reason:ClearTraffic");

                        veh.Delete();
                }
            }
        }

        public static void Delay(Action a, int time)
        {
            Task.Run(() =>
            {
                Thread.Sleep(time);
                QueueAction(a);
            });
        }

        internal static void DoQueuedActions()
        {
            lock (QueuedActions)
            {
                foreach (var action in QueuedActions.ToArray())
                    try
                    {
                        if (action()) QueuedActions.Remove(action);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex);
                        QueuedActions.Remove(action);
                    }
            }
        }

        /// <summary>
        ///     Queue an action to be executed on next tick, allowing you to call scripting API from another thread.
        /// </summary>
        /// <param name="a">
        ///     An action to be executed with a return value indicating whether the action can be removed after
        ///     execution.
        /// </param>
        internal static void QueueAction(Func<bool> a)
        {
            lock (QueuedActions)
            {
                QueuedActions.Add(a);
            }
        }

        internal static void QueueAction(Action a)
        {
            lock (QueuedActions)
            {
                QueuedActions.Add(() =>
                {
                    a();
                    return true;
                });
            }
        }

        /// <summary>
        ///     Clears all queued actions
        /// </summary>
        internal static void ClearQueuedActions()
        {
            lock (QueuedActions)
            {
                QueuedActions.Clear();
            }
        }
        private static bool _sleeping;
        [ConsoleCommand("Put the game to sleep state by blocking main thread, press any key in the debug console to resume")]
        public static void Sleep()
        {
            if (_sleeping)
                throw new InvalidOperationException("Already in sleep state");

            _sleeping = true;
            Task.Run(() =>
            {
                System.Console.WriteLine("Press any key to put the game out of sleep state");
                System.Console.ReadKey();
                System.Console.WriteLine("Game resumed");
                _sleeping = false;
            });
        }
    }
}