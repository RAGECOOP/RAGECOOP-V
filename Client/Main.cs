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
    /// <summary>
    /// Don't use it!
    /// </summary>
    public class Main : Script
    {
        internal static RelationshipGroup RelationshipGroup;

        private bool GameLoaded = false;

        internal static readonly string CurrentVersion = "V1_3_0";

        internal static bool ShareNPCsWithPlayers = false;
        internal static bool DisableTraffic = false;
        internal static bool NPCsAllowed = false;
        private static bool IsGoingToCar = false;

        internal static Settings MainSettings = null;
        internal static Networking MainNetworking = null;

#if !NON_INTERACTIVE
        internal static MenusMain MainMenu = null;
#endif
        internal static Chat MainChat = null;

        internal static long LocalNetHandle = 0;
        internal static Dictionary<long, EntitiesPlayer> Players = null;
        internal static Dictionary<long, EntitiesPed> NPCs = null;
        internal static Dictionary<long, int> NPCsVehicles = null;

        /// <summary>
        /// Don't use it!
        /// </summary>
        public Main()
        {
            // Required for some synchronization!
            if (Game.Version < GameVersion.v1_0_1290_1_Steam)
            {
                Tick += (object sender, EventArgs e) =>
                {
                    if (Game.IsLoading)
                    {
                        return;
                    }

                    if (!GameLoaded)
                    {
                        GTA.UI.Notification.Show("~r~Please update your GTA5 to v1.0.1290 or newer!", true);
                        GameLoaded = true;
                    }
                };
                return;
            }

            MainSettings = Util.ReadSettings();
            MainNetworking = new Networking();
#if !NON_INTERACTIVE
            MainMenu = new MenusMain();
#endif
            MainChat = new Chat();
            Players = new Dictionary<long, EntitiesPlayer>();
            NPCs = new Dictionary<long, EntitiesPed>();
            NPCsVehicles = new Dictionary<long, int>();

            Tick += OnTick;
#if !NON_INTERACTIVE
            KeyDown += OnKeyDown;
#endif
            Aborted += (object sender, EventArgs e) => CleanUp();

            Util.NativeMemory();
        }

        private ulong LastDataSend;
#if DEBUG
        private ulong LastDebugData;
        private int DebugBytesSend;
        private int DebugBytesReceived;
#endif
        private void OnTick(object sender, EventArgs e)
        {
            if (Game.IsLoading)
            {
                return;
            }
            else if (!GameLoaded && (GameLoaded = true))
            {
                RelationshipGroup = World.AddRelationshipGroup("SYNCPED");
                Game.Player.Character.RelationshipGroup.SetRelationshipBetweenGroups(RelationshipGroup, Relationship.Neutral, true);
#if !NON_INTERACTIVE
                GTA.UI.Notification.Show(GTA.UI.NotificationIcon.AllPlayersConf, "GTACOOP:R", "Welcome!", "Press ~g~F9~s~ to open the menu.");
#endif
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
                ulong time = Util.GetTickCount64();
                if (time - LastDebugData > 1000)
                {
                    LastDebugData = time;

                    DebugBytesReceived = MainNetworking.BytesReceived;
                    MainNetworking.BytesReceived = 0;
                    DebugBytesSend = MainNetworking.BytesSend;
                    MainNetworking.BytesSend = 0;
                }

                new LemonUI.Elements.ScaledText(new PointF(Screen.PrimaryScreen.Bounds.Width / 2, 0), $"L: {MainNetworking.Latency * 1000:N0}ms", 0.5f) { Alignment = GTA.UI.Alignment.Center }.Draw();
                new LemonUI.Elements.ScaledText(new PointF(Screen.PrimaryScreen.Bounds.Width / 2, 30), $"R: {Lidgren.Network.NetUtility.ToHumanReadable(DebugBytesReceived)}/s", 0.5f) { Alignment = GTA.UI.Alignment.Center }.Draw();
                new LemonUI.Elements.ScaledText(new PointF(Screen.PrimaryScreen.Bounds.Width / 2, 60), $"S: {Lidgren.Network.NetUtility.ToHumanReadable(DebugBytesSend)}/s", 0.5f) { Alignment = GTA.UI.Alignment.Center }.Draw();
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

            if ((Util.GetTickCount64() - LastDataSend) < Util.GetGameMs<ulong>())
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
                        if (veh != null)
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

        internal static void CleanUp()
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

            foreach (KeyValuePair<long, EntitiesPed> npc in NPCs)
            {
                npc.Value.Character?.CurrentVehicle?.Delete();
                npc.Value.Character?.Kill();
                npc.Value.Character?.Delete();
            }
            NPCs.Clear();

            NPCsVehicles.Clear();
        }

#if DEBUG
        private ulong ArtificialLagCounter;
        internal static EntitiesPlayer DebugSyncPed;
        internal static ulong LastFullDebugSync = 0;
        internal static bool UseDebug = false;

        private void Debug()
        {
            Ped player = Game.Player.Character;

            if (!Players.ContainsKey(0))
            {
                Players.Add(0, new EntitiesPlayer() { Username = "DebugPlayer" });
                DebugSyncPed = Players[0];
            }

            if ((Util.GetTickCount64() - ArtificialLagCounter) < 56)
            {
                return;
            }

            bool fullSync = (Util.GetTickCount64() - LastFullDebugSync) > 500;

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
                DebugSyncPed.VehicleVelocity = veh.Velocity;
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
