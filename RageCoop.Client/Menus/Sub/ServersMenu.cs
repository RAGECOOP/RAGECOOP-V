using System;
using System.Net;
using System.Drawing;
using System.Collections.Generic;
using Newtonsoft.Json;
using LemonUI.Menus;
using System.Threading;

namespace RageCoop.Client.Menus
{
    internal class ServerListClass
    {
        [JsonProperty("address")]
        public string Address { get; set; }

        [JsonProperty("port")]
        public string Port { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
        
        [JsonProperty("version")]
        public string Version { get; set; }
        
        [JsonProperty("players")]
        public int Players { get; set; }
        
        [JsonProperty("maxPlayers")]
        public int MaxPlayers { get; set; }

        [JsonProperty("country")]
        public string Country { get; set; }
    }

    /// <summary>
    /// Don't use it!
    /// </summary>
    internal static class ServersMenu
    {
        private static Thread GetServersThread;
        internal static NativeMenu Menu = new NativeMenu("RAGECOOP", "Servers", "Go to the server list")
        {
            UseMouse = false,
            Alignment = Main.Settings.FlipMenu ? GTA.UI.Alignment.Right : GTA.UI.Alignment.Left
        };
        internal static NativeItem ResultItem = null;

        /// <summary>
        /// Don't use it!
        /// </summary>
        static ServersMenu()
        {
            Menu.Banner.Color = Color.FromArgb(225, 0, 0, 0);
            Menu.Title.Color = Color.FromArgb(255, 165, 0);

            Menu.Opening += (object sender, System.ComponentModel.CancelEventArgs e) =>
            {
                CleanUpList();
                Menu.Add(ResultItem = new NativeItem("Loading..."));

                // Prevent freezing
                GetServersThread=new Thread(()=> GetAllServers());
                GetServersThread.Start();
            };
            Menu.Closing += (object sender, System.ComponentModel.CancelEventArgs e) =>
            {
                CleanUpList();
            };
        }

        private static void CleanUpList()
        {
            Menu.Clear();
            ResultItem = null;
        }

        private static void GetAllServers()
        {
            List<ServerListClass> serverList = null;
            var realUrl = Main.Settings.MasterServer;
            serverList = JsonConvert.DeserializeObject<List<ServerListClass>>(DownloadString(realUrl));
            
            // Need to be processed in main thread
            Main.QueueAction(() =>
            {
                if (serverList == null)
                {
                    ResultItem.Title = "Something went wrong!";
                    return;
                }
                if (serverList.Count == 0)
                {
                    ResultItem.Title = "No server was found!";
                    return;
                }
                CleanUpList();
                foreach (ServerListClass server in serverList)
                {
                    string address = $"{server.Address}:{server.Port}";
                    NativeItem tmpItem = new NativeItem($"[{server.Country}] {server.Name}", $"~b~{address}~s~~n~~g~Version {server.Version}.x~s~") { AltTitle = $"[{server.Players}/{server.MaxPlayers}]" };
                    tmpItem.Activated += (object sender, EventArgs e) =>
                    {
                        try
                        {
                            Menu.Visible = false;

                            Networking.ToggleConnection(address);
#if !NON_INTERACTIVE
                            CoopMenu.ServerIpItem.AltTitle = address;

                            CoopMenu.Menu.Visible = true;
#endif
                            Main.Settings.LastServerAddress = address;
                            Util.SaveSettings();
                        }
                        catch (Exception ex)
                        {
                            GTA.UI.Notification.Show($"~r~{ex.Message}");
                        }
                    };
                    Menu.Add(tmpItem);
                }
            });
        }
        private static string DownloadString(string url)
        {
            try
            {
                // TLS only
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls13 | SecurityProtocolType.Tls12;
                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

                WebClient client = new WebClient();
                return client.DownloadString(url);
            }
            catch (Exception ex)
            {
                Main.QueueAction(() =>
                {
                    ResultItem.Title = "Download failed!";
                    ResultItem.Description = ex.Message;
                });
                return "";
            }
        }
    }
}