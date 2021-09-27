using System.ComponentModel;

using Lidgren.Network;

namespace CoopClient
{
    public static class Interface
    {
        #region DELEGATES
        public delegate void ConnectEvent(bool connected, string bye_message = null);
        public delegate void MessageEvent(NetIncomingMessage message);
        public delegate void ChatMessage(string from, string message, CancelEventArgs args);
        #endregion

        #region EVENTS
        public static event ConnectEvent OnConnected;
        public static event ConnectEvent OnDisconnected;
        public static event MessageEvent OnMessage;
        public static event ChatMessage OnChatMessage;

        public static void Connected()
        {
            OnConnected?.Invoke(true);
        }

        public static void Disconnected(string bye_message)
        {
            OnDisconnected?.Invoke(false, bye_message);
        }

        public static void MessageReceived(NetIncomingMessage message)
        {
            OnMessage?.Invoke(message);
        }

        public static bool ChatMessageReceived(string from, string message)
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
