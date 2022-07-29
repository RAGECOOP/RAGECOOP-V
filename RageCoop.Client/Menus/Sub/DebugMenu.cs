using GTA;
using LemonUI.Menus;
using System.Drawing;

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
        static DebugMenu()
        {
            Menu.Banner.Color = Color.FromArgb(225, 0, 0, 0);
            Menu.Title.Color = Color.FromArgb(255, 165, 0);


            Menu.AddSubMenu(DiagnosticMenu);
            DiagnosticMenu.Opening+=(sender, e) =>
            {
                DiagnosticMenu.Clear();
                DiagnosticMenu.Add(new NativeItem("EntityPool", EntityPool.DumpDebug()));
                foreach (var pair in Debug.TimeStamps)
                {
                    DiagnosticMenu.Add(new NativeItem(pair.Key.ToString(), pair.Value.ToString(), pair.Value.ToString()));
                }
            };

        }



    }
}
