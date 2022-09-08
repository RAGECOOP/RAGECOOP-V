using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Threading;

namespace RageCoop.Core
{
    internal class CoopPeer : NetPeer, IDisposable
    {
        public EventHandler<NetIncomingMessage> OnMessageReceived;
        private readonly Thread ListenerThread;
        private bool _stopping = false;
        public CoopPeer(NetPeerConfiguration config) : base(config)
        {
            Start();
            NetIncomingMessage msg;
            ListenerThread = new Thread(() =>
              {
                  while (!_stopping)
                  {
                      msg = WaitMessage(200);
                      if (msg != null)
                      {
                          OnMessageReceived?.Invoke(this, msg);
                      }
                  }
              });
            ListenerThread.Start();
        }

        /// <summary>
        /// Terminate all connections and background thread
        /// </summary>
        public void Dispose()
        {
            _stopping = true;
            Shutdown("Bye!");
            ListenerThread.Join();
        }
        public void SendTo(Packet p, NetConnection connection, ConnectionChannel channel = ConnectionChannel.Default, NetDeliveryMethod method = NetDeliveryMethod.UnreliableSequenced)
        {
            NetOutgoingMessage outgoingMessage = CreateMessage();
            p.Pack(outgoingMessage);
            SendMessage(outgoingMessage, connection, method, (int)channel);
        }
        public void SendTo(Packet p, IList<NetConnection> connections, ConnectionChannel channel = ConnectionChannel.Default, NetDeliveryMethod method = NetDeliveryMethod.UnreliableSequenced)
        {

            NetOutgoingMessage outgoingMessage = CreateMessage();
            p.Pack(outgoingMessage);
            SendMessage(outgoingMessage, connections, method, (int)channel);
        }
        public void Send(Packet p, IList<NetConnection> cons, ConnectionChannel channel = ConnectionChannel.Default, NetDeliveryMethod method = NetDeliveryMethod.UnreliableSequenced)
        {
            NetOutgoingMessage outgoingMessage = CreateMessage();
            p.Pack(outgoingMessage);
            SendMessage(outgoingMessage, cons, method, (int)channel);
        }
    }
}
