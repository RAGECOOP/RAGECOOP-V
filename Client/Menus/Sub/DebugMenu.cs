using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LemonUI;
using LemonUI.Menus;
using GTA;
using System.Drawing;

namespace RageCoop.Client
{
    internal static class DebugMenu
    {
        public static NativeMenu Menu = new NativeMenu("RAGECOOP", "Debug", "Debug settings") { 
            UseMouse = false,
            Alignment = Main.Settings.FlipMenu ? GTA.UI.Alignment.Right : GTA.UI.Alignment.Left
        };
        public static NativeMenu DiagnosticMenu = new NativeMenu("RAGECOOP", "Diagnostic", "Performence and Diagnostic")
        {
            UseMouse = false,
            Alignment = Main.Settings.FlipMenu ? GTA.UI.Alignment.Right : GTA.UI.Alignment.Left
        };
        private static NativeItem d1=new NativeItem("PositionPrediction");
        static DebugMenu()
        {
            Menu.Banner.Color = Color.FromArgb(225, 0, 0, 0);
            Menu.Title.Color = Color.FromArgb(255, 165, 0);

            d1.Activated+=(sender,e) =>
            {
                try{ SyncParameters.PositioinPrediction =float.Parse(Game.GetUserInput(WindowTitle.EnterMessage20, SyncParameters.PositioinPrediction.ToString(), 20));}
                catch { }
                Update();
            };
            

            Menu.Add(d1);
            Menu.AddSubMenu(DiagnosticMenu);
            Menu.Opening+=(sender, e) =>Update(); 
            DiagnosticMenu.Opening+=(sender, e) =>
            {
                DiagnosticMenu.Clear();
                DiagnosticMenu.Add(new NativeItem("EntityPool", EntityPool.DumpDebug()));
                foreach (var pair in Debug.TimeStamps)
                {
                    DiagnosticMenu.Add(new NativeItem(pair.Key.ToString(), pair.Value.ToString(), pair.Value.ToString()));
                }
            };

            Update();
        }

       

        private static void Update()
        {
            d1.AltTitle = SyncParameters.PositioinPrediction.ToString();
        }
    }
}
