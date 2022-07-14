using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LemonUI.Menus;
using System.Web;
using System.Net;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;

namespace RageCoop.Client.Menus
{
    internal class UpdateMenu
    {
        public static bool IsUpdating { get; private set; } = false;
        private static NativeItem _updatingItem = new NativeItem("Updating...");
        private static NativeItem _downloadItem = new NativeItem("Download","Download and update to latest nightly");

        private static string _downloadPath = Path.Combine(Main.Settings.DataDirectory, "RageCoop.Client.zip");
        public static NativeMenu Menu = new NativeMenu("Update", "Update", "Download and install latest nightly build from GitHub")
        {
            UseMouse = false,
            Alignment = Main.Settings.FlipMenu ? GTA.UI.Alignment.Right : GTA.UI.Alignment.Left
        };
        static UpdateMenu()
        {
            Menu.Banner.Color = Color.FromArgb(225, 0, 0, 0);
            Menu.Title.Color = Color.FromArgb(255, 165, 0);
            Menu.Opening+=Opening;
            _downloadItem.Activated+=StartUpdate;
        }

        private static void StartUpdate(object sender, EventArgs e)
        {
            IsUpdating=true;
            Menu.Clear();
            Menu.Add(_updatingItem);
            Task.Run(() =>
            {
                try
                {
                    if (File.Exists(_downloadPath)) { File.Delete(_downloadPath); }
                    WebClient client = new WebClient();

                    // TLS only
                    ServicePointManager.Expect100Continue = true;
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls13 | SecurityProtocolType.Tls12;
                    ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

                    client.DownloadProgressChanged += (s, e1) => { Main.QueueAction(() => { _updatingItem.AltTitle=$"{e1.ProgressPercentage}%"; }); };
                    client.DownloadFileCompleted +=(s, e2) => { Install(); };
                    client.DownloadFileAsync(new Uri("https://github.com/RAGECOOP/RAGECOOP-V/releases/download/nightly/RageCoop.Client.zip"), _downloadPath);
                }
                catch (Exception ex)
                {
                    Main.Logger.Error(ex);
                }
            });
        }

        private static void Install()
        {
            try
            {
                Main.QueueAction(() =>
                {
                    _updatingItem.AltTitle="Installing...";
                });
                new FastZip().ExtractZip(_downloadPath, "Scripts",FastZip.Overwrite.Always, null,null,null,true);
                Main.QueueAction(() =>
                {
                    Util.Reload();
                    IsUpdating=false;
                });
            }
            catch (Exception ex)
            {
                Main.Logger.Error(ex);
            }
        }

        private static void Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Menu.Clear();
            if (Networking.IsOnServer)
            {
                Menu.Add(new NativeItem("Disconnect from the server first"));
            }
            else if (IsUpdating)
            {
                Menu.Add(_updatingItem);
            }
            else
            {
                Menu.Add(_downloadItem);
            }
        }
    }
}
