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

        public static void Connected()
        {
            OnConnection?.Invoke(true, GetPlayerID());
        }

        public static void Disconnected(string reason)
        {
            OnConnection?.Invoke(false, GetPlayerID(), reason);
        }

        public static void Connected(long netHandle)
        {
            OnConnection?.Invoke(true, netHandle);
        }

        public static void Disconnected(long netHandle)
        {
            OnConnection?.Invoke(false, netHandle);
        }

        public static void ModPacketReceived(long from, string mod, byte customID, byte[] bytes)
        {
            OnModPacketReceived?.Invoke(from, mod, customID, bytes);
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
            Networking.DisConnectFromServer(serverAddress);
        }

        /// <summary>
        /// ?
        /// </summary>
        public static void Disconnect()
        {
            Networking.DisConnectFromServer(null);
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
        public static long GetPlayerID()
        {
            return Main.LocalPlayerID;
        }

        /*

        /// <summary>
        /// Get a player using their Lidgren Network net handle
        /// </summary>
        /// <param name="handle">Lidgren-Network net handle</param>
        public static CharacterEntity GetPed(int ID)
        {
            lock (Main.Characters)
            {
                return Main.Characters.ContainsKey(ID) ? Main.Characters[ID] : null;
            }
        }
        */
        /// <summary>
        /// Check if a RAGECOOP menu is visible
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
        /// Send any data (bytes) to the server
        /// </summary>
        /// <param name="modName">The name of this modification (script)</param>
        /// <param name="customID">The ID to know what the data is</param>
        /// <param name="bytes">Your class, structure or whatever in bytes</param>
        public static void SendDataToServer(string modName, byte customID, byte[] bytes)
        {
            Networking.SendModData(-1, modName, customID, bytes);
        }

        /// <summary>
        /// Send any data (bytes) to the all player
        /// </summary>
        /// <param name="modName">The name of this modification (script)</param>
        /// <param name="customID">The ID to know what the data is</param>
        /// <param name="bytes">Your class, structure or whatever in bytes</param>
        public static void SendDataToAll(string modName, byte customID, byte[] bytes)
        {
            Networking.SendModData(0, modName, customID, bytes);
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
            Networking.SendModData(netHandle, modName, customID, bytes);
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
        /// Enable or disable the local traffic for this player
        /// </summary>
        /// <param name="enable">true to disable traffic</param>
        public static void SetLocalTraffic(bool enable)
        {
            Main.Settings.DisableTraffic = !enable;
        }

        /// <summary>
        /// Sets the alignment for the player list, if set to true it will align left, 
        /// otherwise it will align right
        /// </summary>
        /// <param name="leftAlign">true to move the player list to the left</param>
        public static void SetPlayerListLeftAlign(bool leftAlign)
        {
            PlayerList.LeftAlign = leftAlign;
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
