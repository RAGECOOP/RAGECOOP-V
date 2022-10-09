using ICSharpCode.SharpZipLib.Zip;
using LemonUI.Menus;
using RageCoop.Client.Scripting;
using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace RageCoop.Client.Menus
{
    internal class UpdateMenu
    {
        static readonly API API = Main.API;
        public static bool IsUpdating { get; private set; } = false;
        private static readonly NativeItem _updatingItem = new NativeItem("Updating...");
        private static readonly NativeItem _downloadItem = new NativeItem("Download", "Download and update to latest nightly");

        private static readonly string _downloadPath = Path.Combine(Main.Settings.DataDirectory, "RageCoop.Client.zip");
        public static NativeMenu Menu = new NativeMenu("Update", "Update", "Download and install latest nightly build from GitHub")
        {
            UseMouse = false,
            Alignment = Main.Settings.FlipMenu ? GTA.UI.Alignment.Right : GTA.UI.Alignment.Left
        };
        static UpdateMenu()
        {
            Menu.Banner.Color = Color.FromArgb(225, 0, 0, 0);
            Menu.Title.Color = Color.FromArgb(255, 165, 0);
            Menu.Opening += Opening;
            _downloadItem.Activated += StartUpdate;
        }

        private static void StartUpdate(object sender, EventArgs e)
        {
            IsUpdating = true;
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

                    client.DownloadProgressChanged += (s, e1) => { API.QueueAction(() => { _updatingItem.AltTitle = $"{e1.ProgressPercentage}%"; }); };
                    client.DownloadFileCompleted += (s, e2) => { Install(); };
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
                API.QueueAction(() =>
                {
                    _updatingItem.AltTitle = "Installing...";
                });
                Directory.CreateDirectory(@"Scripts\RageCoop");
                foreach (var f in Directory.GetFiles(@"Scripts\RageCoop", "*.dll", SearchOption.AllDirectories))
                {
                    try { File.Delete(f); }
                    catch { }
                }
                new FastZip().ExtractZip(_downloadPath, "Scripts", FastZip.Overwrite.Always, null, null, null, true);
                try { File.Delete(_downloadPath); } catch { }
                try { File.Delete(Path.Combine("Scripts", "RageCoop.Client.Installer.exe")); } catch { }
                API.QueueAction(() =>
                {
                    Util.Reload();
                    IsUpdating = false;
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
