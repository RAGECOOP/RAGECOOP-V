using GTA;
using GTA.Math;
using GTA.Native;
using RageCoop.Core;
using System.Collections.Generic;
using Lidgren.Network;
using System.Net;

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
        public static Dictionary<int, Player> Players = new Dictionary<int, Player> { };
        public static Dictionary<string, Player> PendingConnections = new Dictionary<string, Player>();
        public static void Tick()
        {
            if (!Networking.IsOnServer)
            {
                return;
            }

            if ((Util.GetTickCount64() - _lastUpdate) >= 1000)
            {
                Update();
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

        private static void Update()
        {
            _lastUpdate = Util.GetTickCount64();

            _mainScaleform.CallFunction("SET_DATA_SLOT_EMPTY", 0);
            
            int i=0;

            foreach (var player in Players.Values)
            {
                _mainScaleform.CallFunction("SET_DATA_SLOT", i++, $"{(player.PedID==Main.LocalPlayerID ? Networking.Latency : player.Latency) * 1000:N0}ms", player.Username, 116, 0, i - 1, "", "", 2, "", "", ' ');
            }

            _mainScaleform.CallFunction("SET_TITLE", "Player list", $"{Players.Count} players");
            _mainScaleform.CallFunction("DISPLAY_VIEW");
        }
        public static void SetPlayer(int id, string username, float latency = 0)
        {
            Main.Logger.Debug($"{id},{username},{latency}");
            Player p;
            if (Players.TryGetValue(id, out p))
            {
                p.Username=username;
                p.PedID=id;
                p._latencyToServer=latency;
            }
            else
            {
                p = new Player { PedID=id, Username=username, _latencyToServer=latency };
                Players.Add(id, p);
            }
        }
        public static void UpdatePlayer(Packets.PlayerInfoUpdate packet)
        {
            var p = GetPlayer(packet.PedID);
            if (p!=null)
            {
                p._latencyToServer = packet.Latency;
                p.Position = packet.Position;
            }
        }
        public static Player GetPlayer(int id)
        {
            Player p;
            Players.TryGetValue(id, out p);
            return p;
        }
        public static Player GetPlayer(SyncedPed p)
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
            Players=new Dictionary<int, Player> { };
        }

    }


    internal class Player
    {
        public string Username { get; internal set; }
        /// <summary>
        /// Universal character ID.
        /// </summary>
        public int PedID
        {
            get; internal set;
        }
        public Vector3 Position { get; set; }
        public SyncedPed Character { get; set; }
        /// <summary>
        /// Player Latency in seconds, will be the latency to server if not using P2P connection.
        /// </summary>
        public float Latency => HasDirectConnection ? Connection.AverageRoundtripTime/2 : _latencyToServer;
        public float PacketTravelTime => HasDirectConnection ? Connection.AverageRoundtripTime/2 : Networking.Latency+_latencyToServer;
        public float _latencyToServer = 0;
        public bool DisplayNameTag { get; set; } = true;
        public NetConnection Connection { get; set; }
        public bool HasDirectConnection => Connection?.Status==NetConnectionStatus.Connected;
    }
}
