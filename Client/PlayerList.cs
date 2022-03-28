using System.Collections.Generic;

using CoopClient.Entities.Player;

using GTA;
using GTA.Native;

namespace CoopClient
{
    internal class PlayerList
    {
        private const float LEFT_POSITION = 0.122f;
        private const float RIGHT_POSITION = 0.9f;
        private readonly Scaleform MainScaleform = new Scaleform("mp_mm_card_freemode");
        private ulong LastUpdate = Util.GetTickCount64();
        internal ulong Pressed { get; set; }

        internal bool LeftAlign = true;

        public void Tick()
        {
            if (!Main.MainNetworking.IsOnServer())
            {
                return;
            }

            if ((Util.GetTickCount64() - LastUpdate) >= 1000)
            {
                Update(Main.Players, Main.MainSettings.Username);
            }

            if ((Util.GetTickCount64() - Pressed) < 5000 && !Main.MainChat.Focused
#if !NON_INTERACTIVE
                && !Main.MainMenu.MenuPool.AreAnyVisible
#endif
                )
            {
                Function.Call(Hash.DRAW_SCALEFORM_MOVIE, MainScaleform.Handle, 
                                LeftAlign ? LEFT_POSITION : RIGHT_POSITION, 0.3f,
                                0.28f, 0.6f,
                                255, 255, 255, 255, 0);
            }
        }

        private void Update(Dictionary<long, EntitiesPlayer> players, string localUsername)
        {
            LastUpdate = Util.GetTickCount64();

            MainScaleform.CallFunction("SET_DATA_SLOT_EMPTY", 0);
            MainScaleform.CallFunction("SET_DATA_SLOT", 0, $"{Main.MainNetworking.Latency * 1000:N0}ms", localUsername, 116, 0, 0, "", "", 2, "", "", ' ');

            int i = 1;
            
            foreach (KeyValuePair<long, EntitiesPlayer> player in players)
            {
                MainScaleform.CallFunction("SET_DATA_SLOT", i++, $"{player.Value.Latency * 1000:N0}ms", player.Value.Username, 116, 0, i - 1, "", "", 2, "", "", ' ');
            }

            MainScaleform.CallFunction("SET_TITLE", "Player list", (players.Count + 1) + " players");
            MainScaleform.CallFunction("DISPLAY_VIEW");
        }
    }
}
