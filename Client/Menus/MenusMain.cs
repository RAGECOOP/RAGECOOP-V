using GTA;

using LemonUI;
using LemonUI.Menus;

namespace CoopClient.Menus
{
    public class MenusMain
    {
        public ObjectPool MenuPool = new ObjectPool();

        public NativeMenu MainMenu = new NativeMenu("GTACOOP:R", Main.CurrentModVersion.Replace("_", "."))
        {
            UseMouse = false,
            Alignment = Main.MainSettings.FlipMenu ? GTA.UI.Alignment.Right : GTA.UI.Alignment.Left
        };
        #region ===== SUB =====
        public Sub.Settings SubSettings = new Sub.Settings();
        #endregion

        #region ===== ITEMS =====
        private readonly NativeItem UsernameItem = new NativeItem("Username") { AltTitle = Main.MainSettings.Username };
        private readonly NativeItem ServerIpItem = new NativeItem("Server IP") { AltTitle = Main.MainSettings.LastServerAddress };
        private readonly NativeItem ServerConnectItem = new NativeItem("Connect");
        private readonly NativeItem AboutItem = new NativeItem("About", "~g~GTACOOP~s~:~b~R ~s~by EntenKoeniq") { LeftBadge = new LemonUI.Elements.ScaledTexture("commonmenu", "shop_new_star") };
        #endregion // !ITEMS

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
    }
}
