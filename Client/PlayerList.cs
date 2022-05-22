using System.Collections.Generic;
using System.Linq;
using RageCoop.Core;
using GTA;
using GTA.Native;

namespace RageCoop.Client
{
    public class PlayerList
    {
        private const float LEFT_POSITION = 0.122f;
        private const float RIGHT_POSITION = 0.9f;
        private readonly Scaleform _mainScaleform = new Scaleform("mp_mm_card_freemode");
        private ulong _lastUpdate = Util.GetTickCount64();
        public ulong Pressed { get; set; }

        public bool LeftAlign = true;
        public List<PlayerData> Players=new List<PlayerData> { };
        public void Tick()
        {
            if (!Main.MainNetworking.IsOnServer())
            {
                return;
            }

            if ((Util.GetTickCount64() - _lastUpdate) >= 1000)
            {
                Update( Main.Settings.Username);
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

        private void Update( string localUsername)
        {
            _lastUpdate = Util.GetTickCount64();

            _mainScaleform.CallFunction("SET_DATA_SLOT_EMPTY", 0);
            _mainScaleform.CallFunction("SET_DATA_SLOT", 0, $"{Main.MainNetworking.Latency * 1000:N0}ms", localUsername, 116, 0, 0, "", "", 2, "", "", ' ');

            int i = 1;
            
            foreach (var player in Players)
            {
                _mainScaleform.CallFunction("SET_DATA_SLOT", i++, $"{player.Latency * 1000:N0}ms", player.Username, 116, 0, i - 1, "", "", 2, "", "", ' ');
            }

            _mainScaleform.CallFunction("SET_TITLE", "Player list", (Players.Count) + " players");
            _mainScaleform.CallFunction("DISPLAY_VIEW");
        }
        public void SetPlayer(int id, string username)
        {
            
            var toset = Players.Where(x => x.PedID==id);
            if (toset.Any())
            {
                var p=toset.First();
                p.Username=username;
                p.PedID=id;
            }
            else
            {
                PlayerData p = new PlayerData { PedID=id, Username=username };
                Players.Add(p);
            }
        }
        public PlayerData GetPlayer(int id)
        {
            return Players.Find(x => x.PedID==id);
        }
        public void RemovePlayer(int id)
        {
            var p = Players.Where(x => x.PedID==id);
            if (p.Any())
            {
                Players.Remove(p.First());
            }
        }

    }
}
