using GTA;
using GTA.Native;
using LemonUI;
using LemonUI.Menus;
using LemonUI.Scaleform;
using System.Drawing;

namespace RageCoop.Client.Menus
{
    /// <summary>
    /// Don't use it!
    /// </summary>
    internal static class CoopMenu
    {
        public static ObjectPool MenuPool = new ObjectPool();
        public static NativeMenu Menu = new NativeMenu("RAGECOOP", "MAIN")
        {
            UseMouse = false,
            Alignment = Main.Settings.FlipMenu ? GTA.UI.Alignment.Right : GTA.UI.Alignment.Left
        };
        public static PopUp PopUp = new PopUp()
        {
            Title = "",
            Prompt = "",
            Subtitle = "",
            Error = "",
            ShowBackground = true,
            Visible = false,
        };
        public static NativeMenu LastMenu { get; set; } = Menu;
        #region ITEMS
        private static readonly NativeItem _usernameItem = new NativeItem("Username") { AltTitle = Main.Settings.Username };
        private static readonly NativeItem _passwordItem = new NativeItem("Password") { AltTitle = new string('*', Main.Settings.Password.Length) };

        public static readonly NativeItem ServerIpItem = new NativeItem("Server IP") { AltTitle = Main.Settings.LastServerAddress };
        internal static readonly NativeItem _serverConnectItem = new NativeItem("Connect");
        private static readonly NativeItem _aboutItem = new NativeItem("About", "~y~SOURCE~s~~n~" +
            "https://github.com/RAGECOOP~n~" +
            "~y~VERSION~s~~n~" +
            Main.Version)
        { LeftBadge = new LemonUI.Elements.ScaledTexture("commonmenu", "shop_new_star") };


        #endregion

        /// <summary>
        /// Don't use it!
        /// </summary>
        static CoopMenu()
        {

            Menu.Banner.Color = Color.FromArgb(225, 0, 0, 0);
            Menu.BannerText.Color = Color.FromArgb(255, 165, 0);

            _usernameItem.Activated += UsernameActivated;
            _passwordItem.Activated += _passwordActivated;
            ServerIpItem.Activated += ServerIpActivated;
            _serverConnectItem.Activated += (sender, item) => { Networking.ToggleConnection(Main.Settings.LastServerAddress); };


            Menu.AddSubMenu(ServersMenu.Menu);

            Menu.Add(_usernameItem);
            Menu.Add(_passwordItem);
            Menu.Add(ServerIpItem);
            Menu.Add(_serverConnectItem);

            Menu.AddSubMenu(SettingsMenu.Menu);
            Menu.AddSubMenu(DevToolMenu.Menu);
#if DEBUG
            Menu.AddSubMenu(DebugMenu.Menu);
#endif

            MenuPool.Add(Menu);
            MenuPool.Add(SettingsMenu.Menu);
            MenuPool.Add(DevToolMenu.Menu);
#if DEBUG
            MenuPool.Add(DebugMenu.Menu);
            MenuPool.Add(DebugMenu.DiagnosticMenu);
#endif
            MenuPool.Add(ServersMenu.Menu);
            MenuPool.Add(PopUp);

            Menu.Add(_aboutItem);
        }


        public static bool ShowPopUp(string prompt, string title, string subtitle, string error, bool showbackground)
        {
            PopUp.Prompt = prompt;
            PopUp.Title = title;
            PopUp.Subtitle = subtitle;
            PopUp.Error = error;
            PopUp.ShowBackground = showbackground;
            PopUp.Visible = true;
            Script.Yield();
            while (true)
            {
                Game.DisableAllControlsThisFrame();
                MenuPool.Process();

                var scaleform = new Scaleform("instructional_buttons");
                scaleform.CallFunction("CLEAR_ALL");
                scaleform.CallFunction("TOGGLE_MOUSE_BUTTONS", 0);
                scaleform.CallFunction("CREATE_CONTAINER");

                scaleform.CallFunction("SET_DATA_SLOT", 0, Function.Call<string>((Hash)0x0499D7B09FC9B407, 2, (int)Control.FrontendAccept, 0), "Continue");
                scaleform.CallFunction("SET_DATA_SLOT", 1, Function.Call<string>((Hash)0x0499D7B09FC9B407, 2, (int)Control.FrontendCancel, 0), "Cancel");
                scaleform.CallFunction("DRAW_INSTRUCTIONAL_BUTTONS", -1);
                scaleform.Render2D();
                if (Game.IsControlJustPressed(Control.FrontendAccept))
                {
                    PopUp.Visible = false;
                    return true;
                }
                else if (Game.IsControlJustPressed(Control.FrontendCancel))
                {
                    PopUp.Visible = false;
                    return false;
                }
                Script.Yield();
                Game.DisableAllControlsThisFrame();

            }
        }
        public static void UsernameActivated(object a, System.EventArgs b)
        {
            string newUsername = Game.GetUserInput(WindowTitle.EnterMessage20, _usernameItem.AltTitle, 20);
            if (!string.IsNullOrWhiteSpace(newUsername))
            {
                Main.Settings.Username = newUsername;
                Util.SaveSettings();

                _usernameItem.AltTitle = newUsername;
            }
        }

        private static void _passwordActivated(object sender, System.EventArgs e)
        {
            string newPass = Game.GetUserInput(WindowTitle.EnterMessage20, "", 20);
            Main.Settings.Password = newPass;
            Util.SaveSettings();
            _passwordItem.AltTitle = new string('*', newPass.Length);
        }
        public static void ServerIpActivated(object a, System.EventArgs b)
        {
            string newServerIp = Game.GetUserInput(WindowTitle.EnterMessage60, ServerIpItem.AltTitle, 60);
            if (!string.IsNullOrWhiteSpace(newServerIp) && newServerIp.Contains(":"))
            {
                Main.Settings.LastServerAddress = newServerIp;
                Util.SaveSettings();

                ServerIpItem.AltTitle = newServerIp;
            }
        }

        public static void InitiateConnectionMenuSetting()
        {
            _usernameItem.Enabled = false;
            ServerIpItem.Enabled = false;
            _serverConnectItem.Enabled = false;
        }

        public static void ConnectedMenuSetting()
        {
            _serverConnectItem.Enabled = true;
            _serverConnectItem.Title = "Disconnect";
            Menu.Visible = false;
        }

        public static void DisconnectedMenuSetting()
        {
            _usernameItem.Enabled = true;
            ServerIpItem.Enabled = true;
            _serverConnectItem.Enabled = true;
            _serverConnectItem.Title = "Connect";
        }
    }
}
