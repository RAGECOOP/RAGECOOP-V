using LemonUI.Menus;

namespace CoopClient.Menus.Sub
{
    public class Settings
    {
        public NativeMenu MainMenu = new NativeMenu("GTACOOP:R", "Settings", "Go to the settings")
        {
            UseMouse = false,
            Alignment = Main.MainSettings.FlipMenu ? GTA.UI.Alignment.Right : GTA.UI.Alignment.Left
        };

        private readonly NativeCheckboxItem DisableTraffic = new NativeCheckboxItem("Disable Traffic", Main.DisableTraffic);
        private readonly NativeCheckboxItem ShareNpcsItem = new NativeCheckboxItem("Share Npcs", Main.ShareNpcsWithPlayers) { Enabled = false };
        private readonly NativeSliderItem StreamedNpcsItem = new NativeSliderItem(string.Format("Streamed Npcs ({0})", Main.MainSettings.StreamedNpc), 20, Main.MainSettings.StreamedNpc > 20 ? 20 : Main.MainSettings.StreamedNpc);
        private readonly NativeCheckboxItem FlipMenuItem = new NativeCheckboxItem("Flip menu", Main.MainSettings.FlipMenu);
#if DEBUG
        private readonly NativeCheckboxItem UseDebugItem = new NativeCheckboxItem("Debug", Main.UseDebug);
        private readonly NativeCheckboxItem ShowNetworkInfo = new NativeCheckboxItem("Show Network Info", Main.MainNetworking.ShowNetworkInfo);
#endif

        public Settings()
        {
            DisableTraffic.CheckboxChanged += DisableTrafficCheckboxChanged;
            ShareNpcsItem.CheckboxChanged += (item, check) => { Main.ShareNpcsWithPlayers = ShareNpcsItem.Checked; };
            StreamedNpcsItem.ValueChanged += StreamedNpcsValueChanged;
            FlipMenuItem.CheckboxChanged += FlipMenuCheckboxChanged;
#if DEBUG
            UseDebugItem.CheckboxChanged += UseDebugCheckboxChanged;
            ShowNetworkInfo.CheckboxChanged += ShowNetworkInfoCheckboxChanged;
#endif

            MainMenu.Add(DisableTraffic);
            MainMenu.Add(ShareNpcsItem);
            MainMenu.Add(StreamedNpcsItem);
            MainMenu.Add(FlipMenuItem);
#if DEBUG
            MainMenu.Add(UseDebugItem);
            MainMenu.Add(ShowNetworkInfo);
#endif
        }

        public void DisableTrafficCheckboxChanged(object a, System.EventArgs b)
        {
            Main.DisableTraffic = DisableTraffic.Checked;

            if (DisableTraffic.Checked)
            {
                if (ShareNpcsItem.Checked)
                {
                    ShareNpcsItem.Checked = false;
                }

                ShareNpcsItem.Enabled = false;
            }
            else if (Main.NpcsAllowed && !ShareNpcsItem.Enabled)
            {
                ShareNpcsItem.Enabled = true;
            }
        }

        public void StreamedNpcsValueChanged(object a, System.EventArgs b)
        {
            Main.MainSettings.StreamedNpc = StreamedNpcsItem.Value;
            Util.SaveSettings();
            StreamedNpcsItem.Title = string.Format("Streamed Npcs ({0})", Main.MainSettings.StreamedNpc);
        }

        public void FlipMenuCheckboxChanged(object a, System.EventArgs b)
        {
            Main.MainMenu.MainMenu.Alignment = FlipMenuItem.Checked ? GTA.UI.Alignment.Right : GTA.UI.Alignment.Left;
            MainMenu.Alignment = FlipMenuItem.Checked ? GTA.UI.Alignment.Right : GTA.UI.Alignment.Left;

            Main.MainSettings.FlipMenu = FlipMenuItem.Checked;
            Util.SaveSettings();
        }

#if DEBUG
        public void UseDebugCheckboxChanged(object a, System.EventArgs b)
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

        public void ShowNetworkInfoCheckboxChanged(object a, System.EventArgs b)
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
