using GTA;

using LemonUI;
using LemonUI.Menus;

namespace CoopClient.Menus
{
    public class MenusMain
    {
        public ObjectPool MenuPool = new ObjectPool();

        public NativeMenu MainMenu = new NativeMenu("GTACOOP:R", "MAIN")
        {
            UseMouse = false,
            Alignment = Main.MainSettings.FlipMenu ? GTA.UI.Alignment.Right : GTA.UI.Alignment.Left
        };
        #region SUB
        public Sub.Settings SubSettings = new Sub.Settings();
        #endregion

        #region ITEMS
        private readonly NativeItem UsernameItem = new NativeItem("Username") { AltTitle = Main.MainSettings.Username };
        private readonly NativeItem ServerIpItem = new NativeItem("Server IP") { AltTitle = Main.MainSettings.LastServerAddress };
        private readonly NativeItem ServerConnectItem = new NativeItem("Connect");
        private readonly NativeItem AboutItem = new NativeItem("About", "~y~SOURCE~s~~n~" +
            "https://github.com/GTACOOP-R~n~" +
            "~y~VERSION~s~~n~" +
            Main.CurrentVersion.Replace("_", ".")) { LeftBadge = new LemonUI.Elements.ScaledTexture("commonmenu", "shop_new_star") };
        #endregion

        public MenusMain()
        {
            UsernameItem.Activated += UsernameActivated;
            ServerIpItem.Activated += ServerIpActivated;
            ServerConnectItem.Activated += (sender, item) => { Main.MainNetworking.DisConnectFromServer(Main.MainSettings.LastServerAddress); };

            MainMenu.Add(UsernameItem);
            MainMenu.Add(ServerIpItem);
            MainMenu.Add(ServerConnectItem);

            MainMenu.AddSubMenu(SubSettings.MainMenu);

            MainMenu.Add(AboutItem);

            MenuPool.Add(MainMenu);
            MenuPool.Add(SubSettings.MainMenu);
        }

        public void UsernameActivated(object a, System.EventArgs b)
        {
            string newUsername = Game.GetUserInput(WindowTitle.EnterMessage20, UsernameItem.AltTitle, 20);
            if (!string.IsNullOrWhiteSpace(newUsername))
            {
                Main.MainSettings.Username = newUsername;
                Util.SaveSettings();

                UsernameItem.AltTitle = newUsername;
                MenuPool.RefreshAll();
            }
        }

        public void ServerIpActivated(object a, System.EventArgs b)
        {
            string newServerIp = Game.GetUserInput(WindowTitle.EnterMessage60, ServerIpItem.AltTitle, 60);
            if (!string.IsNullOrWhiteSpace(newServerIp) && newServerIp.Contains(":"))
            {
                Main.MainSettings.LastServerAddress = newServerIp;
                Util.SaveSettings();

                ServerIpItem.AltTitle = newServerIp;
                MenuPool.RefreshAll();
            }
        }

        public void InitiateConnectionMenuSetting()
        {
            MainMenu.Items[0].Enabled = false;
            MainMenu.Items[1].Enabled = false;
            MainMenu.Items[2].Enabled = false;
        }

        public void ConnectedMenuSetting()
        {
            MainMenu.Items[2].Enabled = true;
            MainMenu.Items[2].Title = "Disconnect";
            SubSettings.MainMenu.Items[1].Enabled = !Main.DisableTraffic && Main.NpcsAllowed;

            MainMenu.Visible = false;
            MenuPool.RefreshAll();
        }

        public void DisconnectedMenuSetting()
        {
            MainMenu.Items[0].Enabled = true;
            MainMenu.Items[1].Enabled = true;
            MainMenu.Items[2].Enabled = true;
            MainMenu.Items[2].Title = "Connect";
            SubSettings.MainMenu.Items[1].Enabled = false;

            MenuPool.RefreshAll();
        }
    }
}
