using System;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;

using CoopClient.Entities;

using GTA;
using GTA.Native;

using LemonUI;
using LemonUI.Menus;

namespace CoopClient
{
    public class Main : Script
    {
        public static RelationshipGroup RelationshipGroup;

        private bool GameLoaded = false;

        public static readonly string CurrentModVersion = Enum.GetValues(typeof(ModVersion)).Cast<ModVersion>().Last().ToString();

        public static bool ShareNpcsWithPlayers = false;
        public static bool NpcsAllowed = false;

        public static Settings MainSettings = Util.ReadSettings();
        public static ObjectPool MainMenuPool = new ObjectPool();
        public static NativeMenu MainMenu = new NativeMenu("GTACoop:R", CurrentModVersion.Replace("_", "."))
        {
            UseMouse = false,
            Alignment = MainSettings.FlipMenu ? GTA.UI.Alignment.Right : GTA.UI.Alignment.Left
        };
        public static NativeMenu MainSettingsMenu = new NativeMenu("GTACoop:R", "Settings", "Go to the settings")
        {
            UseMouse = false,
            Alignment = MainSettings.FlipMenu ? GTA.UI.Alignment.Right : GTA.UI.Alignment.Left
        };
        public static Chat MainChat = new Chat();
        public static PlayerList MainPlayerList = new PlayerList();

        public static Networking MainNetworking = new Networking();

        public static string LocalPlayerID = null;
        public static readonly Dictionary<string, EntitiesPlayer> Players = new Dictionary<string, EntitiesPlayer>();
        public static readonly Dictionary<string, EntitiesNpc> Npcs = new Dictionary<string, EntitiesNpc>();

        public Main()
        {
            Function.Call((Hash)0x0888C3502DBBEEF5); // _LOAD_MP_DLC_MAPS
            Function.Call((Hash)0x9BAE5AD2508DF078, true); // _ENABLE_MP_DLC_MAPS

            NativeItem usernameItem = new NativeItem("Username")
            {
                AltTitle = MainSettings.Username
            };
            usernameItem.Activated += (menu, item) =>
            {
                string newUsername = Game.GetUserInput(WindowTitle.EnterMessage20, usernameItem.AltTitle, 20);
                if (!string.IsNullOrWhiteSpace(newUsername))
                {
                    MainSettings.Username = newUsername;
                    Util.SaveSettings();

                    usernameItem.AltTitle = newUsername;
                    MainMenuPool.RefreshAll();
                }
            };

            NativeItem serverIpItem = new NativeItem("Server IP")
            {
                AltTitle = MainSettings.LastServerAddress
            };
            serverIpItem.Activated += (menu, item) =>
            {
                string newServerIp = Game.GetUserInput(WindowTitle.EnterMessage60, serverIpItem.AltTitle, 60);
                if (!string.IsNullOrWhiteSpace(newServerIp) && newServerIp.Contains(":"))
                {
                    MainSettings.LastServerAddress = newServerIp;
                    Util.SaveSettings();

                    serverIpItem.AltTitle = newServerIp;
                    MainMenuPool.RefreshAll();
                }

            };

            NativeItem serverConnectItem = new NativeItem("Connect");
            serverConnectItem.Activated += (sender, item) =>
            {
                MainNetworking.DisConnectFromServer(MainSettings.LastServerAddress);
            };

            NativeCheckboxItem shareNpcsItem = new NativeCheckboxItem("Share Npcs", ShareNpcsWithPlayers);
            shareNpcsItem.CheckboxChanged += (item, check) =>
            {
                ShareNpcsWithPlayers = shareNpcsItem.Checked;
            };
            shareNpcsItem.Enabled = false;

            NativeCheckboxItem flipMenuItem = new NativeCheckboxItem("Flip menu", MainSettings.FlipMenu);
            flipMenuItem.CheckboxChanged += (item, check) =>
            {
                MainMenu.Alignment = flipMenuItem.Checked ? GTA.UI.Alignment.Right : GTA.UI.Alignment.Left;
                MainSettingsMenu.Alignment = flipMenuItem.Checked ? GTA.UI.Alignment.Right : GTA.UI.Alignment.Left;

                MainSettings.FlipMenu = flipMenuItem.Checked;
                Util.SaveSettings();
            };

            NativeItem aboutItem = new NativeItem("About", "~g~GTACoop~s~:~b~R ~s~by ~o~EntenKoeniq")
            {
                LeftBadge = new LemonUI.Elements.ScaledTexture("commonmenu", "shop_new_star")
            };

#if DEBUG
            NativeCheckboxItem useDebugItem = new NativeCheckboxItem("Debug", UseDebug);
            useDebugItem.CheckboxChanged += (item, check) =>
            {
                UseDebug = useDebugItem.Checked;
                
                if (!useDebugItem.Checked && DebugSyncPed != null)
                {
                    if (DebugSyncPed.Character.Exists())
                    {
                        DebugSyncPed.Character.Kill();
                        DebugSyncPed.Character.Delete();
                    }

                    DebugSyncPed = null;
                    FullDebugSync = true;
                    Players.Remove("DebugKey");
                }
            };
#endif

            MainMenu.Add(usernameItem);
            MainMenu.Add(serverIpItem);
            MainMenu.Add(serverConnectItem);
            MainMenu.AddSubMenu(MainSettingsMenu);
            MainSettingsMenu.Add(shareNpcsItem);
            MainSettingsMenu.Add(flipMenuItem);
#if DEBUG
            MainSettingsMenu.Add(useDebugItem);
#endif
            MainMenu.Add(aboutItem);

            MainMenuPool.Add(MainMenu);
            MainMenuPool.Add(MainSettingsMenu);

            Tick += OnTick;
            KeyDown += OnKeyDown;
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

                Function.Call(Hash.SET_CAN_ATTACK_FRIENDLY, Game.Player.Character, true, true);
                Function.Call(Hash.SET_PED_CAN_BE_TARGETTED, true);
            }

            MainMenuPool.Process();

            MainNetworking.ReceiveMessages();

            if (!MainNetworking.IsOnServer())
            {
                return;
            }

            if (Game.Player.Character.IsGettingIntoVehicle)
            {
                GTA.UI.Notification.Show("~y~Vehicles are not sync yet!", true);
            }

            MainChat.Tick();
            if (!MainChat.Focused && !MainMenuPool.AreAnyVisible)
            {
                MainPlayerList.Tick();
            }

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
                    if (MainMenuPool.AreAnyVisible)
                    {
                        MainMenu.Visible = false;
                        MainSettingsMenu.Visible = false;
                    }
                    else
                    {
                        MainMenu.Visible = true;
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
                        int time = Environment.TickCount;

                        MainPlayerList.Pressed = (time - MainPlayerList.Pressed) < 5000 ? (time - 6000) : time;
                    }
                    break;
            }
        }

        private DateTime ArtificialLagCounter = DateTime.MinValue;
        private EntitiesPlayer DebugSyncPed;
        private bool FullDebugSync = true;
        private bool UseDebug = false;

        private void Debug()
        {
            var player = Game.Player.Character;
            if (!Players.ContainsKey("DebugKey"))
            {
                Players.Add("DebugKey", new EntitiesPlayer() { SocialClubName = "DEBUG", Username = "DebugPlayer" });
                DebugSyncPed = Players["DebugKey"];
            }

            if (DateTime.Now.Subtract(ArtificialLagCounter).TotalMilliseconds < 300)
            {
                return;
            }

            ArtificialLagCounter = DateTime.Now;

            byte? flags = Util.GetPedFlags(player, FullDebugSync, true);

            if (FullDebugSync)
            {
                DebugSyncPed.ModelHash = player.Model.Hash;
                DebugSyncPed.Props = Util.GetPedProps(player);
            }
            DebugSyncPed.Health = player.Health;
            DebugSyncPed.Position = player.Position;

            if (!player.IsInVehicle())
            {
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
            }
            else
            {
                Vehicle veh = player.CurrentVehicle;
                DebugSyncPed.VehicleModelHash = veh.Model.Hash;
                DebugSyncPed.VehicleSeatIndex = (int)player.SeatIndex;
                DebugSyncPed.VehiclePosition = veh.Position;
                DebugSyncPed.VehicleRotation = veh.Quaternion;
                DebugSyncPed.VehicleSpeed = veh.Speed;
                DebugSyncPed.VehicleSteeringAngle = veh.SteeringAngle;
            }

            DebugSyncPed.IsInVehicle = (flags.Value & (byte)PedDataFlags.IsInVehicle) > 0;

            if (DebugSyncPed.Character != null && DebugSyncPed.Character.Exists())
            {
                Function.Call(Hash.SET_ENTITY_NO_COLLISION_ENTITY, DebugSyncPed.Character.Handle, player.Handle, false);
                Function.Call(Hash.SET_ENTITY_NO_COLLISION_ENTITY, player.Handle, DebugSyncPed.Character.Handle, false);
            }

            if (DebugSyncPed.MainVehicle != null && DebugSyncPed.MainVehicle.Exists() && player.IsInVehicle())
            {
                Function.Call(Hash.SET_ENTITY_NO_COLLISION_ENTITY, DebugSyncPed.MainVehicle.Handle, player.CurrentVehicle.Handle, false);
                Function.Call(Hash.SET_ENTITY_NO_COLLISION_ENTITY, player.CurrentVehicle.Handle, DebugSyncPed.MainVehicle.Handle, false);
            }

            FullDebugSync = !FullDebugSync;
        }
    }
}
