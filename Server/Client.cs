using System.Collections.Generic;

using Lidgren.Network;

namespace CoopServer
{
    public class Client
    {
        public long ID = 0;
        public float Latency = 0.0f;
        public PlayerData Player;

        public void Kick(string[] reason)
        {
            Server.MainNetServer.Connections.Find(x => x.RemoteUniqueIdentifier == ID).Disconnect(string.Join(" ", reason));
        }

        public void SendChatMessage(string message, string from = "Server")
        {
            NetConnection userConnection = Server.MainNetServer.Connections.Find(x => x.RemoteUniqueIdentifier == ID);

            ChatMessagePacket packet = new()
            {
                Username = from,
                Message = message
            };

            NetOutgoingMessage outgoingMessage = Server.MainNetServer.CreateMessage();
            packet.PacketToNetOutGoingMessage(outgoingMessage);
            Server.MainNetServer.SendMessage(outgoingMessage, userConnection, NetDeliveryMethod.ReliableOrdered, 0);
        }

        public void SendNativeCall(ulong hash, params object[] args)
        {
            NetConnection userConnection = Server.MainNetServer.Connections.Find(x => x.RemoteUniqueIdentifier == ID);

            List<NativeArgument> arguments = Util.ParseNativeArguments(args);
            if (arguments == null)
            {
                return;
            }

            NativeCallPacket packet = new()
            {
                Hash = hash,
                Args = arguments
            };

            NetOutgoingMessage outgoingMessage = Server.MainNetServer.CreateMessage();
            packet.PacketToNetOutGoingMessage(outgoingMessage);
            Server.MainNetServer.SendMessage(outgoingMessage, userConnection, NetDeliveryMethod.ReliableOrdered, 0);
        }
    }
}
