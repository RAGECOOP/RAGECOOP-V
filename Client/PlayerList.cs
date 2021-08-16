using System;
using System.Collections.Generic;

using CoopClient.Entities;

using GTA;
using GTA.Native;

namespace CoopClient
{
    public class PlayerList : Script
    {
        private readonly Scaleform MainScaleform = new Scaleform("mp_mm_card_freemode");
        private int LastUpdate = Environment.TickCount;
        public static int Pressed { get; set; }

        public PlayerList()
        {
            Init();

            Tick += OnTick;
        }

        private void OnTick(object sender, EventArgs e)
        {
            if ((Environment.TickCount - LastUpdate) >= 1000)
            {
                Update(Main.Players, Main.MainSettings.Username);
            }

            if (!Main.MainNetworking.IsOnServer())
            {
                return;
            }

            if ((Environment.TickCount - Pressed) < 5000 && !Main.MainChat.Focused && !Main.MainMenu.MenuPool.AreAnyVisible)
            {
                Function.Call(Hash.DRAW_SCALEFORM_MOVIE, MainScaleform.Handle, 0.122f, 0.3f, 0.28f, 0.6f, 255, 255, 255, 255, 0);
            }
        }

        private void Init()
        {
            MainScaleform.CallFunction("SET_DATA_SLOT_EMPTY", 0);
            MainScaleform.CallFunction("SET_DATA_SLOT", 0, "", "Me", 116, 0, 0, "", "", 2, "", "", ' ');
            MainScaleform.CallFunction("SET_TITLE", "Player list", "1 players");
            MainScaleform.CallFunction("DISPLAY_VIEW");
        }

        private void Update(Dictionary<long, EntitiesPlayer> players, string localUsername)
        {
            LastUpdate = Environment.TickCount;

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
