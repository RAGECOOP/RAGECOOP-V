﻿using System;
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
        public static NativeMenu Menu = new NativeMenu("RAGECOOP", "Debug", "Debug settings") { 
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

            Menu.Add(d1);
            Menu.Opening+=(sender, e) =>Update();

            Update();
        }
        private static void Update()
        {

            d1.AltTitle = SyncParameters.PositioinPrediction.ToString();
        }
    }
}