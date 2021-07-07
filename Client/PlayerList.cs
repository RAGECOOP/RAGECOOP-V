using System;
using System.Collections.Generic;

using CoopClient.Entities;

using GTA;
using GTA.Native;

namespace CoopClient
{
    public class PlayerList
    {
        private readonly Scaleform MainScaleform = new Scaleform("mp_mm_card_freemode");
        public int Pressed { get; set; }

        public void Init(string localUsername)
        {
            MainScaleform.CallFunction("SET_DATA_SLOT_EMPTY", 0);
            MainScaleform.CallFunction("SET_DATA_SLOT", 0, "", localUsername, 116, 0, 0, "", "", 2, "", "", ' ');
            MainScaleform.CallFunction("SET_TITLE", "Player list", "1 players");
            MainScaleform.CallFunction("DISPLAY_VIEW");
        }

        public void Update(Dictionary<string, EntitiesPlayer> players, string LocalUsername)
        {
            MainScaleform.CallFunction("SET_DATA_SLOT_EMPTY", 0);
            MainScaleform.CallFunction("SET_DATA_SLOT", 0, "", LocalUsername, 116, 0, 0, "", "", 2, "", "", ' ');

            int i = 1;
            foreach (KeyValuePair<string, EntitiesPlayer> player in players)
            {
                MainScaleform.CallFunction("SET_DATA_SLOT", i++, "", player.Value.Username, 116, 0, i - 1, "", "", 2, "", "", ' ');
            }

            MainScaleform.CallFunction("SET_TITLE", "Player list", (players.Count + 1) + " players");
            MainScaleform.CallFunction("DISPLAY_VIEW");
        }

        public void Tick()
        {
            if ((Environment.TickCount - Pressed) < 5000)
            {
                Function.Call(Hash.DRAW_SCALEFORM_MOVIE, MainScaleform.Handle, 0.122f, 0.3f, 0.28f, 0.6f, 255, 255, 255, 255, 0);
            }
        }
    }
}
