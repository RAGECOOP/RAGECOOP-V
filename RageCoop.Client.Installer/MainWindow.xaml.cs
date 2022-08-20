using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using System.IO;
using Path = System.IO.Path;
using System.Diagnostics;
using System.Reflection;
using RageCoop.Core;
using System.Net;
using ICSharpCode.SharpZipLib.Zip;

namespace RageCoop.Client.Installer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Choose();
        }

        private void Choose()
        {
            var od = new OpenFileDialog()
            {
                Filter = "GTA 5 executable |GTA5.exe;PlayGTAV.exe",
                Title="Select you GTAV executable"
            };
            if (od.ShowDialog() ?? false==true)
            {
                Task.Run(() => Install(Directory.GetParent(od.FileName).FullName));
            }
            else
            {
                Environment.Exit(0);
            }
        }
        void Install(string root)
        {
            UpdateStatus("Checking requirements");
            var shvPath = Path.Combine(root, "ScriptHookV.dll");
            var shvdnPath = Path.Combine(root, "ScriptHookVDotNet3.dll");
            var scriptsPath = Path.Combine(root, "Scripts");
            var lemonPath = Path.Combine(scriptsPath, "LemonUI.SHVDN3.dll");
            var installPath = Path.Combine(scriptsPath, "RageCoop");
            Directory.CreateDirectory(installPath);
            if (!File.Exists(shvPath))
            {
                MessageBox.Show("Please install ScriptHookV first!");
                Environment.Exit(1);
            }
            if (!File.Exists(shvdnPath))
            {
                MessageBox.Show("Please install ScriptHookVDotNet first!");
                Environment.Exit(1);
            }
            var shvdnVer = GetVer(shvdnPath);
            if (shvdnVer<new Version(3,5,1))
            {
                MessageBox.Show("Please update ScriptHookVDotNet to latest version!" +
                    $"\nCurrent version is {shvdnVer.ToString()}, 3.5.1 or higher is required");
                Environment.Exit(1);
            }
            if (File.Exists(lemonPath))
            {
                var lemonVer=GetVer(lemonPath);
                if(lemonVer<new Version(1, 7))
                {
                    UpdateStatus("Updating LemonUI");
                    File.WriteAllBytes(lemonPath,getLemon());
                }
            }
            UpdateStatus("Removing old versions");

            foreach (var f in Directory.GetFiles(scriptsPath, "RageCoop.*", SearchOption.AllDirectories))
            {
                File.Delete(f);
            }
            foreach (var f in Directory.GetFiles(installPath, "*.dll", SearchOption.AllDirectories))
            {
                File.Delete(f);
            }

            if (Directory.Exists("RageCoop"))
            {
                UpdateStatus("Installing...");
                CopyFilesRecursively(new DirectoryInfo("RageCoop"),new DirectoryInfo(installPath));
                UpdateStatus("Completed!");
                MessageBox.Show("Installation sucessful!");
                Environment.Exit(0);
            }
            else
            {
                UpdateStatus("Downloading...");
                var downloadPath = "RageCoop.Client.zip";
                downloadPath = Path.GetFullPath(downloadPath);
                WebClient client = new WebClient();

                // TLS only
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls13 | SecurityProtocolType.Tls12;
                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

                client.DownloadProgressChanged += (s, e1) => UpdateStatus($"Downloading {e1.ProgressPercentage}%");
                client.DownloadFileCompleted += (s, e2) =>
                {

                    UpdateStatus("Installing...");
                    Directory.CreateDirectory(installPath);
                    new FastZip().ExtractZip(downloadPath, scriptsPath, FastZip.Overwrite.Always, null, null, null, true);
                    UpdateStatus("Completed!");
                    MessageBox.Show("Installation sucessful!");
                    Environment.Exit(0);
                };
                client.DownloadFileAsync(new Uri("https://github.com/RAGECOOP/RAGECOOP-V/releases/download/nightly/RageCoop.Client.zip"), downloadPath);
            }
        }
        void UpdateStatus(string status)
        {
            Dispatcher.BeginInvoke(new Action(() => Status.Content = status));
        }
        Version GetVer(string location)
        {
            return Version.Parse(FileVersionInfo.GetVersionInfo(location).FileVersion);
        }
        byte[] getLemon()
        {
            return (byte[])Resource.ResourceManager.GetObject("LemonUI_SHVDN3");
        }
        public static void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target)
        {
            foreach (DirectoryInfo dir in source.GetDirectories())
                CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name));
            foreach (FileInfo file in source.GetFiles())
                file.CopyTo(Path.Combine(target.FullName, file.Name));
        }
    }
}
