using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using RageCoop.Core;
using static RageCoop.Client.Shared;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace RageCoop.Client.Installer
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
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
            var od = new OpenFileDialog
            {
                Filter = "GTA 5 executable |GTA5.exe;PlayGTAV.exe",
                Title = "Select you GTAV executable"
            };
            if (od.ShowDialog() ?? !true)
                Task.Run(() =>
                {
                    try
                    {
                        Install(Directory.GetParent(od.FileName).FullName);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Installation failed: " + ex);
                        Environment.Exit(1);
                    }
                });
            else
                Environment.Exit(0);
        }

        private void Install(string root)
        {
            UpdateStatus("Checking requirements");
            var shvPath = Path.Combine(root, "ScriptHookV.dll");
            var shvdnPath = Path.Combine(root, "ScriptHookVDotNet3.dll");
            var scriptsPath = Path.Combine(root, "Scripts");
            var installPath = Path.Combine(root, "RageCoop");
            var legacyPath = Path.Combine(scriptsPath, "RageCoop");
            if (Directory.GetParent(Assembly.GetExecutingAssembly().Location).FullName.StartsWith(installPath))
                throw new InvalidOperationException(
                    "The installer is not meant to be run in the game folder, please extract the zip to somewhere else and run again.");
            if (!File.Exists(shvPath))
            {
                MessageBox.Show("Please install ScriptHookV first!");
                Environment.Exit(1);
            }

            Directory.CreateDirectory(installPath);

            if (!File.Exists(shvdnPath))
            {
                MessageBox.Show("Please install ScriptHookVDotNet first!");
                Environment.Exit(1);
            }

            var shvdnVer = GetVer(shvdnPath);
            if (shvdnVer < new Version(3, 5, 1))
            {
                MessageBox.Show("Please update ScriptHookVDotNet to latest version!" +
                                $"\nCurrent version is {shvdnVer}, 3.5.1 or higher is required");
                Environment.Exit(1);
            }


            UpdateStatus("Removing old versions");

            foreach (var f in Directory.GetFiles(scriptsPath, "RageCoop.*", SearchOption.AllDirectories))
                File.Delete(f);

            // <= 1.5 installation check
            if (Directory.Exists(legacyPath)) Directory.Delete(legacyPath, true);

            foreach (var f in Directory.GetFiles(installPath, "*.dll", SearchOption.AllDirectories)) File.Delete(f);

            if (File.Exists("Scripts/RageCoop.Core.dll") && File.Exists("Scripts/RageCoop.Client.dll") &&
                File.Exists("Loader/RageCoop.Client.Loader.dll"))
            {
                UpdateStatus("Installing...");
                CoreUtils.CopyFilesRecursively(new DirectoryInfo(Directory.GetCurrentDirectory()),
                    new DirectoryInfo(installPath));
                File.Copy("Loader/RageCoop.Client.Loader.dll", Path.Combine(scriptsPath, "RageCoop.Client.Loader.dll"),
                    true);
                Finish();
            }
            else
            {
                throw new Exception(
                    "Required files are missing, please re-download the installer from official website");
            }

            void Finish()
            {
                checkKeys:
                UpdateStatus("Checking conflicts");
                var menyooConfig = Path.Combine(root, @"menyooStuff\menyooConfig.ini");
                var settingsPath = Path.Combine(root, SettingsPath);
                Settings settings = null;
                try
                {
                    settings = Util.ReadSettings(settingsPath);
                }
                catch
                {
                    settings = new Settings();
                }

                if (File.Exists(menyooConfig))
                {
                    var lines = File.ReadAllLines(menyooConfig)
                        .Where(x => !x.StartsWith(";") && x.EndsWith(" = " + (int)settings.MenuKey));
                    if (lines.Any())
                        if (MessageBox.Show("Following menyoo config value will conflict with RAGECOOP menu key\n" +
                                            string.Join("\n", lines)
                                            + "\nDo you wish to change the Menu Key?", "Warning!",
                                MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                        {
                            var ae = new AutoResetEvent(false);
                            UpdateStatus("Press the key you wish to change to");
                            Dispatcher.BeginInvoke(new Action(() =>
                                KeyDown += (s, e) =>
                                {
                                    settings.MenuKey = (GTA.Keys)KeyInterop.VirtualKeyFromKey(e.Key);
                                    ae.Set();
                                }));
                            ae.WaitOne();
                            if (!Util.SaveSettings(settingsPath, settings))
                            {
                                MessageBox.Show("Error occurred when saving settings");
                                Environment.Exit(1);
                            }

                            MessageBox.Show("Menu key changed to " + settings.MenuKey);
                            goto checkKeys;
                        }
                }

                UpdateStatus("Checking ZeroTier");
                try
                {
                    ZeroTierHelper.Check();
                }
                catch
                {
                    if (MessageBox.Show(
                            "You can't join ZeroTier server unless ZeroTier is installed, do you want to download and install it?",
                            "Install ZeroTier", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        var url = "https://download.zerotier.com/dist/ZeroTier%20One.msi";
                        UpdateStatus("Downloading ZeroTier from " + url);
                        try
                        {
                            HttpHelper.DownloadFile(url, "ZeroTier.msi",
                                p => UpdateStatus("Downloading ZeroTier " + p + "%"));
                            UpdateStatus("Installing ZeroTier");
                            Process.Start("ZeroTier.msi").WaitForExit();
                        }
                        catch
                        {
                            MessageBox.Show("Failed to download ZeroTier, please download it from official website");
                            Process.Start(url);
                        }
                    }
                }

                UpdateStatus("Completed!");
                MessageBox.Show("Installation successful!");
                Environment.Exit(0);
            }
        }

        private void UpdateStatus(string status)
        {
            Dispatcher.BeginInvoke(new Action(() => Status.Content = status));
        }

        private Version GetVer(string location)
        {
            return Version.Parse(FileVersionInfo.GetVersionInfo(location).FileVersion);
        }
    }
}