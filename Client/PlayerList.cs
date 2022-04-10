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
        private readonly Scaleform _mainScaleform = new Scaleform("mp_mm_card_freemode");
        private ulong _lastUpdate = Util.GetTickCount64();
        internal ulong Pressed { get; set; }

        internal bool LeftAlign = true;

        public void Tick()
        {
            if (!Main.MainNetworking.IsOnServer())
            {
                return;
            }

            if ((Util.GetTickCount64() - _lastUpdate) >= 1000)
            {
                Update(Main.Players, Main.MainSettings.Username);
            }

            if ((Util.GetTickCount64() - Pressed) < 5000 && !Main.MainChat.Focused
#if !NON_INTERACTIVE
                && !Main.MainMenu.MenuPool.AreAnyVisible
#endif
                )
            {
                Function.Call(Hash.DRAW_SCALEFORM_MOVIE, _mainScaleform.Handle, 
                                LeftAlign ? LEFT_POSITION : RIGHT_POSITION, 0.3f,
                                0.28f, 0.6f,
                                255, 255, 255, 255, 0);
            }
        }

        private void Update(Dictionary<long, EntitiesPlayer> players, string localUsername)
        {
            _lastUpdate = Util.GetTickCount64();

            _mainScaleform.CallFunction("SET_DATA_SLOT_EMPTY", 0);
            _mainScaleform.CallFunction("SET_DATA_SLOT", 0, $"{Main.MainNetworking.Latency * 1000:N0}ms", localUsername, 116, 0, 0, "", "", 2, "", "", ' ');

            int i = 1;
            
            foreach (KeyValuePair<long, EntitiesPlayer> player in players)
            {
                _mainScaleform.CallFunction("SET_DATA_SLOT", i++, $"{player.Value.Latency * 1000:N0}ms", player.Value.Username, 116, 0, i - 1, "", "", 2, "", "", ' ');
            }

            _mainScaleform.CallFunction("SET_TITLE", "Player list", (players.Count + 1) + " players");
            _mainScaleform.CallFunction("DISPLAY_VIEW");
        }
    }
}
