using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace CoopClient
{
    public static class Interface
    {
        #region DELEGATES
        public delegate void ConnectEvent(bool connected, long fromId, string reason = null);
        public delegate void ChatMessage(string from, string message, CancelEventArgs args);
        public delegate void ModEvent(long from, string mod, byte customID, byte[] bytes);
        #endregion

        #region EVENTS
        public static event ConnectEvent OnConnection;
        public static event ChatMessage OnChatMessage;
        public static event ModEvent OnModPacketReceived;

        internal static void Connected()
        {
            OnConnection?.Invoke(true, GetLocalID());
        }

        internal static void Disconnected(string reason)
        {
            OnConnection?.Invoke(false, GetLocalID(), reason);
        }

        internal static void Connected(long userId)
        {
            OnConnection?.Invoke(true, userId);
        }

        internal static void Disconnected(long userId)
        {
            OnConnection?.Invoke(false, userId);
        }

        internal static void ModPacketReceived(long from, string mod, byte customID, byte[] bytes)
        {
            OnModPacketReceived?.Invoke(from, mod, customID, bytes);
        }

        internal static bool ChatMessageReceived(string from, string message)
        {
            CancelEventArgs args = new CancelEventArgs(false);
            OnChatMessage?.Invoke(from, message, args);
            return args.Cancel;
        }
        #endregion

        public static void SendChatMessage(string from, string message)
        {
            Main.MainChat.AddMessage(from, message);
        }

        public static void Connect(string serverAddress)
        {
            Main.MainNetworking.DisConnectFromServer(serverAddress);
        }

        public static void Disconnect()
        {
            Main.MainNetworking.DisConnectFromServer(null);
        }

        public static bool IsOnServer()
        {
            return Main.MainNetworking.IsOnServer();
        }

        public static long GetLocalID()
        {
            return Main.LocalClientID;
        }

        public static Dictionary<long, int?> GetAllPlayers()
        {
            Dictionary<long, int?> result = new Dictionary<long, int?>();
            lock (Main.Players)
            {
                foreach (KeyValuePair<long, Entities.EntitiesPlayer> player in Main.Players.Where(x => x.Key != Main.LocalClientID))
                {
                    result.Add(player.Key, player.Value.Character?.Handle);
                }
            }
            return result;
        }

        public static Entities.EntitiesPlayer GetPlayer(long playerId)
        {
            return Main.Players.ContainsKey(playerId) ? Main.Players[playerId] : null;
        }

        public static bool IsMenuVisible()
        {
#if NON_INTERACTIVE
            return false;
#else
            return Main.MainMenu.MenuPool.AreAnyVisible;
#endif
        }

        public static bool IsChatFocused()
        {
            return Main.MainChat.Focused;
        }

        public static bool IsPlayerListVisible()
        {
            return Util.GetTickCount64() - PlayerList.Pressed < 5000;
        }

        public static string GetCurrentVersion()
        {
            return Main.CurrentVersion;
        }

        // Send bytes to all players
        public static void SendDataToAll(string mod, byte customID, byte[] bytes)
        {
            Main.MainNetworking.SendModData(0, mod, customID, bytes);
        }

        // Send bytes to target
        public static void SendDataToPlayer(long target, string mod, byte customID, byte[] bytes)
        {
            Main.MainNetworking.SendModData(target, mod, customID, bytes);
        }

        public static void Configure(string playerName, bool shareNpcsWithPlayers, int streamedNpcs, bool debug = false)
        {
            Main.MainSettings.Username = playerName;
            Main.ShareNpcsWithPlayers = shareNpcsWithPlayers;
            Main.MainSettings.StreamedNpc = streamedNpcs;
#if DEBUG
            Main.UseDebug = debug;
#endif
        }
    }
}
