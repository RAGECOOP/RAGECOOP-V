using GTA;
using LemonUI.Menus;
using System;
using System.Drawing;

namespace RageCoop.Client
{
    internal static class DevToolMenu
    {
        public static NativeMenu Menu = new NativeMenu("RAGECOOP", "DevTool", "Help with the development")
        {
            UseMouse = false,
            Alignment = Main.Settings.FlipMenu ? GTA.UI.Alignment.Right : GTA.UI.Alignment.Left
        };
        private static readonly NativeCheckboxItem enableItem = new NativeCheckboxItem("Enable");

        private static readonly NativeCheckboxItem enableSecondaryItem = new NativeCheckboxItem("Secondary", "Enable if this vehicle have two muzzles");
        public static NativeItem boneIndexItem = new NativeItem("Current bone index");
        public static NativeItem secondaryBoneIndexItem = new NativeItem("Secondary bone index");
        public static NativeItem clipboardItem = new NativeItem("Copy to clipboard");
        public static NativeListItem<MuzzleDir> dirItem = new NativeListItem<MuzzleDir>("Direction");
        static DevToolMenu()
        {
            Menu.Banner.Color = Color.FromArgb(225, 0, 0, 0);
            Menu.BannerText.Color = Color.FromArgb(255, 165, 0);

            enableItem.Activated += enableItem_Activated;
            enableItem.Checked = false;
            enableSecondaryItem.CheckboxChanged += EnableSecondaryItem_Changed;

            secondaryBoneIndexItem.Enabled = false;
            clipboardItem.Activated += ClipboardItem_Activated;
            dirItem.ItemChanged += DirItem_ItemChanged;
            foreach (var d in Enum.GetValues(typeof(MuzzleDir)))
            {
                dirItem.Items.Add((MuzzleDir)d);
            }
            dirItem.SelectedIndex = 0;

            Menu.Add(enableItem);
            Menu.Add(boneIndexItem);
            Menu.Add(enableSecondaryItem);
            Menu.Add(secondaryBoneIndexItem);
            Menu.Add(dirItem);
            Menu.Add(clipboardItem);
        }

        private static void EnableSecondaryItem_Changed(object sender, EventArgs e)
        {
            if (enableSecondaryItem.Checked)
            {
                DevTool.UseSecondary = true;
                secondaryBoneIndexItem.Enabled = true;
            }
            else
            {
                DevTool.UseSecondary = false;
                secondaryBoneIndexItem.Enabled = false;
            }
        }

        private static void DirItem_ItemChanged(object sender, ItemChangedEventArgs<MuzzleDir> e)
        {
            DevTool.Direction = dirItem.SelectedItem;
        }

        private static void ClipboardItem_Activated(object sender, EventArgs e)
        {
            DevTool.CopyToClipboard(dirItem.SelectedItem);
        }

        private static void enableItem_Activated(object sender, EventArgs e)
        {
            if (enableItem.Checked)
            {
                DevTool.ToMark = Game.Player.Character.CurrentVehicle;
            }
            else
            {
                DevTool.ToMark = null;
            }
        }
    }
}
