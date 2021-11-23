using System.Collections.Generic;

using Lidgren.Network;

namespace CoopServer
{
    public class Client
    {
        public long ID = 0;
        public float Latency = 0.0f;
        public PlayerData Player;
        private readonly Dictionary<string, object> CustomData = new();

        #region CUSTOMDATA FUNCTIONS
        public void SetData<T>(string name, T data)
        {
            if (HasData(name))
            {
                CustomData[name] = data;
            }
            else
            {
                CustomData.Add(name, data);
            }
        }

        public bool HasData(string name)
        {
            return CustomData.ContainsKey(name);
        }

        public T GetData<T>(string name)
        {
            return HasData(name) ? (T)CustomData[name] : default;
        }

        public void RemoveData(string name)
        {
            if (HasData(name))
            {
                CustomData.Remove(name);
            }
        }
        #endregion

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

        public void SendModPacket(string mod, byte customID, byte[] bytes)
        {
            NetConnection userConnection = Server.MainNetServer.Connections.Find(x => x.RemoteUniqueIdentifier == ID);

            NetOutgoingMessage outgoingMessage = Server.MainNetServer.CreateMessage();
            new ModPacket()
            {
                ID = -1,
                Target = 0,
                Mod = mod,
                CustomPacketID = customID,
                Bytes = bytes
            }.PacketToNetOutGoingMessage(outgoingMessage);
            Server.MainNetServer.SendMessage(outgoingMessage, userConnection, NetDeliveryMethod.ReliableOrdered, 0);
            Server.MainNetServer.FlushSendQueue();
        }
    }
}
