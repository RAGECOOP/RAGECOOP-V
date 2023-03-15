using System;
using System.Drawing;

using GTA;
using GTA.UI;
using LemonUI.Menus;
using RageCoop.Client.Scripting;

namespace RageCoop.Client.Menus
{
    internal static class SettingsMenu
    {
        public static NativeMenu Menu = new NativeMenu("RAGECOOP", "Settings", "Go to the settings")
        {
            UseMouse = false,
            Alignment = Settings.FlipMenu ? Alignment.Right : Alignment.Left
        };

        private static readonly NativeCheckboxItem _disableTrafficItem =
            new("Disable Traffic (NPCs/Vehicles)", "Local traffic only",
                Settings.DisableTraffic);

        private static readonly NativeCheckboxItem _flipMenuItem =
            new("Flip menu", Settings.FlipMenu);

        private static readonly NativeCheckboxItem _disablePauseAlt = new("Disable Alternate Pause",
            "Don't freeze game time when Esc pressed", Settings.DisableAlternatePause);

        private static readonly NativeCheckboxItem _disableVoice = new("Enable voice",
            "Check your GTA:V settings to find the right key on your keyboard for PushToTalk and talk to your friends",
            Settings.Voice);

        private static readonly NativeCheckboxItem _showBlip = new("Show player blip",
            "Show other player's blip on map, can be overridden by server resource ",
            Settings.ShowPlayerBlip);

        private static readonly NativeCheckboxItem _showNametag = new("Show player nametag",
            "Show other player's nametag on your screen, only effective if server didn't disable nametag display",
            Settings.ShowPlayerNameTag);

        private static readonly NativeItem _menuKey =
            new NativeItem("Menu Key", "The key to open menu", Settings.MenuKey.ToString());

        private static readonly NativeItem _passengerKey = new("Passenger Key",
            "The key to enter a vehicle as passenger", Settings.PassengerKey.ToString());

        private static readonly NativeItem _vehicleSoftLimit = new("Vehicle limit (soft)",
            "The game won't spawn more NPC traffic if the limit is exceeded. \n-1 for unlimited (not recommended).",
            Settings.WorldVehicleSoftLimit.ToString());

        static SettingsMenu()
        {
            Menu.Banner.Color = Color.FromArgb(225, 0, 0, 0);
            Menu.Title.Color = Color.FromArgb(255, 165, 0);

            _disableTrafficItem.CheckboxChanged += DisableTrafficCheckboxChanged;
            _disablePauseAlt.CheckboxChanged += DisablePauseAltCheckboxChanged;
            _disableVoice.CheckboxChanged += DisableVoiceCheckboxChanged;
            _flipMenuItem.CheckboxChanged += FlipMenuCheckboxChanged;
            _menuKey.Activated += ChaneMenuKey;
            _passengerKey.Activated += ChangePassengerKey;
            _vehicleSoftLimit.Activated += VehicleSoftLimitActivated;
            _showBlip.Activated += (s, e) =>
            {
                Settings.ShowPlayerBlip = _showBlip.Checked;
                Util.SaveSettings();
            };
            _showNametag.Activated += (s, e) =>
            {
                API.Config.ShowPlayerNameTag = _showNametag.Checked;
            };

            Menu.Add(_disableTrafficItem);
            Menu.Add(_disablePauseAlt);
            Menu.Add(_flipMenuItem);
            Menu.Add(_disableVoice);
            Menu.Add(_menuKey);
            Menu.Add(_passengerKey);
            Menu.Add(_vehicleSoftLimit);
            Menu.Add(_showBlip);
            Menu.Add(_showNametag);
        }

        private static void DisableVoiceCheckboxChanged(object sender, EventArgs e)
        {
            if (_disableVoice.Checked)
            {
                if (Networking.IsOnServer && !Voice.WasInitialized()) Voice.Init();
            }
            else
            {
                Voice.ClearAll();
            }

            Settings.Voice = _disableVoice.Checked;
            Util.SaveSettings();
        }

        private static void DisablePauseAltCheckboxChanged(object sender, EventArgs e)
        {
            Settings.DisableAlternatePause = _disablePauseAlt.Checked;
            Util.SaveSettings();
        }

        private static void VehicleSoftLimitActivated(object sender, EventArgs e)
        {
            try
            {
                Settings.WorldVehicleSoftLimit = int.Parse(
                    Game.GetUserInput(WindowTitle.EnterMessage20,
                        Settings.WorldVehicleSoftLimit.ToString(), 20));
                _menuKey.AltTitle = Settings.WorldVehicleSoftLimit.ToString();
                Util.SaveSettings();
            }
            catch
            {
            }
        }

        private static void ChaneMenuKey(object sender, EventArgs e)
        {
            try
            {
                Settings.MenuKey = (Keys)Enum.Parse(
                    typeof(Keys),
                    Game.GetUserInput(WindowTitle.EnterMessage20,
                        Settings.MenuKey.ToString(), 20));
                _menuKey.AltTitle = Settings.MenuKey.ToString();
                Util.SaveSettings();
            }
            catch
            {
            }
        }

        private static void ChangePassengerKey(object sender, EventArgs e)
        {
            try
            {
                Settings.PassengerKey = (Keys)Enum.Parse(
                    typeof(Keys),
                    Game.GetUserInput(WindowTitle.EnterMessage20,
                        Settings.PassengerKey.ToString(), 20));
                _passengerKey.AltTitle = Settings.PassengerKey.ToString();
                Util.SaveSettings();
            }
            catch
            {
            }
        }

        public static void DisableTrafficCheckboxChanged(object a, EventArgs b)
        {
            WorldThread.Traffic(!_disableTrafficItem.Checked);
            Settings.DisableTraffic = _disableTrafficItem.Checked;
            Util.SaveSettings();
        }

        public static void FlipMenuCheckboxChanged(object a, EventArgs b)
        {
            CoopMenu.Menu.Alignment = _flipMenuItem.Checked ? Alignment.Right : Alignment.Left;

            Menu.Alignment = _flipMenuItem.Checked ? Alignment.Right : Alignment.Left;
            Settings.FlipMenu = _flipMenuItem.Checked;
            Util.SaveSettings();
        }
    }
}