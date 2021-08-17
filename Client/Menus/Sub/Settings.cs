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
        
        private readonly NativeCheckboxItem ShareNpcsItem = new NativeCheckboxItem("Share Npcs", Main.ShareNpcsWithPlayers) { Enabled = false };
        private readonly NativeSliderItem StreamedNpcsItem = new NativeSliderItem(string.Format("Streamed Npcs ({0})", Main.MainSettings.StreamedNpc), 20, Main.MainSettings.StreamedNpc);
        private readonly NativeCheckboxItem FlipMenuItem = new NativeCheckboxItem("Flip menu", Main.MainSettings.FlipMenu);
        private readonly NativeCheckboxItem UseDebugItem = new NativeCheckboxItem("Debug", Main.UseDebug);

        public Settings()
        {
            ShareNpcsItem.CheckboxChanged += (item, check) => { Main.ShareNpcsWithPlayers = ShareNpcsItem.Checked; };
            StreamedNpcsItem.ValueChanged += StreamedNpcsValueChanged;
            FlipMenuItem.CheckboxChanged += FlipMenuCheckboxChanged;
#if DEBUG
            UseDebugItem.CheckboxChanged += UseDebugCheckboxChanged;
#endif

            MainMenu.Add(ShareNpcsItem);
            MainMenu.Add(StreamedNpcsItem);
            MainMenu.Add(FlipMenuItem);
#if DEBUG
            MainMenu.Add(UseDebugItem);
#endif
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
    }
}
