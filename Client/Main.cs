using System;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;

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

        public static readonly string CurrentModVersion = "V0_5_0";

        public static bool ShareNpcsWithPlayers = false;
        public static bool NpcsAllowed = false;
        private static bool IsGoingToCar = false;

        public static Settings MainSettings = Util.ReadSettings();

        public static MenusMain MainMenu = new MenusMain();
        public static Chat MainChat = new Chat();

        public static Networking MainNetworking = new Networking();

        public static string LocalPlayerID = null;
        public static readonly Dictionary<string, EntitiesPlayer> Players = new Dictionary<string, EntitiesPlayer>();
        public static readonly Dictionary<string, EntitiesNpc> Npcs = new Dictionary<string, EntitiesNpc>();

        public Main()
        {
            Function.Call((Hash)0x0888C3502DBBEEF5); // _LOAD_MP_DLC_MAPS
            Function.Call((Hash)0x9BAE5AD2508DF078, true); // _ENABLE_MP_DLC_MAPS

            Tick += OnTick;
            KeyDown += OnKeyDown;
            Aborted += OnAbort;

            Util.NativeMemory();
        }

        private int LastDataSend;
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

            MainMenu.MenuPool.Process();

            MainNetworking.ReceiveMessages();

            if (IsGoingToCar && Game.Player.Character.IsInVehicle())
            {
                IsGoingToCar = false;
            }

            if (!MainNetworking.IsOnServer())
            {
                return;
            }

            MainChat.Tick();

            // Display all players
            foreach (KeyValuePair<string, EntitiesPlayer> player in Players)
            {
                player.Value.DisplayLocally(player.Value.Username);
            }

            if (UseDebug)
            {
                Debug();
            }

            if ((Environment.TickCount - LastDataSend) >= (1000 / 60))
            {
                MainNetworking.SendPlayerData();

                LastDataSend = Environment.TickCount;
            }
        }

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
                case Keys.T:
                    if (MainNetworking.IsOnServer())
                    {
                        MainChat.Focused = true;
                    }
                    break;
                case Keys.Y:
                    if (MainNetworking.IsOnServer())
                    {
                        int currentTimestamp = Environment.TickCount;
                        PlayerList.Pressed = (currentTimestamp - PlayerList.Pressed) < 5000 ? (currentTimestamp - 6000) : currentTimestamp;
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
                        Vehicle veh = World.GetNearbyVehicles(Game.Player.Character, 5f).First();
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
            }
        }

        private void OnAbort(object sender, EventArgs e)
        {
            foreach (KeyValuePair<string, EntitiesPlayer> player in Players)
            {
                player.Value.Character?.AttachedBlip?.Delete();
                player.Value.Character?.CurrentVehicle?.Delete();
                player.Value.Character?.Kill();
                player.Value.Character?.Delete();
                player.Value.PedBlip?.Delete();
            }

            foreach (KeyValuePair<string, EntitiesNpc> Npc in Npcs)
            {
                Npc.Value.Character?.CurrentVehicle?.Delete();
                Npc.Value.Character?.Kill();
                Npc.Value.Character?.Delete();
            }
        }

        public static void CleanUp()
        {
            foreach (KeyValuePair<string, EntitiesPlayer> player in Players)
            {
                player.Value.Character?.AttachedBlip?.Delete();
                player.Value.Character?.CurrentVehicle?.Delete();
                player.Value.Character?.Kill();
                player.Value.Character?.Delete();
                player.Value.PedBlip?.Delete();
            }
            Players.Clear();

            foreach (KeyValuePair<string, EntitiesNpc> Npc in Npcs)
            {
                Npc.Value.Character?.CurrentVehicle?.Delete();
                Npc.Value.Character?.Kill();
                Npc.Value.Character?.Delete();
            }
            Npcs.Clear();

            foreach (Ped entity in World.GetAllPeds())
            {
                if (entity.Handle != Game.Player.Character.Handle)
                {
                    entity.Kill();
                    entity.Delete();
                }
            }

            if (!Game.Player.Character.IsInVehicle())
            {
                foreach (Vehicle vehicle in World.GetAllVehicles())
                {
                    vehicle.Delete();
                }
            }
            else
            {
                int? playerVehicleHandle = Game.Player.Character.CurrentVehicle?.Handle;

                foreach (Vehicle vehicle in World.GetAllVehicles())
                {
                    if (playerVehicleHandle != vehicle.Handle)
                    {
                        vehicle.Delete();
                    }
                }
            }
        }

        private int ArtificialLagCounter;
        public static EntitiesPlayer DebugSyncPed;
        public static int LastFullDebugSync = 0;
        public static bool UseDebug = false;

        private void Debug()
        {
            Ped player = Game.Player.Character;
            if (!Players.ContainsKey("DebugKey"))
            {
                Players.Add("DebugKey", new EntitiesPlayer() { SocialClubName = "DEBUG", Username = "DebugPlayer" });
                DebugSyncPed = Players["DebugKey"];
            }

            if ((Environment.TickCount - ArtificialLagCounter) < 37)
            {
                return;
            }

            bool fullSync = (Environment.TickCount - LastFullDebugSync) > 1500;

            if (fullSync)
            {
                DebugSyncPed.ModelHash = player.Model.Hash;
                DebugSyncPed.Props = Util.GetPedProps(player);
            }
            DebugSyncPed.Health = player.Health;
            DebugSyncPed.Position = player.Position;

            byte? flags;

            if (!player.IsInVehicle())
            {
                flags = Util.GetPedFlags(player, fullSync, true);

                DebugSyncPed.Rotation = player.Rotation;
                DebugSyncPed.Velocity = player.Velocity;
                DebugSyncPed.Speed = Util.GetPedSpeed(player);
                DebugSyncPed.AimCoords = Util.GetPedAimCoords(player, false);
                DebugSyncPed.CurrentWeaponHash = (int)player.Weapons.Current.Hash;
                DebugSyncPed.LastSyncWasFull = (flags.Value & (byte)PedDataFlags.LastSyncWasFull) > 0;
                DebugSyncPed.IsAiming = (flags.Value & (byte)PedDataFlags.IsAiming) > 0;
                DebugSyncPed.IsShooting = (flags.Value & (byte)PedDataFlags.IsShooting) > 0;
                DebugSyncPed.IsReloading = (flags.Value & (byte)PedDataFlags.IsReloading) > 0;
                DebugSyncPed.IsJumping = (flags.Value & (byte)PedDataFlags.IsJumping) > 0;
                DebugSyncPed.IsRagdoll = (flags.Value & (byte)PedDataFlags.IsRagdoll) > 0;
                DebugSyncPed.IsOnFire = (flags.Value & (byte)PedDataFlags.IsOnFire) > 0;
                DebugSyncPed.IsInVehicle = (flags.Value & (byte)PedDataFlags.IsInVehicle) > 0;

                if (DebugSyncPed.Character != null && DebugSyncPed.Character.Exists())
                {
                    Function.Call(Hash.SET_ENTITY_NO_COLLISION_ENTITY, DebugSyncPed.Character.Handle, player.Handle, false);
                    Function.Call(Hash.SET_ENTITY_NO_COLLISION_ENTITY, player.Handle, DebugSyncPed.Character.Handle, false);
                }
            }
            else
            {
                Vehicle veh = player.CurrentVehicle;
                veh.Opacity = 75;

                flags = Util.GetVehicleFlags(player, veh, fullSync);

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
                DebugSyncPed.VehicleVelocity = veh.Velocity;
                DebugSyncPed.VehicleSpeed = veh.Speed;
                DebugSyncPed.VehicleSteeringAngle = veh.SteeringAngle;
                DebugSyncPed.VehicleColors = new int[] { primaryColor, secondaryColor };
                DebugSyncPed.VehicleMods = Util.GetVehicleMods(veh);
                DebugSyncPed.VehDoors = Util.GetVehicleDoors(veh.Doors);
                DebugSyncPed.LastSyncWasFull = (flags.Value & (byte)VehicleDataFlags.LastSyncWasFull) > 0;
                DebugSyncPed.IsInVehicle = (flags.Value & (byte)VehicleDataFlags.IsInVehicle) > 0;
                DebugSyncPed.VehIsEngineRunning = (flags.Value & (byte)VehicleDataFlags.IsEngineRunning) > 0;
                DebugSyncPed.VehAreLightsOn = (flags.Value & (byte)VehicleDataFlags.AreLightsOn) > 0;
                DebugSyncPed.VehAreHighBeamsOn = (flags.Value & (byte)VehicleDataFlags.AreHighBeamsOn) > 0;
                DebugSyncPed.VehIsInBurnout = (flags.Value & (byte)VehicleDataFlags.IsInBurnout) > 0;
                DebugSyncPed.VehIsSireneActive = (flags.Value & (byte)VehicleDataFlags.IsSirenActive) > 0;
                DebugSyncPed.VehicleDead = (flags.Value & (byte)VehicleDataFlags.IsDead) > 0;

                if (DebugSyncPed.MainVehicle != null && DebugSyncPed.MainVehicle.Exists() && player.IsInVehicle())
                {
                    Function.Call(Hash.SET_ENTITY_NO_COLLISION_ENTITY, DebugSyncPed.MainVehicle.Handle, player.CurrentVehicle.Handle, false);
                    Function.Call(Hash.SET_ENTITY_NO_COLLISION_ENTITY, player.CurrentVehicle.Handle, DebugSyncPed.MainVehicle.Handle, false);
                }
            }

            int currentTimestamp = Environment.TickCount;

            DebugSyncPed.LastUpdateReceived = currentTimestamp;
            DebugSyncPed.Latency = currentTimestamp - ArtificialLagCounter;

            ArtificialLagCounter = currentTimestamp;

            if (fullSync)
            {
                LastFullDebugSync = currentTimestamp;
            }
        }
    }
}
