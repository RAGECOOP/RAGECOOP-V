using System.ComponentModel;
using System.Timers;

using CoopServer;
using CoopServer.Entities;

namespace FirstGameMode
{
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

            API.RegisterCommand("running", RunningCommand);
            API.RegisterCommands<Commands>();
        }

        public static void RunningCommand(CommandContext ctx)
        {
            API.SendChatMessageToPlayer(ctx.Player.Username, "Server has been running for: " + RunningSince + " seconds!");
        }

        public static void OnPlayerConnected(EntitiesPlayer client)
        {
            API.SendChatMessageToAll("Player " + client.Username + " connected!");
        }

        public static void OnPlayerDisconnected(EntitiesPlayer player)
        {
            API.SendChatMessageToAll("Player " + player.Username + " disconnected!");
        }

        public static void OnChatMessage(string username, string message, CancelEventArgs e)
        {
            e.Cancel = true;

            if (message.StartsWith("EASTEREGG"))
            {
                API.SendChatMessageToPlayer(username, "You found the EASTEREGG! *-*");
                return;
            }

            API.SendChatMessageToAll(message, username);
        }
    }
}
