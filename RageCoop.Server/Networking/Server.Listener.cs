using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lidgren.Network;
using RageCoop.Core;
using RageCoop.Core.Scripting;
using RageCoop.Server.Scripting;

namespace RageCoop.Server
{
    public partial class Server
    {
        private void Listen()
        {
            NetIncomingMessage msg = null;
            while (!_stopping)
            {
                try
                {
                    msg = MainNetServer.WaitMessage(200);
                    ProcessMessage(msg);
                }
                catch (Exception ex)
                {
                    Logger?.Error("Error processing message");
                    Logger?.Error(ex);
                    if (msg != null)
                    {
                        DisconnectAndLog(msg.SenderConnection, PacketType.Unknown, ex);
                    }
                }
            }
            Logger?.Info("Server is shutting down!");
            MainNetServer.Shutdown("Server is shutting down!");
            BaseScript.OnStop();
            Resources.UnloadAll();
        }

        private void ProcessMessage(NetIncomingMessage message)
        {
            Client sender;
            if (message == null) { return; }
            switch (message.MessageType)
            {
                case NetIncomingMessageType.ConnectionApproval:
                    {
                        Logger?.Info($"New incoming connection from: [{message.SenderConnection.RemoteEndPoint}]");
                        if (message.ReadByte() != (byte)PacketType.Handshake)
                        {
                            Logger?.Info($"IP [{message.SenderConnection.RemoteEndPoint.Address}] was blocked, reason: Wrong packet!");
                            message.SenderConnection.Deny("Wrong packet!");
                        }
                        else
                        {
                            try
                            {
                                int len = message.ReadInt32();
                                byte[] data = message.ReadBytes(len);
                                GetHandshake(message.SenderConnection, data.GetPacket<Packets.Handshake>());
                            }
                            catch (Exception e)
                            {
                                Logger?.Info($"IP [{message.SenderConnection.RemoteEndPoint.Address}] was blocked, reason: {e.Message}");
                                Logger?.Error(e);
                                message.SenderConnection.Deny(e.Message);
                            }
                        }
                        break;
                    }
                case NetIncomingMessageType.StatusChanged:
                    {
                        // Get sender client
                        if (!ClientsByNetHandle.TryGetValue(message.SenderConnection.RemoteUniqueIdentifier, out sender))
                        {
                            break;
                        }
                        NetConnectionStatus status = (NetConnectionStatus)message.ReadByte();

                        if (status == NetConnectionStatus.Disconnected)
                        {

                            PlayerDisconnected(sender);
                        }
                        else if (status == NetConnectionStatus.Connected)
                        {
                            PlayerConnected(sender);
                            _worker.QueueJob(() => API.Events.InvokePlayerConnected(sender));
                            Resources.SendTo(sender);
                        }
                        break;
                    }
                case NetIncomingMessageType.Data:
                    {

                        // Get sender client
                        if (ClientsByNetHandle.TryGetValue(message.SenderConnection.RemoteUniqueIdentifier, out sender))
                        {
                            // Get packet type
                            var type = (PacketType)message.ReadByte();
                            switch (type)
                            {
                                case PacketType.Response:
                                    {
                                        int id = message.ReadInt32();
                                        if (PendingResponses.TryGetValue(id, out var callback))
                                        {
                                            callback((PacketType)message.ReadByte(), message.ReadBytes(message.ReadInt32()));
                                            PendingResponses.Remove(id);
                                        }
                                        break;
                                    }
                                case PacketType.Request:
                                    {
                                        int id = message.ReadInt32();
                                        if (RequestHandlers.TryGetValue((PacketType)message.ReadByte(), out var handler))
                                        {
                                            var response = MainNetServer.CreateMessage();
                                            response.Write((byte)PacketType.Response);
                                            response.Write(id);
                                            handler(message.ReadBytes(message.ReadInt32()), sender).Pack(response);
                                            MainNetServer.SendMessage(response, message.SenderConnection, NetDeliveryMethod.ReliableOrdered);
                                        }
                                        break;
                                    }
                                default:
                                    {
                                        byte[] data = message.ReadBytes(message.ReadInt32());
                                        if (type.IsSyncEvent())
                                        {
                                            // Sync Events

                                            if (Settings.UseP2P) { break; }
                                            try
                                            {
                                                var toSend = MainNetServer.Connections.Exclude(message.SenderConnection);
                                                if (toSend.Count != 0)
                                                {
                                                    var outgoingMessage = MainNetServer.CreateMessage();
                                                    outgoingMessage.Write((byte)type);
                                                    outgoingMessage.Write(data.Length);
                                                    outgoingMessage.Write(data);
                                                    MainNetServer.SendMessage(outgoingMessage, toSend, NetDeliveryMethod.UnreliableSequenced, (byte)ConnectionChannel.SyncEvents);
                                                }
                                            }
                                            catch (Exception e)
                                            {
                                                DisconnectAndLog(message.SenderConnection, type, e);
                                            }
                                        }
                                        else
                                        {
                                            HandlePacket(type, data, sender);
                                        }
                                        break;
                                    }
                            }

                        }
                        break;
                    }
                case NetIncomingMessageType.ErrorMessage:
                    Logger?.Error(message.ReadString());
                    break;
                case NetIncomingMessageType.WarningMessage:
                    Logger?.Warning(message.ReadString());
                    break;
                case NetIncomingMessageType.DebugMessage:
                case NetIncomingMessageType.VerboseDebugMessage:
                    Logger?.Debug(message.ReadString());
                    break;
                case NetIncomingMessageType.UnconnectedData:
                    {
                        if (message.ReadByte() == (byte)PacketType.PublicKeyRequest)
                        {
                            var msg = MainNetServer.CreateMessage();
                            var p = new Packets.PublicKeyResponse();
                            Security.GetPublicKey(out p.Modulus, out p.Exponent);
                            p.Pack(msg);
                            Logger?.Debug($"Sending public key to {message.SenderEndPoint}, length:{msg.LengthBytes}");
                            MainNetServer.SendUnconnectedMessage(msg, message.SenderEndPoint);
                        }
                    }
                    break;
                default:
                    Logger?.Error(string.Format("Unhandled type: {0} {1} bytes {2} | {3}", message.MessageType, message.LengthBytes, message.DeliveryMethod, message.SequenceChannel));
                    break;
            }

            MainNetServer.Recycle(message);
        }
    }
}
