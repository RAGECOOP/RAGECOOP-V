using System;
using System.Drawing;
using System.IO;
using GTA;
using GTA.Native;
using GTA.UI;
using LemonUI.Menus;
using Newtonsoft.Json;
using RageCoop.Core;



namespace RageCoop.Client
{
    internal static class DevToolMenu
    {
        public static NativeMenu Menu = new NativeMenu("RAGECOOP", "DevTool", "Internal testing tools")
        {
            UseMouse = false,
            Alignment = Main.Settings.FlipMenu ? Alignment.Right : Alignment.Left
        };

        private static readonly NativeCheckboxItem enableItem = new NativeCheckboxItem("Show weapon bones");
        public static readonly NativeItem DumpFixItem = new NativeItem("Dump weapon fixes");
        public static readonly NativeItem GetAnimItem = new NativeItem("Get current animation");
        public static readonly NativeItem DumpVwHashItem = new NativeItem("Dump VehicleWeaponHash.cs");

        static DevToolMenu()
        {
            Menu.Banner.Color = Color.FromArgb(225, 0, 0, 0);
            Menu.Title.Color = Color.FromArgb(255, 165, 0);

            enableItem.Activated += ShowBones;
            enableItem.Checked = false;
            DumpFixItem.Activated += (s, e) => WeaponUtil.DumpWeaponFix(WeaponFixDataPath);
            GetAnimItem.Activated += (s, e) =>
            {
                if (File.Exists(AnimationsDataPath))
                {
                    var anims = JsonDeserialize<AnimDic[]>(File.ReadAllText(AnimationsDataPath));
                    foreach (var anim in anims)
                    foreach (var a in anim.Animations)
                        if (Call<bool>(IS_ENTITY_PLAYING_ANIM, Main.P, anim.DictionaryName, a, 3))
                        {
                            Console.PrintInfo(anim.DictionaryName + " : " + a);
                            Notification.Show(anim.DictionaryName + " : " + a);
                        }
                }
                else
                {
                    Notification.Show($"~r~{AnimationsDataPath} not found");
                }
            };

            Menu.Add(enableItem);
            Menu.Add(DumpVwHashItem);
            Menu.Add(DumpFixItem);
            Menu.Add(GetAnimItem);
        }

        private static void ShowBones(object sender, EventArgs e)
        {
            if (enableItem.Checked)
            {
                DevTool.ToMark = Game.Player.Character.CurrentVehicle;
                DevTool.Instance.Resume();
            }
            else
            {
                DevTool.Instance.Pause();
                DevTool.ToMark = null;
            }
        }
    }
}