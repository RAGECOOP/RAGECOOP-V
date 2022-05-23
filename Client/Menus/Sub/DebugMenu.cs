using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LemonUI;
using LemonUI.Menus;
using GTA;

namespace RageCoop.Client
{
    internal static class DebugMenu
    {
        public static NativeMenu MainMenu = new NativeMenu("RAGECOOP", "Debug", "Debug settings") { 
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
            d1.Activated+=(sender,e) =>
            {
                try{ SyncParameters.PositioinPrediction =float.Parse(Game.GetUserInput(WindowTitle.EnterMessage20, SyncParameters.PositioinPrediction.ToString(), 20));}
                catch { }
                Update();
            };
            

            MainMenu.Add(d1);
            MainMenu.AddSubMenu(DiagnosticMenu);
            MainMenu.Opening+=(sender, e) =>Update(); 
            DiagnosticMenu.Opening+=(sender, e) =>
            {
                DiagnosticMenu.Clear();
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
