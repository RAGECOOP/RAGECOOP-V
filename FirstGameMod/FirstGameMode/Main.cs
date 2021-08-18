using CoopServer;
using CoopServer.Entities;
using System.Timers;

namespace FirstGameMode
{
    public class Main : ServerScript
    {
        private static readonly Timer RunningSinceTimer = new() { Interval = 1000 };
        private static int RunningSince = 0;

        public override void Start()
        {
            RunningSinceTimer.Start();
            RunningSinceTimer.Elapsed += new ElapsedEventHandler((sender, e) => RunningSince += 1);

            RegisterCommand("running", RunningCommand);
            RegisterCommands<Commands>();
        }

        public static void RunningCommand(CommandContext ctx)
        {
            SendChatMessageToPlayer(ctx.Player.Username, "Server has been running for: " + RunningSince + " seconds!");
        }

        public override void OnPlayerConnect(EntitiesPlayer client)
        {
            SendChatMessageToAll("Player " + client.Username + " connected!");
        }

        public override void OnPlayerDisconnect(EntitiesPlayer player, string reason)
        {
            SendChatMessageToAll(player.Username + " left the server, reason: " + reason);
        }

        public override bool OnChatMessage(string username, string message)
        {
            if (message.StartsWith("EASTEREGG"))
            {
                SendChatMessageToPlayer(username, "You found the EASTEREGG! *-*");
                return true;
            }

            return false;
        }
    }
}
