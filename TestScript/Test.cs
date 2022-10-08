using System;
using GTA;
using GTA.UI;

namespace TestScript
{
    public class Test : Script
    {
        public Test()
        {
            Tick += OnTick;
        }

        private void OnTick(object sender, EventArgs e)
        {
            Screen.ShowHelpTextThisFrame("bruh");
        }
    }
}
