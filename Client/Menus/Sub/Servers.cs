using System;
using System.Net;
using System.Collections.Generic;

using Newtonsoft.Json;

using LemonUI.Menus;

namespace CoopClient.Menus.Sub
{
    internal class ServerListClass
    {
        [JsonProperty("ip")]
        public string IP { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("players")]
        public int Players { get; set; }
        [JsonProperty("maxPlayers")]
        public int MaxPlayers { get; set; }
        [JsonProperty("allowlist")]
        public bool AllowList { get; set; }
        [JsonProperty("mods")]
        public bool Mods { get; set; }
        [JsonProperty("npcs")]
        public bool NPCs { get; set; }
        [JsonProperty("country")]
        public string Country { get; set; }
    }

    /// <summary>
    /// Don't use it!
    /// </summary>
    public class Servers
    {
        internal NativeMenu MainMenu = new NativeMenu("GTACOOP:R", "Servers", "Go to the server list")
        {
            UseMouse = false,
            Alignment = Main.MainSettings.FlipMenu ? GTA.UI.Alignment.Right : GTA.UI.Alignment.Left
        };
        internal NativeItem ResultItem = null;

        /// <summary>
        /// Don't use it!
        /// </summary>
        public Servers()
        {
            MainMenu.Opening += (object sender, System.ComponentModel.CancelEventArgs e) =>
            {
                MainMenu.Add(ResultItem = new NativeItem("Loading..."));
                GetAllServer();
            };
            MainMenu.Closed += (object sender, EventArgs e) =>
            {
                for (int i = 0; i < MainMenu.Items.Count; i++)
                {
                    MainMenu.Remove(MainMenu.Items[i]);
                }
            };
        }
        private void GetAllServer()
        {
            List<ServerListClass> serverList = null;
            try
            {
                WebClient client = new WebClient();
                string data = client.DownloadString("http://gtacoopr.000webhostapp.com/");
                serverList = JsonConvert.DeserializeObject<List<ServerListClass>>(data);
            }
            catch (Exception ex)
            {
                ResultItem.Title = "Download failed!";
                ResultItem.Description = ex.Message; // You have to use any key to see this message
            }

            if (serverList == null)
            {
                return;
            }

            if (ResultItem != null)
            {
                MainMenu.Remove(MainMenu.Items[0]);
                ResultItem = null;
            }

            foreach (ServerListClass server in serverList)
            {
                NativeItem tmpItem = null;
                MainMenu.Add(tmpItem = new NativeItem($"[{server.Country}] {server.Name}", $"~b~{server.IP}~s~~n~Mods = {server.Mods}~n~NPCs = {server.NPCs}") { AltTitle = $"[{server.Players}/{server.MaxPlayers}][{(server.AllowList ? "~r~X~s~" : "~g~O~s~")}]"});
                tmpItem.Activated += (object sender, EventArgs e) =>
                {
                    try
                    {
                        MainMenu.Visible = false;
                        Main.MainMenu.MainMenu.Visible = true;

                        Main.MainNetworking.DisConnectFromServer(server.IP);

                        Main.MainSettings.LastServerAddress = server.IP;
                        Util.SaveSettings();
                    }
                    catch (Exception ex)
                    {
                        GTA.UI.Notification.Show($"~r~{ex.Message}");
                    }
                };
            }
        }
    }
}
