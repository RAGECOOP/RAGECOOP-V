using LemonUI.Menus;

namespace CoopClient.Menus.Sub
{
    /// <summary>
    /// Don't use it!
    /// </summary>
    public class Settings
    {
        internal NativeMenu MainMenu = new NativeMenu("GTACOOP:R", "Settings", "Go to the settings")
        {
            UseMouse = false,
            Alignment = Main.MainSettings.FlipMenu ? GTA.UI.Alignment.Right : GTA.UI.Alignment.Left
        };

        private readonly NativeCheckboxItem DisableTraffic = new NativeCheckboxItem("Disable Traffic (NPCs/Vehicles)", "Local traffic only", Main.DisableTraffic);
        private readonly NativeCheckboxItem ShareNPCsItem = new NativeCheckboxItem("Share NPCs", "~y~WARNING:~s~ High network traffic!", Main.ShareNpcsWithPlayers) { Enabled = false };
        private readonly NativeCheckboxItem FlipMenuItem = new NativeCheckboxItem("Flip menu", Main.MainSettings.FlipMenu);
#if DEBUG
        private readonly NativeCheckboxItem UseDebugItem = new NativeCheckboxItem("Debug", Main.UseDebug);
        private readonly NativeCheckboxItem ShowNetworkInfo = new NativeCheckboxItem("Show Network Info", Main.MainNetworking.ShowNetworkInfo);
#endif

        /// <summary>
        /// Don't use it!
        /// </summary>
        public Settings()
        {
            DisableTraffic.CheckboxChanged += DisableTrafficCheckboxChanged;
            ShareNPCsItem.CheckboxChanged += (item, check) => { Main.ShareNpcsWithPlayers = ShareNPCsItem.Checked; };
            FlipMenuItem.CheckboxChanged += FlipMenuCheckboxChanged;
#if DEBUG
            UseDebugItem.CheckboxChanged += UseDebugCheckboxChanged;
            ShowNetworkInfo.CheckboxChanged += ShowNetworkInfoCheckboxChanged;
#endif

            MainMenu.Add(DisableTraffic);
            MainMenu.Add(ShareNPCsItem);
            MainMenu.Add(FlipMenuItem);
#if DEBUG
            MainMenu.Add(UseDebugItem);
            MainMenu.Add(ShowNetworkInfo);
#endif
        }

        internal void DisableTrafficCheckboxChanged(object a, System.EventArgs b)
        {
            Main.DisableTraffic = DisableTraffic.Checked;

            if (DisableTraffic.Checked)
            {
                if (ShareNPCsItem.Checked)
                {
                    ShareNPCsItem.Checked = false;
                }

                ShareNPCsItem.Enabled = false;
            }
            else if (Main.NpcsAllowed && !ShareNPCsItem.Enabled)
            {
                ShareNPCsItem.Enabled = true;
            }
        }

        internal void FlipMenuCheckboxChanged(object a, System.EventArgs b)
        {
#if !NON_INTERACTIVE
            Main.MainMenu.MainMenu.Alignment = FlipMenuItem.Checked ? GTA.UI.Alignment.Right : GTA.UI.Alignment.Left;
#endif
            MainMenu.Alignment = FlipMenuItem.Checked ? GTA.UI.Alignment.Right : GTA.UI.Alignment.Left;

            Main.MainSettings.FlipMenu = FlipMenuItem.Checked;
            Util.SaveSettings();
        }

#if DEBUG
        internal void UseDebugCheckboxChanged(object a, System.EventArgs b)
        {
            Main.UseDebug = UseDebugItem.Checked;

            if (!UseDebugItem.Checked && Main.DebugSyncPed != null)
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
            Main.MainNetworking.ShowNetworkInfo = ShowNetworkInfo.Checked;

            if (!Main.MainNetworking.ShowNetworkInfo)
            {
                Main.MainNetworking.BytesReceived = 0;
                Main.MainNetworking.BytesSend = 0;
            }
        }
#endif
    }
}
