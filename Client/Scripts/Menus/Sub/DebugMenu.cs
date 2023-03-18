using System;
using System.Drawing;
using System.Linq;
using System.Reflection;
using GTA.UI;
using LemonUI.Menus;
using RageCoop.Core;

namespace RageCoop.Client
{
    internal static class DebugMenu
    {
        public static NativeMenu Menu = new("RAGECOOP", "Debug", "Debug settings")
        {
            UseMouse = false,
            Alignment = Settings.FlipMenu ? Alignment.Right : Alignment.Left
        };

        public static NativeMenu DiagnosticMenu = new("RAGECOOP", "Diagnostic", "Performence and Diagnostic")
        {
            UseMouse = false,
            Alignment = Settings.FlipMenu ? Alignment.Right : Alignment.Left
        };


        public static NativeMenu TuneMenu = new("RAGECOOP", "Change tunable values")
        {
            UseMouse = false,
            Alignment = Settings.FlipMenu ? Alignment.Right : Alignment.Left
        };

        public static NativeItem SimulatedLatencyItem =
            new("Simulated network latency", "Simulated network latency in ms (one way)", "0");

        public static NativeCheckboxItem ShowOwnerItem = new("Show entity owner",
            "Show the owner name of the entity you're aiming at", false);

        private static readonly NativeCheckboxItem ShowNetworkInfoItem =
            new("Show Network Info", Networking.ShowNetworkInfo);

        static DebugMenu()
        {
            Menu.Banner.Color = Color.FromArgb(225, 0, 0, 0);
            Menu.Title.Color = Color.FromArgb(255, 165, 0);

            TuneMenu.Opening += (s, e) =>
            {
                TuneMenu.Clear();
                foreach (var t in typeof(Main).Assembly.GetTypes())
                {
                    foreach (var field in t.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic))
                    {
                        var attri = field.GetCustomAttribute<DebugTunableAttribute>();
                        if (attri == null)
                            continue;
                        var item = new NativeItem($"{t}.{field.Name}");
                        item.AltTitle = field.GetValue(null).ToString();
                        item.Activated += (s, e) =>
                        {
                            try
                            {
                                field.SetValue(null, Convert.ChangeType(Game.GetUserInput(), field.FieldType));
                                item.AltTitle = field.GetValue(null).ToString();
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex);
                            }
                        };
                        TuneMenu.Add(item);
                    }
                }
            };
            DiagnosticMenu.Opening += (sender, e) =>
            {
                DiagnosticMenu.Clear();
                DiagnosticMenu.Add(new NativeItem("EntityPool", EntityPool.DumpDebug()));
                // foreach (var pair in Debug.TimeStamps)
                //     DiagnosticMenu.Add(
                //         new NativeItem(pair.Key.ToString(), pair.Value.ToString(), pair.Value.ToString()));
            };
            ShowNetworkInfoItem.CheckboxChanged += (s, e) =>
            {
                Networking.ShowNetworkInfo = ShowNetworkInfoItem.Checked;
            };
            ShowOwnerItem.CheckboxChanged += (s, e) =>
            {
                Settings.ShowEntityOwnerName = ShowOwnerItem.Checked;
                Util.SaveSettings();
            };
#if DEBUG
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
                    Log.Error(ex);
                }
            };
            Menu.Add(SimulatedLatencyItem);
#endif
            Menu.Add(ShowNetworkInfoItem);
            Menu.Add(ShowOwnerItem);
            Menu.AddSubMenu(DiagnosticMenu);
            Menu.AddSubMenu(TuneMenu);
        }
    }
}