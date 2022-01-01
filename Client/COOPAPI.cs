using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using CoopClient.Entities.Player;

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
        /// <param name="connected"></param>
        /// <param name="from">The Lidgren-Network net handle</param>
        /// <param name="reason"></param>
        public delegate void ConnectEvent(bool connected, long from, string reason = null);
        /// <summary>
        /// ?
        /// </summary>
        /// <param name="from"></param>
        /// <param name="message">The Lidgren-Network net handle</param>
        /// <param name="args"></param>
        public delegate void ChatMessage(string from, string message, CancelEventArgs args);
        /// <summary>
        /// ?
        /// </summary>
        /// <param name="from">The Lidgren-Network net handle</param>
        /// <param name="mod"></param>
        /// <param name="customID"></param>
        /// <param name="bytes"></param>
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
            OnConnection?.Invoke(true, GetLocalNetHandle());
        }

        internal static void Disconnected(string reason)
        {
            OnConnection?.Invoke(false, GetLocalNetHandle(), reason);
        }

        internal static void Connected(long netHandle)
        {
            OnConnection?.Invoke(true, netHandle);
        }

        internal static void Disconnected(long netHandle)
        {
            OnConnection?.Invoke(false, netHandle);
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
        /// Check if the player is already on a server
        /// </summary>
        public static bool IsOnServer()
        {
            return Main.MainNetworking.IsOnServer();
        }

        /// <summary>
        /// Get the local net handle from this Lidgren-Network client when connected to a server
        /// </summary>
        /// <returns>long</returns>
        public static long GetLocalNetHandle()
        {
            return Main.LocalNetHandle;
        }

        /// <summary>
        /// Get all connected player's as a Dictionary.
        /// Key = Lidgren-Network net handle
        /// Value = Character handle or null
        /// </summary>
        public static Dictionary<long, int?> GetAllPlayers()
        {
            Dictionary<long, int?> result = new Dictionary<long, int?>();
            lock (Main.Players)
            {
                foreach (KeyValuePair<long, EntitiesPlayer> player in Main.Players.Where(x => x.Key != Main.LocalNetHandle))
                {
                    result.Add(player.Key, player.Value.Character?.Handle);
                }
            }
            return result;
        }

        /// <summary>
        /// Get a player using their Lidgren Network net handle
        /// </summary>
        /// <param name="handle">Lidgren-Network net handle</param>
        public static EntitiesPlayer GetPlayer(long handle)
        {
            lock (Main.Players)
            {
                return Main.Players.ContainsKey(handle) ? Main.Players[handle] : null;
            }
        }

        /// <summary>
        /// Check if a GTACOOP:R menu is visible
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
        /// Check if the GTACOOP:R chat is visible
        /// </summary>
        public static bool IsChatFocused()
        {
            return Main.MainChat.Focused;
        }

        /// <summary>
        /// Check if the GTACOOP:R list of players is visible
        /// </summary>
        public static bool IsPlayerListVisible()
        {
            return Util.GetTickCount64() - PlayerList.Pressed < 5000;
        }

        /// <summary>
        /// Get the version of GTACOOP:R
        /// </summary>
        public static string GetCurrentVersion()
        {
            return Main.CurrentVersion;
        }

        /// <summary>
        /// Send any data (bytes) to the server
        /// </summary>
        /// <param name="modName">The name of this modification (script)</param>
        /// <param name="customID">The ID to know what the data is</param>
        /// <param name="bytes">Your class, structure or whatever in bytes</param>
        public static void SendDataToServer(string modName, byte customID, byte[] bytes)
        {
            Main.MainNetworking.SendModData(-1, modName, customID, bytes);
        }

        /// <summary>
        /// Send any data (bytes) to the all player
        /// </summary>
        /// <param name="modName">The name of this modification (script)</param>
        /// <param name="customID">The ID to know what the data is</param>
        /// <param name="bytes">Your class, structure or whatever in bytes</param>
        public static void SendDataToAll(string modName, byte customID, byte[] bytes)
        {
            Main.MainNetworking.SendModData(0, modName, customID, bytes);
        }

        /// <summary>
        /// Send any data (bytes) to a player
        /// </summary>
        /// <param name="netHandle">The Lidgren Network net handle that receives the data</param>
        /// <param name="modName">The name of this modification (script)</param>
        /// <param name="customID">The ID to know what the data is</param>
        /// <param name="bytes">Your class, structure or whatever in bytes</param>
        public static void SendDataToPlayer(long netHandle, string modName, byte customID, byte[] bytes)
        {
            Main.MainNetworking.SendModData(netHandle, modName, customID, bytes);
        }

        /// <summary>
        /// Get that player's local username
        /// </summary>
        public static string GetUsername()
        {
            return Main.MainSettings.Username;
        }

        /// <summary>
        /// Set a new username for this player
        /// </summary>
        /// <param name="username">The new username</param>
        /// <returns>false if the player already joined a server or the username is null or empty otherwise true</returns>
        public static bool SetUsername(string username)
        {
            if (IsOnServer() || string.IsNullOrEmpty(username))
            {
                return false;
            }

            Main.MainSettings.Username = username;

            return true;
        }

        /// <summary>
        /// Enable or disable sharing of NPCs with other players
        /// </summary>
        /// <param name="share"></param>
        public static void SetShareNPCs(bool share)
        {
            Main.ShareNPCsWithPlayers = share;
        }

        /// <summary>
        /// Enable or disable the local traffic for this player
        /// </summary>
        /// <param name="enable"></param>
        public static void SetLocalTraffic(bool enable)
        {
            Main.DisableTraffic = !enable;
        }

#if DEBUG
        /// <summary>
        /// ?
        /// </summary>
        /// <param name="value"></param>
        public static void SetDebug(bool value)
        {
            Main.UseDebug = value;
        }
#endif
    }
}
