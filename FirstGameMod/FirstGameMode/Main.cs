using System.Collections.Generic;
using System.ComponentModel;
using System.Timers;

using CoopServer;

namespace FirstGameMode
{
    public class Main : ServerScript
    {
        private static readonly Timer RunningSinceTimer = new() { Interval = 1000 };
        private static int RunningSince = 0;
        private static readonly List<string> SecretLocation = new();

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

        public static void OnPlayerPositionUpdate(Client client)
        {
            if (client.HasData("ShowPlayerPosition") && client.GetData<bool>("ShowPlayerPosition"))
            {
                if (!SecretLocation.Contains(client.Player.Username) && client.Player.IsInRangeOf(new LVector3(0, 0, 75), 7f))
                {
                    client.SendChatMessage("Hey! you find this secret location!");
                    SecretLocation.Add(client.Player.Username);
                    return;
                }
            }
        }
    }
}
