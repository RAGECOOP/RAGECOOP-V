using GTA;
using LemonUI.Menus;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace RageCoop.Client.Menus
{
    internal static class SettingsMenu
    {
        public static NativeMenu Menu = new NativeMenu("RAGECOOP", "Settings", "Go to the settings")
        {
            UseMouse = false,
            Alignment = Main.Settings.FlipMenu ? GTA.UI.Alignment.Right : GTA.UI.Alignment.Left
        };

        private static readonly NativeCheckboxItem _disableTrafficItem = new NativeCheckboxItem("Disable Traffic (NPCs/Vehicles)", "Local traffic only", Main.Settings.DisableTraffic);
        private static readonly NativeCheckboxItem _flipMenuItem = new NativeCheckboxItem("Flip menu", Main.Settings.FlipMenu);
        private static readonly NativeCheckboxItem _disablePauseAlt = new NativeCheckboxItem("Disable Alternate Pause", "Don't freeze game time when Esc pressed", Main.Settings.DisableAlternatePause);
        private static readonly NativeCheckboxItem _showBlip = new NativeCheckboxItem("Show player blip", "Show other player's blip on map", Main.Settings.ShowPlayerBlip);
        private static readonly NativeCheckboxItem _showNametag = new NativeCheckboxItem("Show player nametag", "Show other player's nametag on your screen", Main.Settings.ShowPlayerNameTag);
        private static readonly NativeCheckboxItem _disableVoice = new NativeCheckboxItem("Enable voice", "Check your GTA:V settings to find the right key on your keyboard for PushToTalk and talk to your friends", Main.Settings.Voice);

        private static readonly NativeItem _menuKey = new NativeItem("Menu Key", "The key to open menu", Main.Settings.MenuKey.ToString());
        private static readonly NativeItem _passengerKey = new NativeItem("Passenger Key", "The key to enter a vehicle as passenger", Main.Settings.PassengerKey.ToString());
        private static readonly NativeItem _vehicleSoftLimit = new NativeItem("Vehicle limit (soft)", "The game won't spawn more NPC traffic if the limit is exceeded. \n-1 for unlimited (not recommended).", Main.Settings.WorldVehicleSoftLimit.ToString());
        private static readonly NativeItem _pedSoftLimit = new NativeItem("Ped limit (soft)", "The game won't spawn more NPCs if the limit is exceeded. \n-1 for unlimited (not recommended).", Main.Settings.WorldPedSoftLimit.ToString());

        static SettingsMenu()
        {
            Menu.Banner.Color = Color.FromArgb(225, 0, 0, 0);
            Menu.BannerText.Color = Color.FromArgb(255, 165, 0);

            _disableTrafficItem.CheckboxChanged += DisableTrafficCheckboxChanged;
            _disablePauseAlt.CheckboxChanged += DisablePauseAltCheckboxChanged;
            _disableVoice.CheckboxChanged += DisableVoiceCheckboxChanged;
            _flipMenuItem.CheckboxChanged += FlipMenuCheckboxChanged;
            _menuKey.Activated += ChaneMenuKey;
            _passengerKey.Activated += ChangePassengerKey;
            _vehicleSoftLimit.Activated += VehicleSoftLimitActivated;
            _pedSoftLimit.Activated += PedSoftLimitActivated;
            _showBlip.Activated += (s, e) =>
            {
                Main.Settings.ShowPlayerBlip = _showBlip.Checked;
                Util.SaveSettings();
            };
            _showNametag.Activated += (s, e) =>
            {
                Main.Settings.ShowPlayerNameTag = _showNametag.Checked;
                Util.SaveSettings();
            };

            Menu.Add(_disableTrafficItem);
            Menu.Add(_disablePauseAlt);
            Menu.Add(_flipMenuItem);
            Menu.Add(_disableVoice);
            Menu.Add(_menuKey);
            Menu.Add(_passengerKey);
            Menu.Add(_vehicleSoftLimit);
            Menu.Add(_pedSoftLimit);
            Menu.Add(_showBlip);
            Menu.Add(_showNametag);
        }

        private static void DisableVoiceCheckboxChanged(object sender, EventArgs e)
        {
            if (_disableVoice.Checked)
            {
                if (Networking.IsOnServer && !Voice.WasInitialized())
                {
                    Voice.Init();
                }
            }
            else
            {
                Voice.ClearAll();
            }

            Main.Settings.Voice = _disableVoice.Checked;
            Util.SaveSettings();
        }

        private static void DisablePauseAltCheckboxChanged(object sender, EventArgs e)
        {
            Main.Settings.DisableAlternatePause = _disablePauseAlt.Checked;
            Util.SaveSettings();
        }
        private static void VehicleSoftLimitActivated(object sender, EventArgs e)
        {
            try
            {
                Main.Settings.WorldVehicleSoftLimit = int.Parse(
                    Game.GetUserInput(WindowTitle.EnterMessage20,
                    Main.Settings.WorldVehicleSoftLimit.ToString(), 20));
                _vehicleSoftLimit.AltTitle = Main.Settings.WorldVehicleSoftLimit.ToString();
                Util.SaveSettings();
            }
            catch { }
        }
        private static void PedSoftLimitActivated(object sender, EventArgs e)
        {
            try
            {
                Main.Settings.WorldPedSoftLimit = int.Parse(
                    Game.GetUserInput(WindowTitle.EnterMessage20,
                    Main.Settings.WorldPedSoftLimit.ToString(), 20));
                _pedSoftLimit.AltTitle = Main.Settings.WorldPedSoftLimit.ToString();
                Util.SaveSettings();
            }
            catch { }
        }
        private static void ChaneMenuKey(object sender, EventArgs e)
        {
            try
            {
                Main.Settings.MenuKey = (Keys)Enum.Parse(
                    typeof(Keys),
                    Game.GetUserInput(WindowTitle.EnterMessage20,
                    Main.Settings.MenuKey.ToString(), 20));
                _menuKey.AltTitle = Main.Settings.MenuKey.ToString();
                Util.SaveSettings();
            }
            catch { }
        }

        private static void ChangePassengerKey(object sender, EventArgs e)
        {
            try
            {
                Main.Settings.PassengerKey = (Keys)Enum.Parse(
                    typeof(Keys),
                    Game.GetUserInput(WindowTitle.EnterMessage20,
                    Main.Settings.PassengerKey.ToString(), 20));
                _passengerKey.AltTitle = Main.Settings.PassengerKey.ToString();
                Util.SaveSettings();
            }
            catch { }
        }

        public static void DisableTrafficCheckboxChanged(object a, System.EventArgs b)
        {
            WorldThread.Traffic(!_disableTrafficItem.Checked);
            Main.Settings.DisableTraffic = _disableTrafficItem.Checked;
            Util.SaveSettings();
        }

        public static void FlipMenuCheckboxChanged(object a, System.EventArgs b)
        {
            CoopMenu.Menu.Alignment = _flipMenuItem.Checked ? GTA.UI.Alignment.Right : GTA.UI.Alignment.Left;

            Menu.Alignment = _flipMenuItem.Checked ? GTA.UI.Alignment.Right : GTA.UI.Alignment.Left;
            Main.Settings.FlipMenu = _flipMenuItem.Checked;
            Util.SaveSettings();
        }
    }
}
