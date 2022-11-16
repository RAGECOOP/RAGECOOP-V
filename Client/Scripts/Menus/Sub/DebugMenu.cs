using System;
using System.Drawing;
using GTA;
using GTA.UI;
using LemonUI.Menus;
using RageCoop.Client.GUI;
using RageCoop.Client.Loader;

namespace RageCoop.Client
{
    internal static class DebugMenu
    {
        public static NativeMenu Menu = new NativeMenu("RAGECOOP", "Debug", "Debug settings")
        {
            UseMouse = false,
            Alignment = Main.Settings.FlipMenu ? Alignment.Right : Alignment.Left
        };

        public static NativeMenu DiagnosticMenu = new NativeMenu("RAGECOOP", "Diagnostic", "Performence and Diagnostic")
        {
            UseMouse = false,
            Alignment = Main.Settings.FlipMenu ? Alignment.Right : Alignment.Left
        };

        public static NativeItem ReloadItem = new NativeItem("Reload", "Reload RAGECOOP and associated scripts");

        public static NativeItem SimulatedLatencyItem =
            new NativeItem("Simulated network latency", "Simulated network latency in ms (one way)", "0");

        public static NativeCheckboxItem ShowOwnerItem = new NativeCheckboxItem("Show entity owner",
            "Show the owner name of the entity you're aiming at", false);

        private static readonly NativeCheckboxItem ShowNetworkInfoItem =
            new NativeCheckboxItem("Show Network Info", Networking.ShowNetworkInfo);

        private static readonly NativeCheckboxItem DxHookTest =
            new NativeCheckboxItem("Enable D3D11 hook", false);

        private static readonly NativeCheckboxItem CefTest =
            new NativeCheckboxItem("Test CEF overlay", false);

        private static CefClient _testCef;

        static DebugMenu()
        {
            Menu.Banner.Color = Color.FromArgb(225, 0, 0, 0);
            Menu.Title.Color = Color.FromArgb(255, 165, 0);


            DiagnosticMenu.Opening += (sender, e) =>
            {
                DiagnosticMenu.Clear();
                DiagnosticMenu.Add(new NativeItem("EntityPool", EntityPool.DumpDebug()));
                foreach (var pair in Debug.TimeStamps)
                    DiagnosticMenu.Add(
                        new NativeItem(pair.Key.ToString(), pair.Value.ToString(), pair.Value.ToString()));
            };
            SimulatedLatencyItem.Activated += (s, e) =>
            {
                try
                {
                    SimulatedLatencyItem.AltTitle =
                        ((Networking.SimulatedLatency =
                            int.Parse(Game.GetUserInput(SimulatedLatencyItem.AltTitle)) * 0.002f) * 500).ToString();
                }
                catch (Exception ex)
                {
                    Main.Logger.Error(ex);
                }
            };
            ShowNetworkInfoItem.CheckboxChanged += (s, e) =>
            {
                Networking.ShowNetworkInfo = ShowNetworkInfoItem.Checked;
            };
            ShowOwnerItem.CheckboxChanged += (s, e) =>
            {
                Main.Settings.ShowEntityOwnerName = ShowOwnerItem.Checked;
                Util.SaveSettings();
            };
            DxHookTest.CheckboxChanged += Hook;
            CefTest.CheckboxChanged += CefTestChange;
            ;
            ReloadItem.Activated += ReloadDomain;
            Menu.Add(SimulatedLatencyItem);
            Menu.Add(ShowNetworkInfoItem);
            Menu.Add(ShowOwnerItem);
            Menu.Add(ReloadItem);
            Menu.AddSubMenu(DiagnosticMenu);
            Menu.Add(DxHookTest);
            Menu.Add(CefTest);
        }

        private static void CefTestChange(object sender, EventArgs e)
        {
            if (CefTest.Checked)
            {
                _testCef = CefManager.CreateClient(new Size(640, 480));
                _testCef.Scale = 0.8f;
                _testCef.Opacity = 128;
                Script.Wait(2000);
                _testCef.Controller.LoadUrl("https://ragecoop.online/");
                CefManager.ActiveClient = _testCef;
            }
            else
            {
                CefManager.DestroyClient(_testCef);
            }

            DxHookTest.Checked = HookManager.Hooked;
        }

        private static void Hook(object sender, EventArgs e)
        {
            if (DxHookTest.Checked)
                HookManager.Initialize();
            else
                HookManager.CleanUp();
        }

        private static void ReloadDomain(object sender, EventArgs e)
        {
            LoaderContext.RequestUnload();
        }
    }
}