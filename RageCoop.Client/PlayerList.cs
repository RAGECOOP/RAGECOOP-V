using GTA;
using GTA.Math;
using GTA.Native;
using Lidgren.Network;
using RageCoop.Core;
using System.Collections.Generic;
using System.Linq;
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

            int i = 0;

            foreach (var player in Players.Values)
            {
                _mainScaleform.CallFunction("SET_DATA_SLOT", i++, $"{player.Ping * 1000:N0}ms", player.Username + (player.IsHost ? " (Host)" : ""), 116, 0, i - 1, "", "", 2, "", "", ' ');
            }

            _mainScaleform.CallFunction("SET_TITLE", "Player list", $"{Players.Count} players");
            _mainScaleform.CallFunction("DISPLAY_VIEW");
        }
        public static void SetPlayer(int id, string username, float latency = 0)
        {
            Main.Logger.Debug($"{id},{username},{latency}");
            if (Players.TryGetValue(id, out Player p))
            {
                p.Username = username;
                p.ID = id;
                p._latencyToServer = latency;
            }
            else
            {
                p = new Player { ID = id, Username = username, _latencyToServer = latency };
                Players.Add(id, p);
            }
        }
        public static void UpdatePlayer(Packets.PlayerInfoUpdate packet)
        {
            var p = GetPlayer(packet.PedID);
            if (p != null)
            {
                p._latencyToServer = packet.Latency;
                p.Position = packet.Position;
                p.IsHost = packet.IsHost;
                Main.QueueAction(() =>
                {
                    if (p.FakeBlip?.Exists() != true)
                    {
                        p.FakeBlip = World.CreateBlip(p.Position);
                    }
                    if (EntityPool.PedExists(p.ID))
                    {
                        p.FakeBlip.DisplayType = BlipDisplayType.NoDisplay;
                    }
                    else
                    {
                        p.FakeBlip.Color = Scripting.API.Config.BlipColor;
                        p.FakeBlip.Scale = Scripting.API.Config.BlipScale;
                        p.FakeBlip.Sprite = Scripting.API.Config.BlipSprite;
                        p.FakeBlip.DisplayType = BlipDisplayType.Default;
                        p.FakeBlip.Position = p.Position;
                    }
                });

            }
        }
        public static Player GetPlayer(int id)
        {
            Players.TryGetValue(id, out Player p);
            return p;
        }
        public static Player GetPlayer(SyncedPed p)
        {
            var player = GetPlayer(p.ID);
            if (player != null)
            {
                player.Character = p;
            }
            return player;
        }
        public static void RemovePlayer(int id)
        {
            if (Players.TryGetValue(id, out var player))
            {
                Players.Remove(id);
                Main.QueueAction(() => player.FakeBlip?.Delete());
            }
        }
        public static void Cleanup()
        {
            foreach (var p in Players.Values.ToArray())
            {
                p.FakeBlip?.Delete();
            }
            Players = new Dictionary<int, Player> { };
        }
    }

    public class Player
    {
        public byte HolePunchStatus { get; internal set; } = 1;
        public bool IsHost { get; internal set; }
        public string Username { get; internal set; }
        /// <summary>
        /// Universal ped ID.
        /// </summary>
        public int ID
        {
            get; internal set;
        }
        public IPEndPoint InternalEndPoint { get; internal set; }
        public IPEndPoint ExternalEndPoint { get; internal set; }
        internal bool ConnectWhenPunched { get; set; }
        public Blip FakeBlip { get; internal set; }
        public Vector3 Position { get; internal set; }
        public SyncedPed Character { get; internal set; }
        /// <summary>
        /// Player round-trip time in seconds, will be the rtt to server if not using P2P connection.
        /// </summary>
        public float Ping => Main.LocalPlayerID == ID ? Networking.Latency * 2 : (HasDirectConnection ? Connection.AverageRoundtripTime : _latencyToServer * 2);
        public float PacketTravelTime => HasDirectConnection ? Connection.AverageRoundtripTime / 2 : Networking.Latency + _latencyToServer;
        internal float _latencyToServer = 0;
        public bool DisplayNameTag { get; set; } = true;
        public NetConnection Connection { get; internal set; }
        public bool HasDirectConnection => Connection?.Status == NetConnectionStatus.Connected;
    }
}
