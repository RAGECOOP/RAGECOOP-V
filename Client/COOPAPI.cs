using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace CoopClient
{
    /// <summary>
    /// ?
    /// </summary>
    public static class COOPAPI
    {
        #region DELEGATES
        /// <summary>
        /// ?
        /// </summary>
        public delegate void ConnectEvent(bool connected, long fromId, string reason = null);
        /// <summary>
        /// ?
        /// </summary>
        public delegate void ChatMessage(string from, string message, CancelEventArgs args);
        /// <summary>
        /// ?
        /// </summary>
        public delegate void ModEvent(long from, string mod, byte customID, byte[] bytes);
        #endregion

        #region EVENTS
        /// <summary>
        /// ?
        /// </summary>
        public static event ConnectEvent OnConnection;
        /// <summary>
        /// ?
        /// </summary>
        public static event ChatMessage OnChatMessage;
        /// <summary>
        /// ?
        /// </summary>
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

        /// <summary>
        /// Send a local chat message to this player
        /// </summary>
        /// <param name="from">Username of the player who sent this message</param>
        /// <param name="message">The player's message</param>
        public static void LocalChatMessage(string from, string message)
        {
            Main.MainChat.AddMessage(from, message);
        }

        /// <summary>
        /// ?
        /// </summary>
        public static void Connect(string serverAddress)
        {
            Main.MainNetworking.DisConnectFromServer(serverAddress);
        }

        /// <summary>
        /// ?
        /// </summary>
        public static void Disconnect()
        {
            Main.MainNetworking.DisConnectFromServer(null);
        }

        /// <summary>
        /// ?
        /// </summary>
        public static bool IsOnServer()
        {
            return Main.MainNetworking.IsOnServer();
        }

        /// <summary>
        /// Get the local ID from this Lidgren network client when connected to a server
        /// </summary>
        /// <returns>long</returns>
        public static long GetLocalID()
        {
            return Main.LocalClientID;
        }

        /// <summary>
        /// Get all connected player's as a Dictionary.
        /// Key = Lidgren-Network client ID
        /// Value = Character handle or null
        /// </summary>
        /// <returns>Dictionary(long, int)</returns>
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

        /// <summary>
        /// Get a player using their Lidgren Network Client ID
        /// </summary>
        /// <param name="lnID">Lidgren-Network client ID</param>
        /// <returns>Entities.EntitiesPlayer</returns>
        public static Entities.EntitiesPlayer GetPlayer(long lnID)
        {
            lock (Main.Players)
            {
                return Main.Players.ContainsKey(lnID) ? Main.Players[lnID] : null;
            }
        }

        /// <summary>
        /// ?
        /// </summary>
        public static bool IsMenuVisible()
        {
#if NON_INTERACTIVE
            return false;
#else
            return Main.MainMenu.MenuPool.AreAnyVisible;
#endif
        }

        /// <summary>
        /// ?
        /// </summary>
        public static bool IsChatFocused()
        {
            return Main.MainChat.Focused;
        }

        /// <summary>
        /// ?
        /// </summary>
        public static bool IsPlayerListVisible()
        {
            return Util.GetTickCount64() - PlayerList.Pressed < 5000;
        }

        /// <summary>
        /// ?
        /// </summary>
        public static string GetCurrentVersion()
        {
            return Main.CurrentVersion;
        }

        /// <summary>
        /// Send any data (bytes) to the server
        /// </summary>
        /// <param name="mod">The name of this modification (script)</param>
        /// <param name="customID">The ID to know what the data is</param>
        /// <param name="bytes">Your class, structure or whatever in bytes</param>
        public static void SendDataToServer(string mod, byte customID, byte[] bytes)
        {
            Main.MainNetworking.SendModData(-1, mod, customID, bytes);
        }

        /// <summary>
        /// Send any data (bytes) to the all player
        /// </summary>
        /// <param name="mod">The name of this modification (script)</param>
        /// <param name="customID">The ID to know what the data is</param>
        /// <param name="bytes">Your class, structure or whatever in bytes</param>
        public static void SendDataToAll(string mod, byte customID, byte[] bytes)
        {
            Main.MainNetworking.SendModData(0, mod, customID, bytes);
        }

        /// <summary>
        /// Send any data (bytes) to a player
        /// </summary>
        /// <param name="lnID">The Lidgren Network Client ID that receives the data</param>
        /// <param name="mod">The name of this modification (script)</param>
        /// <param name="customID">The ID to know what the data is</param>
        /// <param name="bytes">Your class, structure or whatever in bytes</param>
        public static void SendDataToPlayer(long lnID, string mod, byte customID, byte[] bytes)
        {
            Main.MainNetworking.SendModData(lnID, mod, customID, bytes);
        }

        /// <summary>
        /// Get that player's local username that has been set
        /// </summary>
        /// <returns>string</returns>
        public static string GetLocalUsername()
        {
            return Main.MainSettings.Username;
        }

        /// <summary>
        /// ?
        /// </summary>
        public static void Configure(string playerName, bool shareNpcsWithPlayers, int streamedNpcs, bool disableTrafficSharing, bool debug = false)
        {
            Main.MainSettings.Username = playerName;
            Main.ShareNpcsWithPlayers = shareNpcsWithPlayers;
            Main.MainSettings.StreamedNPCs = streamedNpcs;
            Main.DisableTraffic = disableTrafficSharing;
#if DEBUG
            Main.UseDebug = debug;
#endif
        }
    }
}
