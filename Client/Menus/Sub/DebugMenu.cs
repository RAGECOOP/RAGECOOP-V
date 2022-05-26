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
        private static NativeCheckboxItem devToolItem=new NativeCheckboxItem("DevTool");
        public static NativeItem boneIndexItem = new NativeItem("Current bone index");
        static DebugMenu()
        {
            d1.Activated+=(sender,e) =>
            {
                try{ SyncParameters.PositioinPrediction =float.Parse(Game.GetUserInput(WindowTitle.EnterMessage20, SyncParameters.PositioinPrediction.ToString(), 20));}
                catch { }
                Update();
            };
            devToolItem.Activated+=DevToolItem_Activated;
            devToolItem.Checked=false;

            MainMenu.Add(d1);
            MainMenu.Add(devToolItem);
            MainMenu.Add(boneIndexItem);
            MainMenu.AddSubMenu(DiagnosticMenu);
            MainMenu.Opening+=(sender, e) =>Update(); 
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

        private static void DevToolItem_Activated(object sender, EventArgs e)
        {
            if (devToolItem.Checked)
            {
                DevTool.ToMark=Game.Player.Character.CurrentVehicle;
            }
            else
            {
                DevTool.ToMark=null;
            }
        }

        private static void Update()
        {
            d1.AltTitle = SyncParameters.PositioinPrediction.ToString();
        }
    }
}
