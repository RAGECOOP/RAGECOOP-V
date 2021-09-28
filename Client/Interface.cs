using System;
using System.ComponentModel;

using Lidgren.Network;

namespace CoopClient
{
    public static class Interface
    {
        #region DELEGATES
        public delegate void ConnectEvent(bool connected, string reason = null);
        public delegate void MessageEvent(NetIncomingMessage message);
        public delegate void ChatMessage(string from, string message, CancelEventArgs args);
        #endregion

        #region EVENTS
        public static event ConnectEvent OnConnection;
        public static event MessageEvent OnMessage;
        public static event ChatMessage OnChatMessage;

        internal static void Connected()
        {
            OnConnection?.Invoke(true);
        }

        internal static void Disconnected(string reason)
        {
            OnConnection?.Invoke(false, reason);
        }

        internal static void MessageReceived(NetIncomingMessage message)
        {
            OnMessage?.Invoke(message);
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

        public static bool IsOnServer()
        {
            return Main.MainNetworking.IsOnServer();
        }

        public static bool IsMenuVisible()
        {
            return Main.MainMenu.MenuPool.AreAnyVisible;
        }

        public static bool IsChatFocused()
        {
            return Main.MainChat.Focused;
        }

        public static bool IsPlayerListVisible()
        {
            return Environment.TickCount - PlayerList.Pressed < 5000;
        }

        public static string GetCurrentVersion()
        {
            return Main.CurrentVersion;
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
