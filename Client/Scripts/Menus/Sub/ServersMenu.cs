using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Net;
using System.Net.Http;
using System.Threading;
using GTA.UI;
using LemonUI.Menus;
using Newtonsoft.Json;
using RageCoop.Client.Scripting;
using RageCoop.Core;

namespace RageCoop.Client.Menus
{
    /// <summary>
    ///     Don't use it!
    /// </summary>
    internal static class ServersMenu
    {
        private static Thread GetServersThread;

        internal static NativeMenu Menu = new NativeMenu("RAGECOOP", "Servers", "Go to the server list")
        {
            UseMouse = false,
            Alignment = Settings.FlipMenu ? Alignment.Right : Alignment.Left
        };

        internal static NativeItem ResultItem = null;

        /// <summary>
        ///     Don't use it!
        /// </summary>
        static ServersMenu()
        {
            Menu.Banner.Color = Color.FromArgb(225, 0, 0, 0);
            Menu.Title.Color = Color.FromArgb(255, 165, 0);

            Menu.Opening += (object sender, CancelEventArgs e) =>
            {
                CleanUpList();
                Menu.Add(ResultItem = new NativeItem("Loading..."));

                // Prevent freezing
                GetServersThread = ThreadManager.CreateThread(() => GetAllServers(),"GetServers");
            };
            Menu.Closing += (object sender, CancelEventArgs e) => { CleanUpList(); };
        }

        private static void CleanUpList()
        {
            Menu.Clear();
            ResultItem = null;
        }

        private static void GetAllServers()
        {
            List<ServerInfo> serverList = null;
            var realUrl = Settings.MasterServer;
            serverList = null;
            try { serverList = JsonDeserialize<List<ServerInfo>>(DownloadString(realUrl)); }
            catch (Exception ex) { Log.Error(ex); }

            // Need to be processed in main thread
            API.QueueAction(() =>
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
                foreach (ServerInfo server in serverList)
                {
                    string address = $"{server.address}:{server.port}";
                    NativeItem tmpItem =
                        new NativeItem($"[{server.country}] {server.name}",
                                $"~b~{address}~s~~n~~g~Version {server.version}.x~s~")
                        { AltTitle = $"[{server.players}/{server.maxPlayers}]" };
                    tmpItem.Activated += (object sender, EventArgs e) =>
                    {
                        try
                        {
                            Menu.Visible = false;
                            if (server.useZT)
                            {
                                address = $"{server.ztAddress}:{server.port}";
                                Notification.Show($"~y~Joining ZeroTier network... {server.ztID}");
                                if (ZeroTierHelper.Join(server.ztID) == null)
                                {
                                    throw new Exception("Failed to obtain ZeroTier network IP");
                                }
                            }

                            Networking.ToggleConnection(address, null, null, PublicKey.FromServerInfo(server));
#if !NON_INTERACTIVE
                            CoopMenu.ServerIpItem.AltTitle = address;

                            CoopMenu.Menu.Visible = true;
#endif
                            Settings.LastServerAddress = address;
                            Util.SaveSettings();
                        }
                        catch (Exception ex)
                        {
                            Notification.Show($"~r~{ex.Message}");
                            if (server.useZT)
                            {
                                Notification.Show(
                                    $"Make sure ZeroTier is correctly installed, download it from https://www.zerotier.com/");
                            }
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

                var client = new HttpClient();
                return client.GetStringAsync(url).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                API.QueueAction(() =>
                {
                    ResultItem.Title = "Download failed!";
                    ResultItem.Description = ex.Message;
                });
                return "";
            }
        }
    }
}