using System;
using System.Threading;
using GTA;
using GTA.UI;
using Lidgren.Network;
using RageCoop.Client.Menus;
using RageCoop.Client.Scripting;
using RageCoop.Core;
using RageCoop.Core.Scripting;

namespace RageCoop.Client
{
    internal static partial class Networking
    {


        private static readonly AutoResetEvent _publicKeyReceived = new AutoResetEvent(false);

        public static void ProcessMessage(NetIncomingMessage message)
        {
            if (message == null) return;
            var _recycle = true;
            switch (message.MessageType)
            {
                case NetIncomingMessageType.StatusChanged:
                    var status = (NetConnectionStatus)message.ReadByte();
                    var reason = message.ReadString();
                    switch (status)
                    {
                        case NetConnectionStatus.InitiatedConnect:
                            if (message.SenderConnection == ServerConnection) CoopMenu.InitiateConnectionMenuSetting();
                            break;
                        case NetConnectionStatus.Connected:
                            if (message.SenderConnection == ServerConnection)
                            {
                                var response = message.SenderConnection.RemoteHailMessage;
                                if ((PacketType)response.ReadByte() != PacketType.HandshakeSuccess)
                                    throw new Exception("Invalid handshake response!");
                                var p = new Packets.HandshakeSuccess();
                                p.Deserialize(response);
                                foreach (var player in p.Players) PlayerList.SetPlayer(player.ID, player.Username);
                                Connected();
                            }
                            else
                            {
                                // Self-initiated connection
                                if (message.SenderConnection.RemoteHailMessage == null) return;

                                var p = message.SenderConnection.RemoteHailMessage.GetPacket<Packets.P2PConnect>();
                                if (PlayerList.Players.TryGetValue(p.ID, out var player))
                                {
                                    player.Connection = message.SenderConnection;
                                    Log.Debug($"Direct connection to {player.Username} established");
                                }
                                else
                                {
                                    Log.Info(
                                        $"Unidentified peer connection from {message.SenderEndPoint} was rejected.");
                                    message.SenderConnection.Disconnect("eat poop");
                                }
                            }

                            break;
                        case NetConnectionStatus.Disconnected:
                            if (message.SenderConnection == ServerConnection) API.QueueAction(() => CleanUp(reason));
                            break;
                    }

                    break;
                case NetIncomingMessageType.Data:
                    {
                        if (message.LengthBytes == 0) break;
                        var packetType = PacketType.Unknown;
                        try
                        {
                            // Get packet type
                            packetType = (PacketType)message.ReadByte();
                            switch (packetType)
                            {
                                case PacketType.Response:
                                    {
                                        var id = message.ReadInt32();
                                        if (PendingResponses.TryGetValue(id, out var callback))
                                        {
                                            callback((PacketType)message.ReadByte(), message);
                                            PendingResponses.Remove(id);
                                        }

                                        break;
                                    }
                                case PacketType.Request:
                                    {
                                        var id = message.ReadInt32();
                                        var realType = (PacketType)message.ReadByte();
                                        if (RequestHandlers.TryGetValue(realType, out var handler))
                                        {
                                            var response = Peer.CreateMessage();
                                            response.Write((byte)PacketType.Response);
                                            response.Write(id);
                                            handler(message).Pack(response);
                                            Peer.SendMessage(response, ServerConnection, NetDeliveryMethod.ReliableOrdered,
                                                message.SequenceChannel);
                                            Peer.FlushSendQueue();
                                        }
                                        else
                                        {
                                            Log.Debug("Did not find a request handler of type: " + realType);
                                        }

                                        break;
                                    }
                                default:
                                    {
                                        HandlePacket(packetType, message, message.SenderConnection, ref _recycle);
                                        break;
                                    }
                            }
                        }
                        catch (Exception ex)
                        {
                            API.QueueAction(() =>
                            {
                                Notification.Show($"~r~~h~Packet Error {ex.Message}");
                                return true;
                            });
                            Log.Error($"[{packetType}] {ex.Message}");
                            Log.Error(ex);
                            Peer.Shutdown($"Packet Error [{packetType}]");
                        }

                        break;
                    }
                case NetIncomingMessageType.UnconnectedData:
                    {
                        var packetType = (PacketType)message.ReadByte();
                        switch (packetType)
                        {
                            case PacketType.HolePunch:
                                {
                                    HolePunch.Punched(message.GetPacket<Packets.HolePunch>(), message.SenderEndPoint);
                                    break;
                                }
                            case PacketType.PublicKeyResponse:
                                {
                                    if (message.SenderEndPoint.ToString() != _targetServerEP.ToString() || !IsConnecting) break;
                                    var packet = message.GetPacket<Packets.PublicKeyResponse>();
                                    Security.SetServerPublicKey(packet.Modulus, packet.Exponent);
                                    _publicKeyReceived.Set();
                                    break;
                                }
                        }

                        break;
                    }
                case NetIncomingMessageType.DebugMessage:
                case NetIncomingMessageType.ErrorMessage:
                case NetIncomingMessageType.WarningMessage:
                case NetIncomingMessageType.VerboseDebugMessage:
                    Log.Trace(message.ReadString());
                    break;
            }

            if (_recycle) Peer.Recycle(message);
        }

        private static void HandlePacket(PacketType packetType, NetIncomingMessage msg, NetConnection senderConnection,
            ref bool recycle)
        {
            switch (packetType)
            {
                case PacketType.HolePunchInit:
                    HolePunch.Add(msg.GetPacket<Packets.HolePunchInit>());
                    break;

                case PacketType.PlayerConnect:
                    PlayerConnect(msg.GetPacket<Packets.PlayerConnect>());
                    break;

                case PacketType.PlayerDisconnect:
                    PlayerDisconnect(msg.GetPacket<Packets.PlayerDisconnect>());
                    break;

                case PacketType.PlayerInfoUpdate:
                    PlayerList.UpdatePlayer(msg.GetPacket<Packets.PlayerInfoUpdate>());
                    break;

                case PacketType.VehicleSync:
                    ReceivedPackets.VehicelPacket.Deserialize(msg);
                    VehicleSync(ReceivedPackets.VehicelPacket);
                    break;

                case PacketType.PedSync:
                    ReceivedPackets.PedPacket.Deserialize(msg);
                    PedSync(ReceivedPackets.PedPacket);
                    break;
                case PacketType.ProjectileSync:
                    ReceivedPackets.ProjectilePacket.Deserialize(msg);
                    ProjectileSync(ReceivedPackets.ProjectilePacket);
                    break;

                case PacketType.ChatMessage:
                    {
                        var packet = new Packets.ChatMessage(b => Security.Decrypt(b));
                        packet.Deserialize(msg);

                        API.QueueAction(() =>
                        {
                            MainChat.AddMessage(packet.Username, packet.Message);
                            return true;
                        });
                    }
                    break;

                case PacketType.Voice:
                    {
                        if (Settings.Voice)
                        {
                            var packet = new Packets.Voice();
                            packet.Deserialize(msg);


                            var player = EntityPool.GetPedByID(packet.ID);
                            player.IsSpeaking = true;
                            player.LastSpeakingTime = Ticked;

                            Voice.AddVoiceData(packet.Buffer, packet.Recorded);
                        }
                    }
                    break;

                case PacketType.CustomEvent:
                    {
                        var packet = new Packets.CustomEvent();
                        if (((CustomEventFlags)msg.PeekByte()).HasEventFlag(CustomEventFlags.Queued))
                        {
                            recycle = false;
                            API.QueueAction(() =>
                            {
                                packet.Deserialize(msg);
                                API.Events.InvokeCustomEventReceived(packet);
                                Peer.Recycle(msg);
                            });
                        }
                        else
                        {
                            packet.Deserialize(msg);
                            API.Events.InvokeCustomEventReceived(packet);
                        }
                    }
                    break;

                case PacketType.FileTransferChunk:
                    {
                        var packet = new Packets.FileTransferChunk();
                        packet.Deserialize(msg);
                        DownloadManager.Write(packet.ID, packet.FileChunk);
                    }
                    break;

                default:
                    if (packetType.IsSyncEvent())
                    {
                        recycle = false;
                        // Dispatch to script thread
                        API.QueueAction(() =>
                        {
                            SyncEvents.HandleEvent(packetType, msg);
                            return true;
                        });
                    }

                    break;
            }
        }

        private static void PedSync(Packets.PedSync packet)
        {
            var c = EntityPool.GetPedByID(packet.ID);
            if (c == null)
                // Log.Debug($"Creating character for incoming sync:{packet.ID}");
                EntityPool.ThreadSafe.Add(c = new SyncedPed(packet.ID));
            var flags = packet.Flags;
            c.ID = packet.ID;
            c.OwnerID = packet.OwnerID;
            c.Health = packet.Health;
            c.Rotation = packet.Rotation;
            c.Velocity = packet.Velocity;
            c.Speed = packet.Speed;
            c.Flags = packet.Flags;
            c.Heading = packet.Heading;
            c.Position = packet.Position;
            if (c.IsRagdoll)
            {
                c.HeadPosition = packet.HeadPosition;
                c.RightFootPosition = packet.RightFootPosition;
                c.LeftFootPosition = packet.LeftFootPosition;
            }
            else if (c.Speed >= 4)
            {
                c.VehicleID = packet.VehicleID;
                c.Seat = packet.Seat;
            }

            if (c.IsAiming) c.AimCoords = packet.AimCoords;
            bool full = packet.Flags.HasPedFlag(PedDataFlags.IsFullSync);
            if (full)
            {
                if (packet.Speed == 4)
                    c.VehicleWeapon = packet.VehicleWeapon;
                c.CurrentWeapon = packet.CurrentWeapon;
                c.Clothes = packet.Clothes;
                c.WeaponComponents = packet.WeaponComponents;
                c.WeaponTint = packet.WeaponTint;
                c.Model = packet.ModelHash;
                c.BlipColor = packet.BlipColor;
                c.BlipSprite = packet.BlipSprite;
                c.BlipScale = packet.BlipScale;
            }
            c.SetLastSynced(full);
        }

        private static void VehicleSync(Packets.VehicleSync packet)
        {
            var v = EntityPool.GetVehicleByID(packet.ED.ID);
            if (v == null) EntityPool.ThreadSafe.Add(v = new SyncedVehicle(packet.ED.ID));
            if (v.IsLocal) return;
            v.ID = packet.ED.ID;
            v.OwnerID = packet.ED.OwnerID;
            v.Position = packet.ED.Position;
            v.Quaternion = packet.ED.Quaternion;
            v.Velocity = packet.ED.Velocity;
            v.Model = packet.ED.ModelHash;
            v.VD = packet.VD;
            bool full = packet.VD.Flags.HasVehFlag(VehicleDataFlags.IsFullSync);
            if (full)
            {
                v.VDF = packet.VDF;
                v.VDV = packet.VDV;
            }
            v.SetLastSynced(full);
        }

        private static void ProjectileSync(Packets.ProjectileSync packet)
        {
            var p = EntityPool.GetProjectileByID(packet.ID);
            if (p == null)
            {
                if (packet.Flags.HasProjDataFlag(ProjectileDataFlags.Exploded)) return;
                // Log.Debug($"Creating new projectile: {(WeaponHash)packet.WeaponHash}");
                EntityPool.ThreadSafe.Add(p = new SyncedProjectile(packet.ID));
            }

            p.Flags = packet.Flags;
            p.Position = packet.Position;
            p.Rotation = packet.Rotation;
            p.Velocity = packet.Velocity;
            p.WeaponHash = (WeaponHash)packet.WeaponHash;
            p.Shooter = packet.Flags.HasProjDataFlag(ProjectileDataFlags.IsShotByVehicle)
                ? (SyncedEntity)EntityPool.GetVehicleByID(packet.ShooterID)
                : EntityPool.GetPedByID(packet.ShooterID);
            p.SetLastSynced(false);
        }

        /// <summary>
        ///     Reduce GC pressure by reusing frequently used packets
        /// </summary>
        private static class ReceivedPackets
        {
            public static readonly Packets.PedSync PedPacket = new Packets.PedSync();
            public static readonly Packets.VehicleSync VehicelPacket = new Packets.VehicleSync();
            public static readonly Packets.ProjectileSync ProjectilePacket = new Packets.ProjectileSync();
        }
    }
}