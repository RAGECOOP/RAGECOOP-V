using Lidgren.Network;

namespace CoopServer
{
    public class ServerScript
    {
        public virtual void Start()
        {
            Logging.Info("Gamemode loaded successfully!");
        }

        public virtual void OnPlayerConnect(Entities.EntitiesPlayer client)
        {
            Logging.Info("New player [" + client.SocialClubName + " | " + client.Username + "] connected!");
        }

        public virtual void OnPlayerDisconnect(Entities.EntitiesPlayer client, string reason)
        {
            Logging.Info(client.Username + " left the server, reason: " + reason);
        }

        protected static void SendChatMessageToAll(string message, string username = "Server")
        {
            ChatMessagePacket packet = new()
            {
                Username = username,
                Message = message
            };

            Logging.Info(username + packet.Message);

            NetOutgoingMessage outgoingMessage = Server.MainNetServer.CreateMessage();
            packet.PacketToNetOutGoingMessage(outgoingMessage);
            Server.MainNetServer.SendMessage(outgoingMessage, Server.MainNetServer.Connections, NetDeliveryMethod.ReliableOrdered, 0);
        }
    }
}
