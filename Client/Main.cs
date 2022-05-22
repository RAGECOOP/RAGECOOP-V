#undef DEBUG
using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Diagnostics;
using RageCoop.Client.Menus;
using RageCoop.Core;
using GTA;
using GTA.Native;
using GTA.Math;

namespace RageCoop.Client
{
    /// <summary>
    /// Don't use it!
    /// </summary>
    public class Main : Script
    {

        private bool _gameLoaded = false;
        private static bool _isGoingToCar = false;

        public static readonly string CurrentVersion = "V1_4_1";

        public static int MyPlayerID=0;
        public static bool DisableTraffic = true;
        public static bool NPCsAllowed = false;
         internal static RelationshipGroup SyncedPedsGroup;

        public static Settings Settings = null;
        public static Networking MainNetworking = null;

#if !NON_INTERACTIVE
        public static MenusMain MainMenu = null;
#endif
        public static Chat MainChat = null;
        public static PlayerList MainPlayerList = new PlayerList();
        public static Stopwatch Counter = new Stopwatch();

        public static ulong Ticked = 0;

        /*
        // <ID,Entity>
        public static Dictionary<int, CharacterEntity> Characters = new Dictionary<int, CharacterEntity>();

        // Dictionary<int ID, Entuty>
        public static Dictionary<int, VehicleEntity> Vehicles = new Dictionary<int, VehicleEntity>();
        */
        public static Loggger Logger=new Loggger("Scripts\\RageCoop\\RageCoop.Client.log");
        
        private static List<Func<bool>> QueuedActions = new List<Func<bool>>();

        /// <summary>
        /// Don't use it!
        /// </summary>
        public Main()
        {
            // Required for some synchronization!
            /*if (Game.Version < GameVersion.v1_0_1290_1_Steam)
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
            }*/
            SyncedPedsGroup=World.AddRelationshipGroup("SYNCPED");
            Game.Player.Character.RelationshipGroup.SetRelationshipBetweenGroups(SyncedPedsGroup, Relationship.Neutral, true);
            Settings = Util.ReadSettings();
            MainNetworking = new Networking();
            MainNetworking.Start();
#if !NON_INTERACTIVE
            MainMenu = new MenusMain();
#endif
            MainChat = new Chat();
            Logger.LogLevel =0;
            Tick += OnTick;
            KeyDown += OnKeyDown;
            Aborted += (object sender, EventArgs e) => CleanUp();
            
            Util.NativeMemory();
            Counter.Restart();
        }
        
#if DEBUG
        private ulong _lastDebugData;
        private int _debugBytesSend;
        private int _debugBytesReceived;
#endif
        private void OnTick(object sender, EventArgs e)
        {
            if (Game.IsLoading)
            {
                return;
            }
            else if (!_gameLoaded && (_gameLoaded = true))
            {
#if !NON_INTERACTIVE
                GTA.UI.Notification.Show(GTA.UI.NotificationIcon.AllPlayersConf, "RAGECOOP", "Welcome!", "Press ~g~F9~s~ to open the menu.");
#endif
            }

#if !NON_INTERACTIVE
            MainMenu.MenuPool.Process();
#endif
            

            if (_isGoingToCar && Game.Player.Character.IsInVehicle())
            {
                _isGoingToCar = false;
            }
            DoQueuedActions();
            if (!MainNetworking.IsOnServer())
            {
                return;
            }
            if (Game.TimeScale!=1)
            {
                Game.TimeScale=1;
            }
            try
            {
                MainNetworking.Tick();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            
            if (!DownloadManager.DownloadComplete)
            {
                DownloadManager.RenderProgress();
            }

            MapLoader.LoadAll();

#if DEBUG
            if (MainNetworking.ShowNetworkInfo)
            {
                ulong time = Util.GetTickCount64();
                if (time - _lastDebugData > 1000)
                {
                    _lastDebugData = time;

                    _debugBytesReceived = MainNetworking.BytesReceived;
                    MainNetworking.BytesReceived = 0;
                    _debugBytesSend = MainNetworking.BytesSend;
                    MainNetworking.BytesSend = 0;
                }

                new LemonUI.Elements.ScaledText(new PointF(Screen.PrimaryScreen.Bounds.Width / 2, 0), $"L: {MainNetworking.Latency * 1000:N0}ms", 0.5f) { Alignment = GTA.UI.Alignment.Center }.Draw();
                new LemonUI.Elements.ScaledText(new PointF(Screen.PrimaryScreen.Bounds.Width / 2, 30), $"R: {Lidgren.Network.NetUtility.ToHumanReadable(_debugBytesReceived)}/s", 0.5f) { Alignment = GTA.UI.Alignment.Center }.Draw();
                new LemonUI.Elements.ScaledText(new PointF(Screen.PrimaryScreen.Bounds.Width / 2, 60), $"S: {Lidgren.Network.NetUtility.ToHumanReadable(_debugBytesSend)}/s", 0.5f) { Alignment = GTA.UI.Alignment.Center }.Draw();
            }
#endif



            MainChat.Tick();
            MainPlayerList.Tick();

#if DEBUG
            if (UseDebug)
            {
                Debug();
            }
#endif



            Ticked++;
        }

#if !NON_INTERACTIVE
        bool _lastEnteringVeh=false;
        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (MainChat.Focused)
            {
                MainChat.OnKeyDown(e.KeyCode);
                return;
            }
            if (Game.IsControlPressed(GTA.Control.FrontendPause))
            {
                Function.Call(Hash.ACTIVATE_FRONTEND_MENU, Function.Call<int>(Hash.GET_HASH_KEY, "FE_MENU_VERSION_SP_PAUSE"), false, 0);
                return;
            }

            switch (e.KeyCode)
            {
                case Keys.F9:
                    if (MainMenu.MenuPool.AreAnyVisible)
                    {
                        MainMenu.MainMenu.Visible = false;
                        MainMenu.SubSettings.MainMenu.Visible = false;
                    }
                    else
                    {
                        MainMenu.MainMenu.Visible = true;
                    }
                    break;
                case Keys.J:
                    Game.Player.Character.CurrentVehicle.ApplyForce(new Vector3(0, 0, 100));
                    Script.Yield();
                    GTA.UI.Notification.Show(Game.Player.Character.CurrentVehicle.Speed.ToString());
                    break;
                default:
                    if (Game.IsControlJustPressed(GTA.Control.MultiplayerInfo))
                    {
                        if (MainNetworking.IsOnServer())
                        {
                            ulong currentTimestamp = Util.GetTickCount64();
                            MainPlayerList.Pressed = (currentTimestamp - MainPlayerList.Pressed) < 5000 ? (currentTimestamp - 6000) : currentTimestamp;
                        }
                    }
                    else if (Game.IsControlJustPressed(GTA.Control.MpTextChatAll))
                    {
                        if (MainNetworking.IsOnServer())
                        {
                            MainChat.Focused = true;
                        }
                    }
                    break;
            }


            if (e.KeyCode==Keys.L)
            {
                GTA.UI.Notification.Show(DumpCharacters());
            }
            if (e.KeyCode==Keys.I)
            {
                GTA.UI.Notification.Show(DumpPlayers());
            }
            if (e.KeyCode==Keys.U)
            {
                Debug.ShowTimeStamps();
            }
            if (e.KeyCode==Keys.G)
            {
                var P = Game.Player.Character;
                if (P.IsInVehicle())
                {
                    _lastEnteringVeh=false;
                    P.Task.LeaveVehicle();
                }
                else
                {
                    var V = World.GetClosestVehicle(P.Position, 50);

                    if (_lastEnteringVeh)
                    {
                        P.Task.ClearAllImmediately();
                        
                        _lastEnteringVeh = false;
                    }
                    else if (V!=null)
                    {
                        var seat = Util.getNearestSeat(P, V);
                        P.Task.EnterVehicle(V, seat);
                        _lastEnteringVeh=true;
                    }
                }
            }
        }
#else
        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (MainChat.Focused)
            {
                MainChat.OnKeyDown(e.KeyCode);
                return;
            }

            if (Game.IsControlJustPressed(GTA.Control.MultiplayerInfo))
            {
                if (MainNetworking.IsOnServer())
                {
                    ulong currentTimestamp = Util.GetTickCount64();
                    PlayerList.Pressed = (currentTimestamp - PlayerList.Pressed) < 5000 ? (currentTimestamp - 6000) : currentTimestamp;
                }
            }
            else if (Game.IsControlJustPressed(GTA.Control.MpTextChatAll))
            {
                if (MainNetworking.IsOnServer())
                {
                    MainChat.Focused = true;
                }
            }
        }
#endif

        public static void CleanUp()
        {

            MainChat.Clear();
            EntityPool.Cleanup();
            MainPlayerList=new PlayerList();

            Main.MyPlayerID=default;

        }

        public static readonly Dictionary<ulong, byte> CheckNativeHash = new Dictionary<ulong, byte>()
        {
            { 0xD49F9B0955C367DE, 1 }, // Entities
            { 0xEF29A16337FACADB, 1 }, //
            { 0xB4AC7D0CF06BFE8F, 1 }, //
            { 0x9B62392B474F44A0, 1 }, //
            { 0x7DD959874C1FD534, 1 }, //
            { 0xAF35D0D2583051B0, 2 }, // Vehicles
            { 0x63C6CCA8E68AE8C8, 2 }, //
            { 0x509D5878EB39E842, 3 }, // Props
            { 0x9A294B2138ABB884, 3 }, //
            { 0x46818D79B1F7499A, 4 }, // Blips
            { 0x5CDE92C702A8FCE7, 4 }, //
            { 0xBE339365C863BD36, 4 }, //
            { 0x5A039BB0BCA604B6, 4 }, //
            { 0x0134F0835AB6BFCB, 5 }  // Checkpoints
        };
        public static Dictionary<int, byte> ServerItems = new Dictionary<int, byte>();
        public static void CleanUpWorld()
        {
            if (ServerItems.Count == 0)
            {
                return;
            }

            lock (ServerItems)
            {
                foreach (KeyValuePair<int, byte> item in ServerItems)
                {
                    try
                    {
                        switch (item.Value)
                        {
                            case 1:
                                World.GetAllEntities().FirstOrDefault(x => x.Handle == item.Key)?.Delete();
                                break;
                            case 2:
                                World.GetAllVehicles().FirstOrDefault(x => x.Handle == item.Key)?.Delete();
                                break;
                            case 3:
                                World.GetAllProps().FirstOrDefault(x => x.Handle == item.Key)?.Delete();
                                break;
                            case 4:
                                Blip blip = new Blip(item.Key);
                                if (blip.Exists())
                                {
                                    blip.Delete();
                                }
                                break;
                            case 5:
                                Checkpoint checkpoint = new Checkpoint(item.Key);
                                if (checkpoint.Exists())
                                {
                                    checkpoint.Delete();
                                }
                                break;
                        }
                    }
                    catch
                    {
                        GTA.UI.Notification.Show("~r~~h~CleanUpWorld() Error");
                        Logger.Error($"CleanUpWorld(): ~r~Item {item.Value} cannot be deleted!");
                    }
                }

                ServerItems.Clear();
            }
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
                    catch(Exception ex)
                    {
                        GTA.UI.Screen.ShowSubtitle(ex.ToString());
                        QueuedActions.Remove(action);
                    }
                }
            }
        }

        /// <summary>
        /// Queue an action  to be executed on next tick, allowing you to call scripting API from another thread.
        /// </summary>
        /// <param name="a"> The action to be executed, must return a bool indicating whether the action cane be removed after execution.</param>
        public static void QueueAction(Func<bool> a)
        {
            lock (QueuedActions)
            {
                QueuedActions.Add(a);
            }
        }
        public static void QueueAction(Action a)
        {
            lock (QueuedActions)
            {
                QueuedActions.Add(() => { a(); return true; }) ;
            }
        }
        /// <summary>
        /// Clears all queued actions
        /// </summary>
        public static void ClearQueuedActions()
        {
            lock (QueuedActions) { QueuedActions.Clear(); }
        }

        public static string DumpCharacters()
        {
            string s = "Characters:";
            lock (EntityPool.PedsLock)
            {
                foreach (int id in EntityPool.GetPedIDs())
                {
                    var c = EntityPool.GetPedByID(id);
                    s+=$"\r\nID:{c.ID} Owner:{c.OwnerID} LastUpdated:{c.LastUpdated} LastSynced:{c.LastSynced} LastStateSynced:{c.LastStateSynced}";
                    // s+=$"\r\n{c.IsAiming} {c.IsJumping} {c.IsOnFire} {c.IsOnLadder} {c.IsRagdoll} {c.IsReloading} {c.IsShooting} {c.Speed}";
                }
            }
            Logger.Trace(s);
            return s;
        }
        public static string DumpPlayers()
        {
            string s = "Players:";
            foreach (PlayerData p in MainPlayerList.Players)
            {
                
                s+=$"\r\nID:{p.PedID} Username:{p.Username}";
            }
            Logger.Trace(s);
            return s;
        }

#if DEBUG
        private ulong _artificialLagCounter;
        public static EntitiesPlayer DebugSyncPed;
        public static ulong LastFullDebugSync = 0;
        public static bool UseDebug = false;

        private void Debug()
        {
            Ped player = Game.Player.Character;

            if (!Players.ContainsKey(0))
            {
                Players.Add(0, new EntitiesPlayer() { Username = "DebugPlayer" });
                DebugSyncPed = Players[0];
            }

            if ((Util.GetTickCount64() - _artificialLagCounter) < 147)
            {
                return;
            }

            bool fullSync = (Util.GetTickCount64() - LastFullDebugSync) > 39;

            if (fullSync)
            {
                DebugSyncPed.ModelHash = player.Model.Hash;
                DebugSyncPed.Clothes = player.GetPedClothes();
            }
            DebugSyncPed.Health = player.Health;
            DebugSyncPed.Position = player.Position;

            ushort? flags;

            if (player.IsInVehicle())
            {
                Vehicle veh = player.CurrentVehicle;
                veh.Opacity = 75;

                flags = player.GetVehicleFlags(veh);

                byte secondaryColor;
                byte primaryColor;
                unsafe
                {
                    Function.Call<byte>(Hash.GET_VEHICLE_COLOURS, veh, &primaryColor, &secondaryColor);
                }

                DebugSyncPed.VehicleModelHash = veh.Model.Hash;
                DebugSyncPed.VehicleSeatIndex = (short)player.SeatIndex;
                DebugSyncPed.Position = veh.Position;
                DebugSyncPed.VehicleRotation = veh.Quaternion;
                DebugSyncPed.VehicleEngineHealth = veh.EngineHealth;
                DebugSyncPed.VehRPM = veh.CurrentRPM;
                DebugSyncPed.Velocity = veh.Velocity;
                DebugSyncPed.VehicleSpeed = veh.Speed;
                DebugSyncPed.VehicleSteeringAngle = veh.SteeringAngle;
                DebugSyncPed.AimCoords = veh.IsTurretSeat((int)player.SeatIndex) ? Util.GetVehicleAimCoords() : new GTA.Math.Vector3();
                DebugSyncPed.VehicleColors = new byte[] { primaryColor, secondaryColor };
                DebugSyncPed.VehicleMods = veh.Mods.GetVehicleMods();
                DebugSyncPed.VehDamageModel = veh.GetVehicleDamageModel();
                DebugSyncPed.LastSyncWasFull = true;
                DebugSyncPed.IsInVehicle = true;
                DebugSyncPed.VehIsEngineRunning = (flags.Value & (ushort)VehicleDataFlags.IsEngineRunning) > 0;
                DebugSyncPed.VehAreLightsOn = (flags.Value & (ushort)VehicleDataFlags.AreLightsOn) > 0;
                DebugSyncPed.VehAreBrakeLightsOn = (flags.Value & (ushort)VehicleDataFlags.AreBrakeLightsOn) > 0;
                DebugSyncPed.VehAreHighBeamsOn = (flags.Value & (ushort)VehicleDataFlags.AreHighBeamsOn) > 0;
                DebugSyncPed.VehIsSireneActive = (flags.Value & (ushort)VehicleDataFlags.IsSirenActive) > 0;
                DebugSyncPed.VehicleDead = (flags.Value & (ushort)VehicleDataFlags.IsDead) > 0;
                DebugSyncPed.IsHornActive = (flags.Value & (ushort)VehicleDataFlags.IsHornActive) > 0;
                DebugSyncPed.Transformed = (flags.Value & (ushort)VehicleDataFlags.IsTransformed) > 0;
                DebugSyncPed.VehRoofOpened = (flags.Value & (ushort)VehicleDataFlags.RoofOpened) > 0;
                DebugSyncPed.VehLandingGear = veh.IsPlane ? (byte)veh.LandingGearState : (byte)0;
                
                if (DebugSyncPed.MainVehicle != null && DebugSyncPed.MainVehicle.Exists() && player.IsInVehicle())
                {
                    Function.Call(Hash.SET_ENTITY_NO_COLLISION_ENTITY, DebugSyncPed.MainVehicle.Handle, veh.Handle, false);
                    Function.Call(Hash.SET_ENTITY_NO_COLLISION_ENTITY, veh.Handle, DebugSyncPed.MainVehicle.Handle, false);
                }
            }
            else
            {
                flags = player.GetPedFlags(true);

                DebugSyncPed.Rotation = player.Rotation;
                DebugSyncPed.Velocity = player.Velocity;
                DebugSyncPed.Speed = player.GetPedSpeed();
                DebugSyncPed.AimCoords = player.GetPedAimCoords(false);
                DebugSyncPed.CurrentWeaponHash = (uint)player.Weapons.Current.Hash;
                DebugSyncPed.WeaponComponents = player.Weapons.Current.GetWeaponComponents();
                DebugSyncPed.LastSyncWasFull = true;
                DebugSyncPed.IsAiming = (flags.Value & (ushort)PedDataFlags.IsAiming) > 0;
                DebugSyncPed.IsShooting = (flags.Value & (ushort)PedDataFlags.IsShooting) > 0;
                DebugSyncPed.IsReloading = (flags.Value & (ushort)PedDataFlags.IsReloading) > 0;
                DebugSyncPed.IsJumping = (flags.Value & (ushort)PedDataFlags.IsJumping) > 0;
                DebugSyncPed.IsRagdoll = (flags.Value & (ushort)PedDataFlags.IsRagdoll) > 0;
                DebugSyncPed.IsOnFire = (flags.Value & (ushort)PedDataFlags.IsOnFire) > 0;
                DebugSyncPed.IsInParachuteFreeFall = (flags.Value & (ushort)PedDataFlags.IsInParachuteFreeFall) > 0;
                DebugSyncPed.IsParachuteOpen = (flags.Value & (ushort)PedDataFlags.IsParachuteOpen) > 0;
                DebugSyncPed.IsOnLadder = (flags.Value & (ushort)PedDataFlags.IsOnLadder) > 0;
                DebugSyncPed.IsVaulting = (flags.Value & (ushort)PedDataFlags.IsVaulting) > 0;
                DebugSyncPed.IsInVehicle = false;

                if (DebugSyncPed.Character != null && DebugSyncPed.Character.Exists())
                {
                    Function.Call(Hash.SET_ENTITY_NO_COLLISION_ENTITY, DebugSyncPed.Character.Handle, player.Handle, false);
                    Function.Call(Hash.SET_ENTITY_NO_COLLISION_ENTITY, player.Handle, DebugSyncPed.Character.Handle, false);
                }
            }

            ulong currentTimestamp = Util.GetTickCount64();

            DebugSyncPed.LastUpdateReceived = currentTimestamp;
            DebugSyncPed.Latency = (currentTimestamp - _artificialLagCounter) / 1000f;

            _artificialLagCounter = currentTimestamp;

            if (fullSync)
            {
                LastFullDebugSync = currentTimestamp;
            }
        }
#endif
    }
}
