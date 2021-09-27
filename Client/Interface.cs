using Lidgren.Network;

namespace CoopClient
{
    public static class Interface
    {
        public delegate void ConnectEvent(bool connected, string bye_message);
        public static event ConnectEvent OnConnect;
        public static event ConnectEvent OnDisconnect;
        public delegate void MessageEvent(NetIncomingMessage message);
        public static event MessageEvent OnMessage;

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

        public static void Disconnected( string bye_message)
        {
            OnDisconnect?.Invoke(false, bye_message);
        }

        public static void Connected()
        {
            OnConnect?.Invoke(true, "");
        }

        public static void MessageReceived(NetIncomingMessage message)
        {
            OnMessage?.Invoke(message);
        }

    }
}
