using System;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Drawing;

using CoopClient.Entities;
using CoopClient.Menus;

using GTA;
using GTA.Native;

namespace CoopClient
{
    public class Main : Script
    {
        public static RelationshipGroup RelationshipGroup;

        private bool GameLoaded = false;

        public static readonly string CurrentVersion = "V0_8_0_1";

        public static bool ShareNpcsWithPlayers = false;
        public static bool DisableTraffic = false;
        public static bool NpcsAllowed = false;
        private static bool IsGoingToCar = false;

        public static Settings MainSettings = Util.ReadSettings();
        public static Networking MainNetworking = new Networking();

#if !NON_INTERACTIVE
        public static MenusMain MainMenu = new MenusMain();
#endif
        public static Chat MainChat = new Chat();

        public static long LocalClientID = 0;
        public static readonly Dictionary<long, EntitiesPlayer> Players = new Dictionary<long, EntitiesPlayer>();
        public static readonly Dictionary<long, EntitiesNpc> Npcs = new Dictionary<long, EntitiesNpc>();

        public Main()
        {
            Function.Call((Hash)0x0888C3502DBBEEF5); // _LOAD_MP_DLC_MAPS
            Function.Call((Hash)0x9BAE5AD2508DF078, true); // _ENABLE_MP_DLC_MAPS

            Tick += OnTick;
#if !NON_INTERACTIVE
            KeyDown += OnKeyDown;
#endif
            Aborted += (object sender, EventArgs e) => CleanUp();

            Util.NativeMemory();
        }

        private ulong LastDataSend;
        private void OnTick(object sender, EventArgs e)
        {
            if (Game.IsLoading)
            {
                return;
            }
            else if (!GameLoaded && (GameLoaded = true))
            {
                RelationshipGroup = World.AddRelationshipGroup("SYNCPED");
                Game.Player.Character.RelationshipGroup = RelationshipGroup;
            }

#if !NON_INTERACTIVE
            MainMenu.MenuPool.Process();
#endif

            MainNetworking.ReceiveMessages();

            if (IsGoingToCar && Game.Player.Character.IsInVehicle())
            {
                IsGoingToCar = false;
            }

            if (!MainNetworking.IsOnServer())
            {
                return;
            }

#if DEBUG
            if (MainNetworking.ShowNetworkInfo)
            {
                new LemonUI.Elements.ScaledText(new PointF(Screen.PrimaryScreen.Bounds.Width / 2, 0), $"L: {MainNetworking.Latency * 1000:N0}ms", 0.5f) { Alignment = GTA.UI.Alignment.Center }.Draw();
                new LemonUI.Elements.ScaledText(new PointF(Screen.PrimaryScreen.Bounds.Width / 2, 30), $"R: {MainNetworking.BytesReceived * 0.000001} mb", 0.5f) { Alignment = GTA.UI.Alignment.Center }.Draw();
                new LemonUI.Elements.ScaledText(new PointF(Screen.PrimaryScreen.Bounds.Width / 2, 60), $"S: {MainNetworking.BytesSend * 0.000001} mb", 0.5f) { Alignment = GTA.UI.Alignment.Center }.Draw();
            }
#endif

            MainChat.Tick();

            // Display all players
            foreach (KeyValuePair<long, EntitiesPlayer> player in Players)
            {
                player.Value.DisplayLocally(player.Value.Username);
            }

#if DEBUG
            if (UseDebug)
            {
                Debug();
            }
#endif

            if ((Util.GetTickCount64() - LastDataSend) < ((ulong)(1f / (Game.FPS > 60f ? 60f : Game.FPS)  * 1000f)))
            {
                return;
            }

            MainNetworking.SendPlayerData();

            LastDataSend = Util.GetTickCount64();
        }

#if !NON_INTERACTIVE
        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (MainChat.Focused)
            {
                MainChat.OnKeyDown(e.KeyCode);
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
                case Keys.G:
                    if (IsGoingToCar)
                    {
                        Game.Player.Character.Task.ClearAll();
                        IsGoingToCar = false;
                    }
                    else if (!Game.Player.Character.IsInVehicle())
                    {
                        Vehicle veh = World.GetNearbyVehicles(Game.Player.Character, 5f).FirstOrDefault();
                        if (veh != default)
                        {
                            for (int i = 0; i < veh.PassengerCapacity; i++)
                            {
                                if (veh.IsSeatFree((VehicleSeat)i))
                                {
                                    Game.Player.Character.Task.EnterVehicle(veh, (VehicleSeat)i);
                                    IsGoingToCar = true;
                                    break;
                                }
                            }
                        }
                    }
                    break;
                default:
                    if (Game.IsControlJustPressed(GTA.Control.MultiplayerInfo))
                    {
                        if (MainNetworking.IsOnServer())
                        {
                            ulong currentTimestamp = Util.GetTickCount64();
                            PlayerList.Pressed = (currentTimestamp - PlayerList.Pressed) < 5000 ? (currentTimestamp - 6000) : currentTimestamp;
                        }
                        return;
                    }
                    else if (Game.IsControlJustPressed(GTA.Control.MpTextChatAll))
                    {
                        if (MainNetworking.IsOnServer())
                        {
                            MainChat.Focused = true;
                        }
                        return;
                    }
                    break;
            }
        }
#endif

        public static void CleanUp()
        {
            MainChat.Clear();

            foreach (KeyValuePair<long, EntitiesPlayer> player in Players)
            {
                player.Value.Character?.AttachedBlip?.Delete();
                player.Value.Character?.CurrentVehicle?.Delete();
                player.Value.Character?.Kill();
                player.Value.Character?.Delete();
                player.Value.PedBlip?.Delete();
            }
            Players.Clear();

            foreach (KeyValuePair<long, EntitiesNpc> Npc in Npcs)
            {
                Npc.Value.Character?.CurrentVehicle?.Delete();
                Npc.Value.Character?.Kill();
                Npc.Value.Character?.Delete();
            }
            Npcs.Clear();

            foreach (Ped entity in World.GetAllPeds().Where(p => p.Handle != Game.Player.Character.Handle))
            {
                entity.Kill();
                entity.Delete();
            }

            foreach (Vehicle veh in World.GetAllVehicles().Where(v => v.Handle != Game.Player.Character.CurrentVehicle?.Handle))
            {
                veh.Delete();
            }
        }

#if DEBUG
        private ulong ArtificialLagCounter;
        public static EntitiesPlayer DebugSyncPed;
        public static ulong LastFullDebugSync = 0;
        public static bool UseDebug = false;

        private void Debug()
        {
            Ped player = Game.Player.Character;
            if (!Players.ContainsKey(0))
            {
                Players.Add(0, new EntitiesPlayer() { SocialClubName = "DEBUG", Username = "DebugPlayer" });
                DebugSyncPed = Players[0];
            }

            if ((Util.GetTickCount64() - ArtificialLagCounter) < 157)
            {
                return;
            }

            bool fullSync = (Util.GetTickCount64() - LastFullDebugSync) > 1500;

            if (fullSync)
            {
                DebugSyncPed.ModelHash = player.Model.Hash;
                DebugSyncPed.Props = player.GetPedProps();
            }
            DebugSyncPed.Health = player.Health;
            DebugSyncPed.Position = player.Position;

            byte? flags;

            Vehicle vehicleTryingToEnter = null;

            if (player.IsInVehicle() || (vehicleTryingToEnter = player.VehicleTryingToEnter) != null)
            {
                Vehicle veh = player.CurrentVehicle ?? vehicleTryingToEnter;
                veh.Opacity = 75;

                flags = veh.GetVehicleFlags(fullSync);

                int secondaryColor;
                int primaryColor;
                unsafe
                {
                    Function.Call<int>(Hash.GET_VEHICLE_COLOURS, veh, &primaryColor, &secondaryColor);
                }

                DebugSyncPed.VehicleModelHash = veh.Model.Hash;
                DebugSyncPed.VehicleSeatIndex = (int)player.SeatIndex;
                DebugSyncPed.VehiclePosition = veh.Position;
                DebugSyncPed.VehicleRotation = veh.Quaternion;
                DebugSyncPed.VehicleEngineHealth = veh.EngineHealth;
                DebugSyncPed.VehRPM = veh.CurrentRPM;
                DebugSyncPed.VehicleVelocity = veh.Velocity;
                DebugSyncPed.VehicleSpeed = veh.Speed;
                DebugSyncPed.VehicleSteeringAngle = veh.SteeringAngle;
                DebugSyncPed.VehicleColors = new int[] { primaryColor, secondaryColor };
                DebugSyncPed.VehicleMods = veh.Mods.GetVehicleMods();
                DebugSyncPed.VehDoors = veh.Doors.GetVehicleDoors();
                DebugSyncPed.VehTires = veh.Wheels.GetBrokenTires();
                DebugSyncPed.LastSyncWasFull = (flags.Value & (byte)VehicleDataFlags.LastSyncWasFull) > 0;
                DebugSyncPed.IsInVehicle = true;
                DebugSyncPed.VehIsEngineRunning = (flags.Value & (byte)VehicleDataFlags.IsEngineRunning) > 0;
                DebugSyncPed.VehAreLightsOn = (flags.Value & (byte)VehicleDataFlags.AreLightsOn) > 0;
                DebugSyncPed.VehAreHighBeamsOn = (flags.Value & (byte)VehicleDataFlags.AreHighBeamsOn) > 0;
                DebugSyncPed.VehIsSireneActive = (flags.Value & (byte)VehicleDataFlags.IsSirenActive) > 0;
                DebugSyncPed.VehicleDead = (flags.Value & (byte)VehicleDataFlags.IsDead) > 0;

                if (DebugSyncPed.MainVehicle != null && DebugSyncPed.MainVehicle.Exists() && player.IsInVehicle())
                {
                    Function.Call(Hash.SET_ENTITY_NO_COLLISION_ENTITY, DebugSyncPed.MainVehicle.Handle, veh.Handle, false);
                    Function.Call(Hash.SET_ENTITY_NO_COLLISION_ENTITY, veh.Handle, DebugSyncPed.MainVehicle.Handle, false);
                }
            }
            else
            {
                flags = player.GetPedFlags(fullSync, true);

                DebugSyncPed.Rotation = player.Rotation;
                DebugSyncPed.Velocity = player.Velocity;
                DebugSyncPed.Speed = player.GetPedSpeed();
                DebugSyncPed.AimCoords = player.GetPedAimCoords(false);
                DebugSyncPed.CurrentWeaponHash = (int)player.Weapons.Current.Hash;
                DebugSyncPed.LastSyncWasFull = (flags.Value & (byte)PedDataFlags.LastSyncWasFull) > 0;
                DebugSyncPed.IsAiming = (flags.Value & (byte)PedDataFlags.IsAiming) > 0;
                DebugSyncPed.IsShooting = (flags.Value & (byte)PedDataFlags.IsShooting) > 0;
                DebugSyncPed.IsReloading = (flags.Value & (byte)PedDataFlags.IsReloading) > 0;
                DebugSyncPed.IsJumping = (flags.Value & (byte)PedDataFlags.IsJumping) > 0;
                DebugSyncPed.IsRagdoll = (flags.Value & (byte)PedDataFlags.IsRagdoll) > 0;
                DebugSyncPed.IsOnFire = (flags.Value & (byte)PedDataFlags.IsOnFire) > 0;
                DebugSyncPed.IsInVehicle = false;

                if (DebugSyncPed.Character != null && DebugSyncPed.Character.Exists())
                {
                    Function.Call(Hash.SET_ENTITY_NO_COLLISION_ENTITY, DebugSyncPed.Character.Handle, player.Handle, false);
                    Function.Call(Hash.SET_ENTITY_NO_COLLISION_ENTITY, player.Handle, DebugSyncPed.Character.Handle, false);
                }
            }

            ulong currentTimestamp = Util.GetTickCount64();

            DebugSyncPed.LastUpdateReceived = currentTimestamp;
            DebugSyncPed.Latency = currentTimestamp - ArtificialLagCounter;

            ArtificialLagCounter = currentTimestamp;

            if (fullSync)
            {
                LastFullDebugSync = currentTimestamp;
            }
        }
#endif
    }
}
