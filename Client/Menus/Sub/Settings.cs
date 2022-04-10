using System.Drawing;

using LemonUI.Menus;

namespace CoopClient.Menus.Sub
{
    /// <summary>
    /// Don't use it!
    /// </summary>
    public class Settings
    {
        internal NativeMenu MainMenu = new NativeMenu("RAGECOOP", "Settings", "Go to the settings")
        {
            UseMouse = false,
            Alignment = Main.MainSettings.FlipMenu ? GTA.UI.Alignment.Right : GTA.UI.Alignment.Left
        };

        private readonly NativeCheckboxItem _disableTrafficItem = new NativeCheckboxItem("Disable Traffic (NPCs/Vehicles)", "Local traffic only", Main.DisableTraffic);
        private readonly NativeCheckboxItem _shareNPCsItem = new NativeCheckboxItem("Share NPCs", "~y~WARNING:~s~ High network traffic!", Main.ShareNPCsWithPlayers) { Enabled = false };
        private readonly NativeCheckboxItem _flipMenuItem = new NativeCheckboxItem("Flip menu", Main.MainSettings.FlipMenu);
#if DEBUG
        private readonly NativeCheckboxItem _useDebugItem = new NativeCheckboxItem("Debug", Main.UseDebug);
        private readonly NativeCheckboxItem _showNetworkInfoItem = new NativeCheckboxItem("Show Network Info", Main.MainNetworking.ShowNetworkInfo);
#endif

        /// <summary>
        /// Don't use it!
        /// </summary>
        public Settings()
        {
            MainMenu.Banner.Color = Color.FromArgb(225, 0, 0, 0);
            MainMenu.Title.Color = Color.FromArgb(255, 165, 0);

            _disableTrafficItem.CheckboxChanged += DisableTrafficCheckboxChanged;
            _shareNPCsItem.CheckboxChanged += (item, check) => { Main.ShareNPCsWithPlayers = _shareNPCsItem.Checked; };
            _flipMenuItem.CheckboxChanged += FlipMenuCheckboxChanged;
#if DEBUG
            _useDebugItem.CheckboxChanged += UseDebugCheckboxChanged;
            _showNetworkInfoItem.CheckboxChanged += ShowNetworkInfoCheckboxChanged;
#endif

            MainMenu.Add(_disableTrafficItem);
            MainMenu.Add(_shareNPCsItem);
            MainMenu.Add(_flipMenuItem);
#if DEBUG
            MainMenu.Add(_useDebugItem);
            MainMenu.Add(_showNetworkInfoItem);
#endif
        }

        internal void DisableTrafficCheckboxChanged(object a, System.EventArgs b)
        {
            Main.DisableTraffic = _disableTrafficItem.Checked;

            if (_disableTrafficItem.Checked)
            {
                if (_shareNPCsItem.Checked)
                {
                    _shareNPCsItem.Checked = false;
                }

                _shareNPCsItem.Enabled = false;
            }
            else if (Main.NPCsAllowed && !_shareNPCsItem.Enabled)
            {
                _shareNPCsItem.Enabled = true;
            }
        }

        internal void FlipMenuCheckboxChanged(object a, System.EventArgs b)
        {
#if !NON_INTERACTIVE
            Main.MainMenu.MainMenu.Alignment = _flipMenuItem.Checked ? GTA.UI.Alignment.Right : GTA.UI.Alignment.Left;
#endif
            MainMenu.Alignment = _flipMenuItem.Checked ? GTA.UI.Alignment.Right : GTA.UI.Alignment.Left;
            Main.MainMenu.ServerList.MainMenu.Alignment = _flipMenuItem.Checked ? GTA.UI.Alignment.Right : GTA.UI.Alignment.Left;

            Main.MainSettings.FlipMenu = _flipMenuItem.Checked;
            Util.SaveSettings();
        }

#if DEBUG
        internal void UseDebugCheckboxChanged(object a, System.EventArgs b)
        {
            Main.UseDebug = _useDebugItem.Checked;

            if (!_useDebugItem.Checked && Main.DebugSyncPed != null)
            {
                if (Main.DebugSyncPed.Character.Exists())
                {
                    Main.DebugSyncPed.Character.Kill();
                    Main.DebugSyncPed.Character.Delete();
                }

                Main.DebugSyncPed = null;
                Main.LastFullDebugSync = 0;
                Main.Players.Remove(0);
            }
        }

        internal void ShowNetworkInfoCheckboxChanged(object a, System.EventArgs b)
        {
            Main.MainNetworking.ShowNetworkInfo = _showNetworkInfoItem.Checked;

            if (!Main.MainNetworking.ShowNetworkInfo)
            {
                Main.MainNetworking.BytesReceived = 0;
                Main.MainNetworking.BytesSend = 0;
            }
        }
#endif
    }
}
