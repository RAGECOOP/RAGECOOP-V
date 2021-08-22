using System.Collections.Generic;
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
        public static bool ShowPlayerPosition = false;
        private static List<string> SecretLocation = new List<string>();

        public Main()
        {
            RunningSinceTimer.Start();
            RunningSinceTimer.Elapsed += new ElapsedEventHandler((sender, e) => RunningSince += 1);

            API.OnPlayerConnected += OnPlayerConnected;
            API.OnPlayerDisconnected += OnPlayerDisconnected;
            API.OnChatMessage += OnChatMessage;
            API.OnPlayerPositionUpdate += OnPlayerPositionUpdate;

            API.RegisterCommand("running", RunningCommand);
            API.RegisterCommands<Commands>();
        }

        public static void RunningCommand(CommandContext ctx)
        {
            API.SendChatMessageToPlayer(ctx.Player.Username, "Server has been running for: " + RunningSince + " seconds!");
        }

        public static void OnPlayerConnected(EntitiesPlayer player)
        {
            API.SendChatMessageToAll("Player " + player.Username + " connected!");
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

        public static void OnPlayerPositionUpdate(EntitiesPlayer player)
        {
            if (ShowPlayerPosition)
            {
                if (!SecretLocation.Contains(player.Username) && player.IsInRangeOf(new LVector3(0, 0, 75), 7f))
                {
                    API.SendChatMessageToPlayer(player.Username, "Hey! you find this secret location!");
                    SecretLocation.Add(player.Username);
                    return;
                }
            }
        }
    }
}
