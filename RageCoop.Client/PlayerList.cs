using GTA;
using GTA.Native;
using RageCoop.Core;
using System.Collections.Generic;

namespace RageCoop.Client
{
    internal static class PlayerList
    {
        private const float LEFT_POSITION = 0.122f;
        private const float RIGHT_POSITION = 0.9f;
        private static readonly Scaleform _mainScaleform = new Scaleform("mp_mm_card_freemode");
        private static ulong _lastUpdate = Util.GetTickCount64();
        public static ulong Pressed { get; set; }

        public static bool LeftAlign = true;
        public static Dictionary<int, PlayerData> Players = new Dictionary<int, PlayerData> { };
        public static void Tick()
        {
            if (!Networking.IsOnServer)
            {
                return;
            }

            if ((Util.GetTickCount64() - _lastUpdate) >= 1000)
            {
                Update(Main.Settings.Username);
            }

            if ((Util.GetTickCount64() - Pressed) < 5000 && !Main.MainChat.Focused
#if !NON_INTERACTIVE
                && !Menus.CoopMenu.MenuPool.AreAnyVisible
#endif
                )
            {
                Function.Call(Hash.DRAW_SCALEFORM_MOVIE, _mainScaleform.Handle,
                                LeftAlign ? LEFT_POSITION : RIGHT_POSITION, 0.3f,
                                0.28f, 0.6f,
                                255, 255, 255, 255, 0);
            }
        }

        private static void Update(string localUsername)
        {
            _lastUpdate = Util.GetTickCount64();

            _mainScaleform.CallFunction("SET_DATA_SLOT_EMPTY", 0);
            
            int i=0;

            foreach (var player in Players)
            {
                _mainScaleform.CallFunction("SET_DATA_SLOT", i++, $"{player.Value.Latency * 1000:N0}ms", player.Value.Username, 116, 0, i - 1, "", "", 2, "", "", ' ');
            }

            _mainScaleform.CallFunction("SET_TITLE", "Player list", $"{Players.Count} players");
            _mainScaleform.CallFunction("DISPLAY_VIEW");
        }
        public static void SetPlayer(int id, string username, float latency = 0)
        {
            if(id == Main.LocalPlayerID) { Networking.Latency=latency; }
            Main.Logger.Debug($"{id},{username},{latency}");
            PlayerData p;
            if (Players.TryGetValue(id, out p))
            {
                p.Username=username;
                p.PedID=id;
                p.Latency=latency;
            }
            else
            {
                p = new PlayerData { PedID=id, Username=username, Latency=latency };
                Players.Add(id, p);
            }
        }
        public static void UpdatePlayer(Packets.PlayerInfoUpdate packet)
        {
            if (packet.PedID == Main.LocalPlayerID) {Main.Logger.Debug("Latency updated"); Networking.Latency=packet.Latency; }
            var p = GetPlayer(packet.PedID);
            if (p!=null)
            {
                p.Latency= packet.Latency;
            }
        }
        public static PlayerData GetPlayer(int id)
        {
            PlayerData p;
            Players.TryGetValue(id, out p);
            return p;
        }
        public static PlayerData GetPlayer(SyncedPed p)
        {
            var player = GetPlayer(p.ID);
            if (player!=null)
            {
                player.Character=p;
            }
            return player;
        }
        public static void RemovePlayer(int id)
        {
            if (Players.ContainsKey(id))
            {
                Players.Remove(id);
            }
        }
        public static void Cleanup()
        {
            Players=new Dictionary<int, PlayerData> { };
        }

    }


    internal class PlayerData
    {
        public string Username { get; internal set; }
        /// <summary>
        /// Universal character ID.
        /// </summary>
        public int PedID
        {
            get; internal set;
        }
        public SyncedPed Character { get; set; }
        /// <summary>
        /// Player Latency in second.
        /// </summary>
        public float Latency { get; set; }

        public bool DisplayNameTag { get; set; } = true;
    }
}
