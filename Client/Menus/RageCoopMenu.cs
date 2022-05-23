using GTA;

using System.Drawing;

using LemonUI;
using LemonUI.Menus;

namespace RageCoop.Client.Menus
{
    /// <summary>
    /// Don't use it!
    /// </summary>
    public class RageCoopMenu
    {
        public ObjectPool MenuPool = new ObjectPool();

        public NativeMenu MainMenu = new NativeMenu("RAGECOOP", "MAIN")
        {
            UseMouse = false,
            Alignment = Main.Settings.FlipMenu ? GTA.UI.Alignment.Right : GTA.UI.Alignment.Left
        };
        #region SUB
        public Sub.SettingsMenu SubSettings = new Sub.SettingsMenu();
        #endregion

        #region ITEMS
        private readonly NativeItem _usernameItem = new NativeItem("Username") { AltTitle = Main.Settings.Username };
        public readonly NativeItem ServerIpItem = new NativeItem("Server IP") { AltTitle = Main.Settings.LastServerAddress };
        private readonly NativeItem _serverConnectItem = new NativeItem("Connect");
        private readonly NativeItem _aboutItem = new NativeItem("About", "~y~SOURCE~s~~n~" +
            "https://github.com/RAGECOOP~n~" +
            "~y~VERSION~s~~n~" +
            Main.CurrentVersion.Replace("_", ".")) { LeftBadge = new LemonUI.Elements.ScaledTexture("commonmenu", "shop_new_star") };
        

        #endregion

        /// <summary>
        /// Don't use it!
        /// </summary>
        public RageCoopMenu()
        {
            MainMenu.Banner.Color = Color.FromArgb(225, 0, 0, 0);
            MainMenu.Title.Color = Color.FromArgb(255, 165, 0);

            _usernameItem.Activated += UsernameActivated;
            ServerIpItem.Activated += ServerIpActivated;
            _serverConnectItem.Activated += (sender, item) => { Main.MainNetworking.DisConnectFromServer(Main.Settings.LastServerAddress); };


            MainMenu.Add(_usernameItem);
            MainMenu.Add(ServerIpItem);
            MainMenu.Add(_serverConnectItem);

            MainMenu.AddSubMenu(SubSettings.MainMenu);
            MainMenu.AddSubMenu(DebugMenu.MainMenu);
            MainMenu.Add(_aboutItem);

            MenuPool.Add(MainMenu);
            MenuPool.Add(SubSettings.MainMenu);
            MenuPool.Add(DebugMenu.MainMenu);
            MenuPool.Add(DebugMenu.DiagnosticMenu);
        }

        public void UsernameActivated(object a, System.EventArgs b)
        {
            string newUsername = Game.GetUserInput(WindowTitle.EnterMessage20, _usernameItem.AltTitle, 20);
            if (!string.IsNullOrWhiteSpace(newUsername))
            {
                Main.Settings.Username = newUsername;
                Util.SaveSettings();

                _usernameItem.AltTitle = newUsername;
            }
        }

        public void ServerIpActivated(object a, System.EventArgs b)
        {
            string newServerIp = Game.GetUserInput(WindowTitle.EnterMessage60, ServerIpItem.AltTitle, 60);
            if (!string.IsNullOrWhiteSpace(newServerIp) && newServerIp.Contains(":"))
            {
                Main.Settings.LastServerAddress = newServerIp;
                Util.SaveSettings();

                ServerIpItem.AltTitle = newServerIp;
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
            SubSettings.MainMenu.Items[1].Enabled = !Main.DisableTraffic && Main.NPCsAllowed;

            MainMenu.Visible = false;
        }

        public void DisconnectedMenuSetting()
        {
            MainMenu.Items[0].Enabled = true;
            MainMenu.Items[1].Enabled = true;
            MainMenu.Items[2].Enabled = true;
            MainMenu.Items[2].Title = "Connect";
            SubSettings.MainMenu.Items[1].Enabled = false;
        }
    }
}
