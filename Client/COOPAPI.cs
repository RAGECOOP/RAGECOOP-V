#undef DEBUG
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using RageCoop.Core;

namespace RageCoop.Client
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
        /// <param name="from">The player's id</param>
        /// <param name="reason"></param>
        public delegate void ConnectEvent(bool connected, int from, string reason = null);
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

        public static void Connected()
        {
            OnConnection?.Invoke(true, GetPlayerID());
        }

        public static void Disconnected(string reason)
        {
            OnConnection?.Invoke(false, GetPlayerID(), reason);
        }

        public static void Connected(int playerID)
        {
            OnConnection?.Invoke(true, playerID);
        }

        public static void Disconnected(int playerID)
        {
            OnConnection?.Invoke(false, playerID);
        }

        public static bool ChatMessageReceived(string from, string message)
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
        /// Connect to any server
        /// </summary>
        /// <param name="serverAddress">The server address to connect. Example: 127.0.0.1:4499</param>
        public static void Connect(string serverAddress)
        {
            Networking.ToggleConnection(serverAddress);
        }

        /// <summary>
        /// ?
        /// </summary>
        public static void Disconnect()
        {
            Networking.ToggleConnection(null);
        }

        /// <summary>
        /// Check if the player is already on a server
        /// </summary>
        public static bool IsOnServer()
        {
            return Networking.IsOnServer;
        }

        /// <summary>
        /// Get the local player's ID
        /// </summary>
        /// <returns>PlayerID</returns>
        public static int GetPlayerID()
        {
            return Main.LocalPlayerID;
        }

        /// <summary>
        /// Check if a RAGECOOP menu is visible
        /// </summary>
        public static bool IsMenuVisible()
        {
#if NON_INTERACTIVE
            return false;
#else
            return Menus.CoopMenu.MenuPool.AreAnyVisible;
#endif
        }

        /// <summary>
        /// Check if the RAGECOOP chat is visible
        /// </summary>
        public static bool IsChatFocused()
        {
            return Main.MainChat.Focused;
        }

        /// <summary>
        /// Check if the RAGECOOP list of players is visible
        /// </summary>
        public static bool IsPlayerListVisible()
        {
            return Util.GetTickCount64() - PlayerList.Pressed < 5000;
        }

        /// <summary>
        /// Get the version of RAGECOOP
        /// </summary>
        public static string GetCurrentVersion()
        {
            return Main.CurrentVersion;
        }

        /// <summary>
        /// Get that player's local username
        /// </summary>
        public static string GetUsername()
        {
            return Main.Settings.Username;
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

            Main.Settings.Username = username;

            return true;
        }

        /// <summary>
        /// Get or set the client's settings.
        /// </summary>
        /// <returns>The client's settings, you should NEVER change settings without notifying the player.</returns>
        public static Settings Settings()
        {
            return Main.Settings;
        }
    }
}
