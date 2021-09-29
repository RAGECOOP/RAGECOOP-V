using System;
using System.ComponentModel;
using System.Timers;

using CoopServer;

namespace FirstGameMode
{
    [Serializable]
    public class SetPlayerTime
    {
        public int Hours {  get; set; }
        public int Minutes {  get; set; }
        public int Seconds {  get; set; }
    }

    public class Main : ServerScript
    {
        private static readonly Timer RunningSinceTimer = new() { Interval = 1000 };
        private static int RunningSince = 0;

        public Main()
        {
            RunningSinceTimer.Start();
            RunningSinceTimer.Elapsed += new ElapsedEventHandler((sender, e) => RunningSince += 1);

            API.OnPlayerConnected += OnPlayerConnected;
            API.OnPlayerDisconnected += OnPlayerDisconnected;
            API.OnChatMessage += OnChatMessage;
            API.OnModPacketReceived += OnModPacketReceived;

            API.RegisterCommand("running", RunningCommand);
            API.RegisterCommands<Commands>();
        }

        private void OnModPacketReceived(long from, string mod, byte customID, byte[] bytes, CancelEventArgs args)
        {
            if (mod != "FirstScript" || customID != 1)
            {
                return;
            }

            args.Cancel = true;

            // Get data from bytes
            SetPlayerTime setPlayerTime = bytes.CDeserialize<SetPlayerTime>();

            // Find the client by 'from' and send the time back as a nativecall
            API.GetAllClients().Find(x => x.ID == from).SendNativeCall(0x47C3B5848C3E45D8, setPlayerTime.Hours, setPlayerTime.Minutes, setPlayerTime.Seconds);
        }

        public static void RunningCommand(CommandContext ctx)
        {
            ctx.Client.SendChatMessage("Server has been running for: " + RunningSince + " seconds!");
        }

        public static void OnPlayerConnected(Client client)
        {
            API.SendChatMessageToAll("Player " + client.Player.Username + " connected!");
        }

        public static void OnPlayerDisconnected(Client client)
        {
            API.SendChatMessageToAll("Player " + client.Player.Username + " disconnected!");
        }

        public static void OnChatMessage(string username, string message, CancelEventArgs e)
        {
            e.Cancel = true;

            if (message.StartsWith("EASTEREGG"))
            {
                Client client;
                if ((client = API.GetClientByUsername(username)) != null)
                {
                    client.SendChatMessage("You found the EASTEREGG! *-*");
                }
                return;
            }

            API.SendChatMessageToAll(message, username);
        }
    }
}
