using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using GTA;
using GTA.Math;
using GTA.Native;
using GTA.UI;
using LemonUI.Elements;
using LemonUI.Menus;
using Lidgren.Network;
using RageCoop.Client.Menus;
using RageCoop.Client.Scripting;
using RageCoop.Core;
using Control = GTA.Control;
namespace RageCoop.Client
{
    [ScriptAttributes(Author = "RageCoop", SupportURL = "https://github.com/RAGECOOP/RAGECOOP-V", NoScriptThread = true)]
    internal class Main : Script
    {
        internal static Version ModVersion = typeof(Main).Assembly.GetName().Version;

        internal static int LocalPlayerID = 0;

        internal static RelationshipGroup SyncedPedsGroup;

        internal static ClientSettings Settings = null;
        internal static Chat MainChat = null;
        internal static Stopwatch Counter = new();
        internal static Logger Log = null;
        internal static ulong Ticked = 0;
        internal static Vector3 PlayerPosition;
        internal static Scripting.Resources MainRes = null;

        public static Ped P;
        public static float FPS;
        private static bool _lastDead;
        public static bool CefRunning;
        public static bool IsUnloading { get; private set; }

        /// <summary>
        ///     Don't use it!
        /// </summary>
        public Main()
        {
            Directory.CreateDirectory(DataPath);
            try
            {
                Settings = Util.ReadSettings();
            }
            catch
            {
                Notification.Show("Malformed configuration, overwriting with default values...");
                Settings = new();
                Util.SaveSettings();
            }

            Log = new Logger()
            {
                FlushImmediately = true,
                Writers = null,
#if DEBUG
                LogLevel = 0,
#else
                LogLevel = Settings.LogLevel,
#endif
            };
            Log.OnFlush += (line, formatted) =>
            {
                SHVDN.Logger.Write(line.Message, (uint)line.LogLevel);
            };

            // Run static constructor to register all function pointers and remoting entries
            RuntimeHelpers.RunClassConstructor(typeof(API).TypeHandle);
        }

        protected override void OnAborted(AbortedEventArgs e)
        {
            base.OnAborted(e);
            try
            {
                IsUnloading = e.IsUnloading;
                CleanUp("Abort");
                WorldThread.DoQueuedActions();
                if (IsUnloading)
                {
                    ThreadManager.OnUnload();
                    Log.Dispose();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }
        protected override void OnStart()
        {
            base.OnStart();

            if (Game.Version < GameVersion.v1_0_1290_1_Steam)
            {
                throw new NotSupportedException("Please update your GTA5 to v1.0.1290 or newer!");
            }

            MainRes = new();



            Log.Info(
                $"Main script initialized");

            BaseScript.OnStart();
            SyncedPedsGroup = World.AddRelationshipGroup("SYNCPED");
            Game.Player.Character.RelationshipGroup.SetRelationshipBetweenGroups(SyncedPedsGroup, Relationship.Neutral,
                true);
            MainChat = new Chat();

            Util.NativeMemory();
            Counter.Restart();

        }
        protected override void OnTick()
        {
            base.OnTick();
            P = Game.Player.Character;
            PlayerPosition = P.ReadPosition();
            FPS = Game.FPS;
#if CEF
            if (CefRunning)
            {
                CefManager.Tick();
            }
#endif
            if (!Networking.IsOnServer)
            {
                return;
            }
            try
            {
                EntityPool.DoSync();
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }


            if (Game.TimeScale != 1.0f)
            {
                Game.TimeScale = 1;
            }

            if (Networking.ShowNetworkInfo)
            {
                new ScaledText(new PointF(200, 0),
                        $"L: {Networking.Latency * 1000:N0}ms", 0.5f)
                { Alignment = Alignment.Center }.Draw();
                new ScaledText(new PointF(200, 30),
                        $"R: {NetUtility.ToHumanReadable(Statistics.BytesDownPerSecond)}/s", 0.5f)
                { Alignment = Alignment.Center }.Draw();
                new ScaledText(new PointF(200, 60),
                        $"S: {NetUtility.ToHumanReadable(Statistics.BytesUpPerSecond)}/s", 0.5f)
                { Alignment = Alignment.Center }.Draw();
            }

            MainChat.Tick();
            PlayerList.Tick();
            if (!API.Config.EnableAutoRespawn)
            {
                Call(PAUSE_DEATH_ARREST_RESTART, true);
                Call(IGNORE_NEXT_RESTART, true);
                Call(FORCE_GAME_STATE_PLAYING);
                Call(TERMINATE_ALL_SCRIPTS_WITH_THIS_NAME, "respawn_controller");
                if (P.IsDead)
                {
                    Call(SET_FADE_OUT_AFTER_DEATH, false);

                    if (P.Health != 1)
                    {
                        P.Health = 1;
                        Game.Player.WantedLevel = 0;
                        Log.Debug("Player died.");
                        API.Events.InvokePlayerDied();
                    }

                    Screen.StopEffects();
                }
                else
                {
                    Call(DISPLAY_HUD, true);
                }
            }
            else if (P.IsDead && !_lastDead)
            {
                API.Events.InvokePlayerDied();
            }

            _lastDead = P.IsDead;
            Ticked++;
        }

        protected override void OnKeyUp(GTA.KeyEventArgs e)
        {
            base.OnKeyUp(e);

            if (e.KeyCode == Keys.U)
            {
                foreach (var prop in typeof(APIBridge).GetProperties(BindingFlags.Public | BindingFlags.Static))
                {
                    Console.PrintInfo($"{prop.Name}: {JsonSerialize(prop.GetValue(null))}");
                }
                foreach (var prop in typeof(APIBridge.Config).GetProperties(BindingFlags.Public | BindingFlags.Static))
                {
                    Console.PrintInfo($"{prop.Name}: {JsonSerialize(prop.GetValue(null))}");
                }
            }
            if (e.KeyCode == Keys.I)
            {
                APIBridge.SendChatMessage("test");
            }
#if CEF
            if (CefRunning)
            {
                CefManager.KeyUp(e.KeyCode);
            }
#endif
        }

        protected unsafe override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (MainChat.Focused)
            {
                MainChat.OnKeyDown(e.KeyCode);
                return;
            }
#if CEF
            if (CefRunning)
            {
                CefManager.KeyDown(e.KeyCode);
            }
#endif
            if (Networking.IsOnServer)
            {
                if (Voice.WasInitialized())
                {
                    if (Game.IsControlPressed(Control.PushToTalk))
                    {
                        Voice.StartRecording();
                        return;
                    }

                    if (Voice.IsRecording())
                    {
                        Voice.StopRecording();
                        return;
                    }
                }

                if (Game.IsControlPressed(Control.FrontendPause))
                {
                    Call(ACTIVATE_FRONTEND_MENU,
                        SHVDN.NativeMemory.GetHashKey("FE_MENU_VERSION_SP_PAUSE"), false, 0);
                    return;
                }

                if (Game.IsControlPressed(Control.FrontendPauseAlternate) && Settings.DisableAlternatePause)
                {
                    Call(ACTIVATE_FRONTEND_MENU,
                        SHVDN.NativeMemory.GetHashKey("FE_MENU_VERSION_SP_PAUSE"), false, 0);
                    return;
                }
            }

            if (e.KeyCode == Settings.MenuKey)
            {
                if (CoopMenu.MenuPool.AreAnyVisible)
                {
                    CoopMenu.MenuPool.ForEach<NativeMenu>(x =>
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
            else if (Game.IsControlJustPressed(Control.MpTextChatAll))
            {
                if (Networking.IsOnServer)
                {
                    MainChat.Focused = true;
                }
            }
            else if (MainChat.Focused)
            {
                return;
            }
            else if (Game.IsControlJustPressed(Control.MultiplayerInfo))
            {
                if (Networking.IsOnServer)
                {
                    ulong currentTimestamp = Util.GetTickCount64();
                    PlayerList.Pressed = (currentTimestamp - PlayerList.Pressed) < 5000
                        ? (currentTimestamp - 6000)
                        : currentTimestamp;
                }
            }
            else if (e.KeyCode == Settings.PassengerKey)
            {
                if (P == null || P.IsInVehicle())
                {
                    return;
                }

                if (P.IsTaskActive(TaskType.CTaskEnterVehicle))
                {
                    P.Task.ClearAll();
                }
                else
                {
                    var V = World.GetClosestVehicle(P.ReadPosition(), 15);

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

        internal static void Connected()
        {
            Memory.ApplyPatches();
            if (Settings.Voice && !Voice.WasInitialized())
            {
                Voice.Init();
            }

            API.QueueAction(() =>
            {
                WorldThread.Traffic(!Settings.DisableTraffic);
                Call(SET_ENABLE_VEHICLE_SLIPSTREAMING, true);
                CoopMenu.ConnectedMenuSetting();
                MainChat.Init();
                Notification.Show("~g~Connected!");
            });

            Log.Info(">> Connected <<");
        }

        private static readonly object _cleanupLock = new();
        public static void CleanUp(string reason)
        {
            lock (_cleanupLock)
            {

                if (reason != "Abort")
                {
                    Log.Info($">> Disconnected << reason: {reason}");
                    Notification.Show("~r~Disconnected: " + reason);
                }

                if (MainChat?.Focused == true)
                {
                    MainChat.Focused = false;
                }

                PlayerList.Cleanup();
                MainChat?.Clear();
                EntityPool.Cleanup();
                WorldThread.Traffic(true);
                Call(SET_ENABLE_VEHICLE_SLIPSTREAMING, false);
                CoopMenu.DisconnectedMenuSetting();
                LocalPlayerID = default;
                MainRes?.Unload();
                Memory.RestorePatches();
#if CEF
            if (CefRunning)
            {
                CefManager.CleanUp();
            }
#endif

                DownloadManager.Cleanup();
                Voice.ClearAll();
                Networking.Peer?.Dispose();
                Networking.Peer = null;
            }
        }

    }
}