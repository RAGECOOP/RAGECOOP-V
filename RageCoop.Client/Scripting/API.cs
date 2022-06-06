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
    public static class API
    {
        public static class Config
        {
            /// <summary>
            /// Enable automatic respawn.
            /// </summary>
            public static bool EnableAutoRespawn { get; set; } = true;
            /// <summary>
            /// Don't show other player's name tag
            /// </summary>
            public static bool DisplayNameTag { get; set; }=true;
            /// <summary>
            /// Show other players' blip on map
            /// </summary>
            public static bool DisplayBlip { get; set; } = true;
        }

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
        #endregion

        /// <summary>
        /// ?
        /// </summary>
        public static event ChatMessage OnChatMessage;
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
        /// Disconnect from the server
        /// </summary>
        public static void Disconnect()
        {
            Networking.ToggleConnection(null);
        }

        /// <summary>
        /// Check if the player is already on a server
        /// </summary>
        public static bool IsOnServer
        {
            get { return Networking.IsOnServer; }
        }

        /// <summary>
        /// Get the local player's ID
        /// </summary>
        /// <returns>PlayerID</returns>
        public static int LocalPlayerID
        {
            get { return Main.LocalPlayerID; }
        }

        /// <summary>
        /// Check if a RAGECOOP menu is visible
        /// </summary>
        public static bool IsMenuVisible
        {
            get { return Menus.CoopMenu.MenuPool.AreAnyVisible; }
        }

        /// <summary>
        /// Check if the RAGECOOP chat is visible
        /// </summary>
        public static bool IsChatFocused
        {
            get { return Main.MainChat.Focused; }
        }

        /// <summary>
        /// Check if the RAGECOOP list of players is visible
        /// </summary>
        public static bool IsPlayerListVisible
        {
            get { return Util.GetTickCount64() - PlayerList.Pressed < 5000; }
        }

        /// <summary>
        /// Get the version of RAGECOOP
        /// </summary>
        public static string CurrentVersion
        {
            get { return Main.CurrentVersion; }
        }

        /// <summary>
        /// Get or set local player's username, set won't be effective if already connected to a server.
        /// </summary>
        public static string Username
        {
            get { return Main.Settings.Username; }
            set
            {
                if (IsOnServer || string.IsNullOrEmpty(value))
                {
                    return;
                }
                Main.Settings.Username = value;
            }
        }
    }
}
