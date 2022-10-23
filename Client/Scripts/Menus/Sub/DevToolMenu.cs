using System;
using System.Drawing;
using System.IO;
using GTA;
using GTA.Native;
using GTA.UI;
using LemonUI.Menus;
using Newtonsoft.Json;
using Console = GTA.Console;

namespace RageCoop.Client
{
    internal class AnimDic
    {
        public string[] Animations;
        public string DictionaryName;
    }

    internal static class DevToolMenu
    {
        private const string AnimationsPath = @"RageCoop\Data\animDictsCompact.json";

        public static NativeMenu Menu = new NativeMenu("RAGECOOP", "DevTool", "Internal testing tools")
        {
            UseMouse = false,
            Alignment = Main.Settings.FlipMenu ? Alignment.Right : Alignment.Left
        };

        private static readonly NativeCheckboxItem enableItem = new NativeCheckboxItem("Enable");
        public static readonly NativeItem dumpItem = new NativeItem("Dump vehicle weapons");
        public static readonly NativeItem dumpFixItem = new NativeItem("Dump weapon fixes");
        public static readonly NativeItem dumpWHashItem = new NativeItem("Dump WeaponHash.cs");
        public static readonly NativeItem getAnimItem = new NativeItem("Get current animation");

        public static readonly NativeItem dumpVWHashItem = new NativeItem("Dump VehicleWeaponHash.cs");

        static DevToolMenu()
        {
            Menu.Banner.Color = Color.FromArgb(225, 0, 0, 0);
            Menu.Title.Color = Color.FromArgb(255, 165, 0);

            enableItem.Activated += enableItem_Activated;
            enableItem.Checked = false;

            dumpItem.Activated += DumpItem_Activated;
            dumpVWHashItem.Activated += (s, e) => WeaponUtil.DumpVehicleWeaponHashes();
            dumpWHashItem.Activated += (s, e) => WeaponUtil.DumpWeaponHashes();
            dumpFixItem.Activated += (s, e) => WeaponUtil.DumpWeaponFix();
            getAnimItem.Activated += (s, e) =>
            {
                if (File.Exists(AnimationsPath))
                {
                    var anims = JsonConvert.DeserializeObject<AnimDic[]>(File.ReadAllText(AnimationsPath));
                    foreach (var anim in anims)
                    foreach (var a in anim.Animations)
                        if (Function.Call<bool>(Hash.IS_ENTITY_PLAYING_ANIM, Main.P, anim.DictionaryName, a, 3))
                        {
                            Console.Info(anim.DictionaryName + " : " + a);
                            Notification.Show(anim.DictionaryName + " : " + a);
                        }
                }
                else
                {
                    Notification.Show($"~r~{AnimationsPath} not found");
                }
            };

            Menu.Add(enableItem);
            Menu.Add(dumpItem);
            Menu.Add(dumpVWHashItem);
            Menu.Add(dumpWHashItem);
            Menu.Add(dumpFixItem);
            Menu.Add(getAnimItem);
        }

        private static void DumpItem_Activated(object sender, EventArgs e)
        {
            dumpItem.Enabled = false;
            Directory.CreateDirectory(@"RageCoop\Data\tmp");
            var input = @"RageCoop\Data\tmp\vehicles.json";
            var dumpLocation = @"RageCoop\Data\VehicleWeapons.json";
            try
            {
                VehicleWeaponInfo.Dump(input, dumpLocation);
                Console.Info("Weapon info dumped to " + dumpLocation);
            }
            catch (Exception ex)
            {
                Console.Error("~r~" + ex);
            }
            finally
            {
                dumpItem.Enabled = true;
            }
        }

        private static void enableItem_Activated(object sender, EventArgs e)
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