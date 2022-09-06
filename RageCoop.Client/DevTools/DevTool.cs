using GTA;
using GTA.Math;
using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace RageCoop.Client
{
    internal class DevTool : Script
    {
        public static Vehicle ToMark;
        public static bool UseSecondary = false;
        public static int Current = 0;
        public static int Secondary = 0;
        public static MuzzleDir Direction = MuzzleDir.Forward;
        public DevTool()
        {
            Tick += OnTick;
            KeyDown += OnKeyDown;
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (ToMark == null || (!ToMark.Exists())) { return; }
            if (DevToolMenu.Menu.SelectedItem == DevToolMenu.boneIndexItem)
            {

                switch (e.KeyCode)
                {
                    case Keys.Right:
                        Current++;
                        break;
                    case Keys.Left:
                        Current--;
                        break;
                }
            }
            else if (DevToolMenu.Menu.SelectedItem == DevToolMenu.secondaryBoneIndexItem)
            {

                switch (e.KeyCode)
                {
                    case Keys.Right:
                        Secondary++;
                        break;
                    case Keys.Left:
                        Secondary--;
                        break;
                }
            }
            Update();
        }
        private static void Update()
        {

            if (Current > ToMark.Bones.Count - 1)
            {
                Current = 0;
            }
            else if (Current < 0)
            {
                Current = ToMark.Bones.Count - 1;
            }
            DevToolMenu.boneIndexItem.AltTitle = Current.ToString();
            if (Secondary > ToMark.Bones.Count - 1)
            {
                Secondary = 0;
            }
            else if (Secondary < 0)
            {
                Secondary = ToMark.Bones.Count - 1;
            }
            DevToolMenu.secondaryBoneIndexItem.AltTitle = Secondary.ToString();
        }
        private static void OnTick(object sender, EventArgs e)
        {
            if (ToMark == null || !ToMark.Exists()) { return; }
            Update();
            Draw(Current);
            if (UseSecondary)
            {
                Draw(Secondary);
            }

        }
        private static void Draw(int boneindex)
        {
            var bone = ToMark.Bones[boneindex];
            World.DrawLine(bone.Position, bone.Position + 2 * bone.ForwardVector, Color.Blue);
            World.DrawLine(bone.Position, bone.Position + 2 * bone.UpVector, Color.Green);
            World.DrawLine(bone.Position, bone.Position + 2 * bone.RightVector, Color.Yellow);
            Vector3 todraw = bone.ForwardVector;
            switch ((byte)Direction)
            {
                case 0:
                    todraw = bone.ForwardVector;
                    break;
                case 1:
                    todraw = bone.RightVector;
                    break;
                case 2:
                    todraw = bone.UpVector;
                    break;
                case 3:
                    todraw = bone.ForwardVector * -1;
                    break;
                case 4:
                    todraw = bone.RightVector * -1;
                    break;
                case 5:
                    todraw = bone.UpVector * -1;
                    break;
            }
            World.DrawLine(bone.Position, bone.Position + 10 * todraw, Color.Red);
        }
        public static void CopyToClipboard(MuzzleDir dir)
        {

            if (ToMark != null)
            {
                string s;
                if (UseSecondary)
                {
                    if ((byte)dir < 3)
                    {
                        s = $@"
                        // {ToMark.DisplayName}
                        case {ToMark.Model.Hash}:
                            return BulletsShot%2==0 ? {Current} : {Secondary};
                        ";
                    }
                    else
                    {
                        s = $@"
                        // {ToMark.DisplayName}
                        case {ToMark.Model.Hash}:
                            return BulletsShot%2==0 ? {Current} : {Secondary};
                        ";
                    }
                }
                else
                {
                    if ((byte)dir < 3)
                    {
                        s = $@"
                        // {ToMark.DisplayName}
                        case {ToMark.Model.Hash}:
                            return {Current};
                        ";
                    }
                    else
                    {
                        s = $@"
                        // {ToMark.DisplayName}
                        case {ToMark.Model.Hash}:
                            return {Current};
                        ";
                    }
                }
                Thread thread = new Thread(() => Clipboard.SetText(s));
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                thread.Join();
                GTA.UI.Notification.Show("Copied to clipboard, please paste it on the GitHub issue page!");
            }
        }

    }
    internal enum MuzzleDir : byte
    {
        Forward = 0,
        Right = 1,
        Up = 2,
        Backward = 3,
        Left = 4,
        Down = 5,
    }
}
