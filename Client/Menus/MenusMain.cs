using GTA;

using LemonUI;
using LemonUI.Menus;

namespace CoopClient.Menus
{
    /// <summary>
    /// Don't use it!
    /// </summary>
    public class MenusMain
    {
        internal ObjectPool MenuPool = new ObjectPool();

        internal NativeMenu MainMenu = new NativeMenu("GTACOOP:R", "MAIN")
        {
            UseMouse = false,
            Alignment = Main.MainSettings.FlipMenu ? GTA.UI.Alignment.Right : GTA.UI.Alignment.Left
        };
        #region SUB
        /// <summary>
        /// Don't use it!
        /// </summary>
        public Sub.Settings SubSettings = new Sub.Settings();
        /// <summary>
        /// Don't use it!
        /// </summary>
        public Sub.Servers ServerList = new Sub.Servers();
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

        /// <summary>
        /// Don't use it!
        /// </summary>
        public MenusMain()
        {
            UsernameItem.Activated += UsernameActivated;
            ServerIpItem.Activated += ServerIpActivated;
            ServerConnectItem.Activated += (sender, item) => { Main.MainNetworking.DisConnectFromServer(Main.MainSettings.LastServerAddress); };

            MainMenu.AddSubMenu(ServerList.MainMenu);

            MainMenu.Add(UsernameItem);
            MainMenu.Add(ServerIpItem);
            MainMenu.Add(ServerConnectItem);

            MainMenu.AddSubMenu(SubSettings.MainMenu);

            MainMenu.Add(AboutItem);

            MenuPool.Add(ServerList.MainMenu);
            MenuPool.Add(MainMenu);
            MenuPool.Add(SubSettings.MainMenu);
        }

        internal void UsernameActivated(object a, System.EventArgs b)
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

        internal void ServerIpActivated(object a, System.EventArgs b)
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

        internal void InitiateConnectionMenuSetting()
        {
            MainMenu.Items[0].Enabled = false;
            MainMenu.Items[1].Enabled = false;
            MainMenu.Items[2].Enabled = false;
            MainMenu.Items[3].Enabled = false;
        }

        internal void ConnectedMenuSetting()
        {
            MainMenu.Items[3].Enabled = true;
            MainMenu.Items[3].Title = "Disconnect";
            SubSettings.MainMenu.Items[1].Enabled = !Main.DisableTraffic && Main.NpcsAllowed;

            MainMenu.Visible = false;
            ServerList.MainMenu.Visible = false;
            MenuPool.RefreshAll();
        }

        internal void DisconnectedMenuSetting()
        {
            MainMenu.Items[0].Enabled = true;
            MainMenu.Items[1].Enabled = true;
            MainMenu.Items[2].Enabled = true;
            MainMenu.Items[3].Enabled = true;
            MainMenu.Items[3].Title = "Connect";
            SubSettings.MainMenu.Items[1].Enabled = false;

            MenuPool.RefreshAll();
        }
    }
}
