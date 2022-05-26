using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA;
using GTA.Math;
using System.Drawing;
using System.Windows.Forms;

namespace RageCoop.Client
{
    internal class DevTool:Script
    {
        public static Entity ToMark;
        public static int Current = 0;
        public DevTool()
        {
            Tick+=OnTick;
            KeyUp+=OnKeyUp;
        }

        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Right:
                    Current++;
                    DebugMenu.boneIndexItem.AltTitle= Current.ToString();
                    break;
                case Keys.Left:
                    Current--;
                    DebugMenu.boneIndexItem.AltTitle= Current.ToString();
                    break;
            }
        }

        private void OnTick(object sender, EventArgs e)
        {
            if(ToMark == null || !ToMark.Exists()){ return;}
            if ((Current< 0)||(Current>ToMark.Bones.Count-1)) {
                Current=0;
                DebugMenu.boneIndexItem.AltTitle= Current.ToString();
            }
            var bone = ToMark.Bones[Current];
            World.DrawLine(bone.Position, bone.Position+2*bone.ForwardVector, Color.Blue);
            World.DrawLine(bone.Position, bone.Position+2*bone.UpVector, Color.Green);
            World.DrawLine(bone.Position, bone.Position+2*bone.RightVector, Color.Yellow);
        }
    }
}
