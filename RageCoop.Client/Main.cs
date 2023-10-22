using GTA;
using GTA.Math;
using GTA.Native;
using RageCoop.Client.Menus;
using RageCoop.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RageCoop.Client
{
    /// <summary>
    /// Don't use it!
    /// </summary>
    internal class Main : Script
    {
        private bool _gameLoaded = false;
        internal static Version Version = typeof(Main).Assembly.GetName().Version;

        internal static int LocalPlayerID = 0;

        internal static RelationshipGroup SyncedPedsGroup;

        internal static new Settings Settings = null;
        internal static Scripting.BaseScript BaseScript = new Scripting.BaseScript();

#if !NON_INTERACTIVE
#endif
        internal static Chat MainChat = null;
        internal static Stopwatch Counter = new Stopwatch();
        internal static Logger Logger = null;

        internal static ulong Ticked = 0;
        internal static Vector3 PlayerPosition;
        internal static Scripting.Resources Resources = null;
        private static readonly List<Func<bool>> QueuedActions = new List<Func<bool>>();
        public static Worker Worker;

        /// <summary>
        /// Don't use it!
        /// </summary>
        public Main()
        {
            Worker = new Worker("RageCoop.Client.Main.Worker", Logger);
            try
            {
                Settings = Util.ReadSettings();
            }
            catch
            {
                GTA.UI.Notification.Show("Malformed configuration, overwriting with default values...");
                Settings = new Settings();
                Util.SaveSettings();
            }
            Directory.CreateDirectory(Settings.DataDirectory);
            Logger = new Logger()
            {
                LogPath = $"{Settings.DataDirectory}\\RageCoop.Client.log",
                UseConsole = false,
#if DEBUG
                LogLevel = 0,
#else
                LogLevel=Settings.LogLevel,
#endif
            };
            Resources = new Scripting.Resources();
            if (Game.Version < GameVersion.v1_0_1290_1_Steam)
            {
                Tick += (object sender, EventArgs e) =>
                {
                    if (Game.IsLoading)
                    {
                        return;
                    }
                    if (!_gameLoaded)
                    {
                        GTA.UI.Notification.Show("~r~Please update your GTA5 to v1.0.1290 or newer!", true);
                        _gameLoaded = true;
                    }
                };
                return;
            }
            BaseScript.OnStart();
            SyncedPedsGroup = World.AddRelationshipGroup("SYNCPED");
            Game.Player.Character.RelationshipGroup.SetRelationshipBetweenGroups(SyncedPedsGroup, Relationship.Neutral, true);
#if !NON_INTERACTIVE
#endif
            MainChat = new Chat();
            Tick += OnTick;
            Tick += (s, e) => { Scripting.API.Events.InvokeTick(); };
            KeyDown += OnKeyDown;
            KeyDown += (s, e) => { Scripting.API.Events.InvokeKeyDown(s, e); };
            KeyUp += (s, e) => { Scripting.API.Events.InvokeKeyUp(s, e); };
            Aborted += (object sender, EventArgs e) => Disconnected("Abort");

            Util.NativeMemory();
            Counter.Restart();
        }

        public static Ped P;
        public static float FPS;
        private bool _lastDead;
        private void OnTick(object sender, EventArgs e)
        {
            P = Game.Player.Character;
            PlayerPosition = P.ReadPosition();
            FPS = Game.FPS;
            if (Game.IsLoading)
            {
                return;
            }
            else if (!_gameLoaded && (_gameLoaded = true))
            {
#if !NON_INTERACTIVE
                GTA.UI.Notification.Show(GTA.UI.NotificationIcon.AllPlayersConf, "RAGECOOP", "Welcome!", $"Press ~g~{Main.Settings.MenuKey}~s~ to open the menu.");
#endif
            }

#if !NON_INTERACTIVE
            CoopMenu.MenuPool.Process();
#endif


            DoQueuedActions();
            if (!Networking.IsOnServer)
            {
                return;
            }
            if (Game.TimeScale != 1)
            {
                Game.TimeScale = 1;
            }
            try
            {
                EntityPool.DoSync();
            }
            catch (Exception ex)
            {
#if DEBUG
                Main.Logger.Error(ex);
#endif
            }

            if (Networking.ShowNetworkInfo)
            {
                new LemonUI.Elements.ScaledText(new PointF(Screen.PrimaryScreen.Bounds.Width / 2, 0), $"L: {Networking.Latency * 1000:N0}ms", 0.5f) { Alignment = GTA.UI.Alignment.Center }.Draw();
                new LemonUI.Elements.ScaledText(new PointF(Screen.PrimaryScreen.Bounds.Width / 2, 30), $"R: {Lidgren.Network.NetUtility.ToHumanReadable(Statistics.BytesDownPerSecond)}/s", 0.5f) { Alignment = GTA.UI.Alignment.Center }.Draw();
                new LemonUI.Elements.ScaledText(new PointF(Screen.PrimaryScreen.Bounds.Width / 2, 60), $"S: {Lidgren.Network.NetUtility.ToHumanReadable(Statistics.BytesUpPerSecond)}/s", 0.5f) { Alignment = GTA.UI.Alignment.Center }.Draw();
            }

            MainChat.Tick();
            PlayerList.Tick();
            if (!Scripting.API.Config.EnableAutoRespawn)
            {
                Function.Call(Hash.PAUSE_DEATH_ARREST_RESTART, true);
                Function.Call(Hash.IGNORE_NEXT_RESTART, true);
                Function.Call(Hash.FORCE_GAME_STATE_PLAYING);
                Function.Call(Hash.TERMINATE_ALL_SCRIPTS_WITH_THIS_NAME, "respawn_controller");
                if (P.IsDead)
                {
                    Function.Call(Hash.SET_FADE_OUT_AFTER_DEATH, false);

                    if (P.Health != 1)
                    {
                        P.Health = 1;
                        Game.Player.WantedLevel = 0;
                        Main.Logger.Debug("Player died.");
                        Scripting.API.Events.InvokePlayerDied(KillMessage());
                    }
                    GTA.UI.Screen.StopEffects();
                }
                else
                {
                    Function.Call(Hash.DISPLAY_HUD, true);
                }
            }
            else if (P.IsDead && !_lastDead)
            {
                Scripting.API.Events.InvokePlayerDied(KillMessage());
            }

            _lastDead = P.IsDead;
            Ticked++;
        }
        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (MainChat.Focused)
            {
                MainChat.OnKeyDown(e.KeyCode);
                return;
            }
            if (Networking.IsOnServer)
            {
                if (Voice.WasInitialized())
                {
                    if (Game.IsControlPressed(GTA.Control.PushToTalk))
                    {
                        Voice.StartRecording();
                        return;
                    }
                    else if (Voice.IsRecording())
                    {
                        Voice.StopRecording();
                        return;
                    }
                }

                if (Game.IsControlPressed(GTA.Control.FrontendPause))
                {
                    Function.Call(Hash.ACTIVATE_FRONTEND_MENU, Function.Call<int>(Hash.GET_HASH_KEY, "FE_MENU_VERSION_SP_PAUSE"), false, 0);
                    return;
                }
                if (Game.IsControlPressed(GTA.Control.FrontendPauseAlternate) && Settings.DisableAlternatePause)
                {
                    Function.Call(Hash.ACTIVATE_FRONTEND_MENU, Function.Call<int>(Hash.GET_HASH_KEY, "FE_MENU_VERSION_SP_PAUSE"), false, 0);
                    return;
                }
            }
            if (e.KeyCode == Settings.MenuKey)
            {
                if (CoopMenu.MenuPool.AreAnyVisible)
                {
                    CoopMenu.MenuPool.ForEach<LemonUI.Menus.NativeMenu>(x =>
                    {
                        if (x.Visible)
                        {
                            CoopMenu.LastMenu = x;
                            x.Visible = false;
                        }
                    });
                }
                else
                {
                    CoopMenu.LastMenu.Visible = true;
                }
            }
            else if (Game.IsControlJustPressed(GTA.Control.MpTextChatAll))
            {
                if (Networking.IsOnServer)
                {
                    MainChat.Focused = true;
                }
            }
            else if (MainChat.Focused) { return; }
            else if (Game.IsControlJustPressed(GTA.Control.MultiplayerInfo))
            {
                if (Networking.IsOnServer)
                {
                    ulong currentTimestamp = Util.GetTickCount64();
                    PlayerList.Pressed = (currentTimestamp - PlayerList.Pressed) < 5000 ? (currentTimestamp - 6000) : currentTimestamp;
                }
            }
            else if (e.KeyCode == Settings.PassengerKey)
            {
                var P = Game.Player.Character;

                if (!P.IsInVehicle())
                {
                    if (P.IsTaskActive(TaskType.CTaskEnterVehicle))
                    {
                        P.Task.ClearAll();
                    }
                    else
                    {
                        var V = World.GetClosestVehicle(P.ReadPosition(), 50);

                        if (V != null)
                        {
                            var seat = P.GetNearestSeat(V);
                            var p = V.GetPedOnSeat(seat);
                            if (p != null && !p.IsDead)
                            {
                                for (int i = -1; i < V.PassengerCapacity; i++)
                                {
                                    seat = (VehicleSeat)i;
                                    p = V.GetPedOnSeat(seat);
                                    if (p == null || p.IsDead)
                                    {
                                        break;
                                    }
                                }
                            }
                            P.Task.EnterVehicle(V, seat, -1, 5, EnterVehicleFlags.None);
                        }
                    }
                }
            }
        }
        internal static void Connected()
        {
            Memory.ApplyPatches();
            if (Settings.Voice && !Voice.WasInitialized())
            {
                Voice.Init();
            }
            QueueAction(() =>
            {
                WorldThread.Traffic(!Settings.DisableTraffic);
                Function.Call(Hash.SET_ENABLE_VEHICLE_SLIPSTREAMING, true);
                CoopMenu.ConnectedMenuSetting();
                MainChat.Init();
                GTA.UI.Notification.Show("~g~Connected!");
            });

            Logger.Info(">> Connected <<");
        }
        public static void Disconnected(string reason)
        {


            Logger.Info($">> Disconnected << reason: {reason}");
            QueueAction(() =>
            {
                if (MainChat.Focused)
                {
                    MainChat.Focused = false;
                }
                PlayerList.Cleanup();
                MainChat.Clear();
                EntityPool.Cleanup();
                WorldThread.Traffic(true);
                Function.Call(Hash.SET_ENABLE_VEHICLE_SLIPSTREAMING, false);
                CoopMenu.DisconnectedMenuSetting();
                if (reason != "Abort")
                    GTA.UI.Notification.Show("~r~Disconnected: " + reason);
                LocalPlayerID = default;
            });
            Memory.RestorePatches();
            DownloadManager.Cleanup();
            Voice.ClearAll();
            Resources.Unload();
        }
        private static void DoQueuedActions()
        {
            lock (QueuedActions)
            {
                foreach (var action in QueuedActions.ToArray())
                {
                    try
                    {
                        if (action())
                        {
                            QueuedActions.Remove(action);
                        }
                    }
                    catch (Exception ex)
                    {
#if DEBUG
                        Logger.Error(ex);
#endif
                        QueuedActions.Remove(action);
                    }
                }
            }
        }

        /// <summary>
        /// Queue an action to be executed on next tick, allowing you to call scripting API from another thread.
        /// </summary>
        /// <param name="a"> An action to be executed with a return value indicating whether the action can be removed after execution.</param>
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
                QueuedActions.Add(() => { a(); return true; });
            }
        }
        /// <summary>
        /// Clears all queued actions
        /// </summary>
        internal static void ClearQueuedActions()
        {
            lock (QueuedActions) { QueuedActions.Clear(); }
        }

        public static void Delay(Action a, int time)
        {
            Task.Run(() =>
            {
                Thread.Sleep(time);
                QueueAction(a);
            });
        }

        private string KillMessage()
        {
            if (P.Killer != null)
            {
                var killer = EntityPool.GetPedByHandle(P.Killer.Handle);
                if (killer != null && killer.ID == killer.Owner.ID)
                    return $"~h~{PlayerList.GetPlayer(LocalPlayerID).Username}~h~ was killed by ~h~{killer.Owner.Username}~h~ ({P.CauseOfDeath})";
            }
            return $"~h~{PlayerList.GetPlayer(LocalPlayerID).Username}~h~ died";
        }
    }
}
