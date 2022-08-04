using GTA;
using LemonUI.Menus;
using System.Drawing;
using System;

namespace RageCoop.Client
{
    internal static class DebugMenu
    {
        public static NativeMenu Menu = new NativeMenu("RAGECOOP", "Debug", "Debug settings")
        {
            UseMouse = false,
            Alignment = Main.Settings.FlipMenu ? GTA.UI.Alignment.Right : GTA.UI.Alignment.Left
        };
        public static NativeMenu DiagnosticMenu = new NativeMenu("RAGECOOP", "Diagnostic", "Performence and Diagnostic")
        {
            UseMouse = false,
            Alignment = Main.Settings.FlipMenu ? GTA.UI.Alignment.Right : GTA.UI.Alignment.Left
        };
        public static NativeItem SimulatedLatencyItem = new NativeItem("Simulated network latency", "Simulated network latency in ms (one way)", "0");
        private static readonly NativeCheckboxItem ShowNetworkInfoItem = new NativeCheckboxItem("Show Network Info", Networking.ShowNetworkInfo);

        static DebugMenu()
        {
            Menu.Banner.Color = Color.FromArgb(225, 0, 0, 0);
            Menu.Title.Color = Color.FromArgb(255, 165, 0);


            DiagnosticMenu.Opening+=(sender, e) =>
            {
                DiagnosticMenu.Clear();
                DiagnosticMenu.Add(new NativeItem("EntityPool", EntityPool.DumpDebug()));
                foreach (var pair in Debug.TimeStamps)
                {
                    DiagnosticMenu.Add(new NativeItem(pair.Key.ToString(), pair.Value.ToString(), pair.Value.ToString()));
                }
            };
            SimulatedLatencyItem.Activated+=(s, e) =>
            {
                try
                {
                    SimulatedLatencyItem.AltTitle=((Networking.SimulatedLatency=int.Parse(Game.GetUserInput(SimulatedLatencyItem.AltTitle))*0.002f)*500).ToString();
                }
                catch(Exception ex) { Main.Logger.Error(ex); }
            };
            ShowNetworkInfoItem.CheckboxChanged += (s,e) => { Networking.ShowNetworkInfo = ShowNetworkInfoItem.Checked; };
            Menu.Add(SimulatedLatencyItem);
            Menu.Add(ShowNetworkInfoItem);
            Menu.AddSubMenu(DiagnosticMenu);

        }


    }
}
