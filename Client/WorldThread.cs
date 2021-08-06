using System;

using GTA;
using GTA.Native;

namespace CoopClient
{
    public class WorldThread : Script
    {
        public WorldThread()
        {
            Tick += OnTick;
            Interval = 1000 / 60;
        }

        public static void OnTick(object sender, EventArgs e)
        {
            if (Game.IsLoading)
            {
                return;
            }

            Function.Call((Hash)0xB96B00E976BE977F, 0.0f); // _SET_WAVES_INTENSITY

            Function.Call(Hash.SET_CAN_ATTACK_FRIENDLY, Game.Player.Character.Handle, true, false);
            Function.Call(Hash.SET_PED_CAN_BE_TARGETTED, Game.Player.Character.Handle, true);
        }
    }
}
