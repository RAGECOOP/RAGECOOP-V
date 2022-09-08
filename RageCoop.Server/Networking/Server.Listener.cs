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
                                GetHandshake(message.SenderConnection, message.GetPacket<Packets.Handshake>());
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
                            QueueJob(() => API.Events.InvokePlayerConnected(sender));
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
                                            callback((PacketType)message.ReadByte(), message);
                                            PendingResponses.Remove(id);
                                        }
                                        break;
                                    }
                                case PacketType.Request:
                                    {
                                        int id = message.ReadInt32();
                                        var reqType = (PacketType)message.ReadByte();
                                        if (RequestHandlers.TryGetValue(reqType, out var handler))
                                        {
                                            var response = MainNetServer.CreateMessage();
                                            response.Write((byte)PacketType.Response);
                                            response.Write(id);
                                            handler(message, sender).Pack(response);
                                            MainNetServer.SendMessage(response, message.SenderConnection, NetDeliveryMethod.ReliableOrdered);
                                        }
                                        else
                                        {
                                            Logger.Warning("Did not find a request handler of type: " + reqType);
                                        }
                                        break;
                                    }
                                default:
                                    {
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
                                                    outgoingMessage.Write(message.ReadBytes(message.LengthBytes-1));
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
                                            HandlePacket(type, message, sender);
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

        private void HandlePacket(PacketType type, NetIncomingMessage msg, Client sender)
        {
            try
            {
                switch (type)
                {
                    case PacketType.PedSync:
                        PedSync(msg.GetPacket<Packets.PedSync>(), sender);
                        break;

                    case PacketType.VehicleSync:
                        VehicleSync(msg.GetPacket<Packets.VehicleSync>(), sender);
                        break;

                    case PacketType.ProjectileSync:
                        ProjectileSync(msg.GetPacket<Packets.ProjectileSync>(), sender);
                        break;

                    case PacketType.ChatMessage:
                        {
                            Packets.ChatMessage packet = new((b) =>
                            {
                                return Security.Decrypt(b, sender.EndPoint);
                            });
                            packet.Deserialize(msg);
                            ChatMessageReceived(packet.Username, packet.Message, sender);
                        }
                        break;

                    case PacketType.Voice:
                        {
                            if (Settings.UseVoice && !Settings.UseP2P)
                            {
                                Forward(msg.GetPacket<Packets.Voice>(), sender, ConnectionChannel.Voice);
                            }
                        }
                        break;

                    case PacketType.CustomEvent:
                        {
                            Packets.CustomEvent packet = new Packets.CustomEvent();
                            packet.Deserialize(msg);
                            QueueJob(() => API.Events.InvokeCustomEventReceived(packet, sender));
                        }
                        break;
                    default:
                        Logger?.Error("Unhandled Data / Packet type");
                        break;

                }
            }
            catch (Exception e)
            {
                DisconnectAndLog(sender.Connection, type, e);
            }
        }
    }
}
