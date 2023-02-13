using System;
using System.Collections.Generic;
using System.Threading;
using Lidgren.Network;

namespace RageCoop.Core
{
    internal class CoopPeer : NetPeer, IDisposable
    {
        private readonly Logger Log;
        private readonly Thread _receiver;
        private bool _stopping;
        public EventHandler<NetIncomingMessage> OnMessageReceived;

        public CoopPeer(NetPeerConfiguration config,Logger logger) : base(config)
        {
            Log = logger;
            Start();
            NetIncomingMessage msg;
            _receiver = new Thread(() =>
            {
                while (!_stopping)
                {
                    msg = WaitMessage(200);
                    if (msg != null) OnMessageReceived?.Invoke(this, msg);
                }
            });
            _receiver.Start();
        }

        /// <summary>
        ///     Terminate all connections and background thread
        /// </summary>
        public void Dispose()
        {
            _stopping = true;
            if (Status == NetPeerStatus.Running)
            {
                Shutdown("Bye!");
            }
            if (_receiver.IsAlive)
            {
                Log?.Debug("Stopping message thread");
                _receiver.Join();
            }
            Log?.Debug("Stopping network thread");
            Join();
            Log?.Debug("CoopPeer disposed");
        }

        public void SendTo(Packet p, NetConnection connection, ConnectionChannel channel = ConnectionChannel.Default,
            NetDeliveryMethod method = NetDeliveryMethod.UnreliableSequenced)
        {
            var outgoingMessage = CreateMessage();
            p.Pack(outgoingMessage);
            SendMessage(outgoingMessage, connection, method, (int)channel);
        }

        public void SendTo(Packet p, IList<NetConnection> connections,
            ConnectionChannel channel = ConnectionChannel.Default,
            NetDeliveryMethod method = NetDeliveryMethod.UnreliableSequenced)
        {
            var outgoingMessage = CreateMessage();
            p.Pack(outgoingMessage);
            SendMessage(outgoingMessage, connections, method, (int)channel);
        }

        public void Send(Packet p, IList<NetConnection> cons, ConnectionChannel channel = ConnectionChannel.Default,
            NetDeliveryMethod method = NetDeliveryMethod.UnreliableSequenced)
        {
            var outgoingMessage = CreateMessage();
            p.Pack(outgoingMessage);
            SendMessage(outgoingMessage, cons, method, (int)channel);
        }
    }
}