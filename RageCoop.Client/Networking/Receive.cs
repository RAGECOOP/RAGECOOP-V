using GTA;
using Lidgren.Network;
using RageCoop.Client.Menus;
using RageCoop.Core;
using System;
using System.Linq;
using System.Threading;

namespace RageCoop.Client
{
    internal static partial class Networking
    {

        /// <summary>
        /// Reduce GC pressure by reusing frequently used packets
        /// </summary>
        private static class ReceivedPackets
        {
            public static Packets.PedSync PedPacket = new Packets.PedSync();
            public static Packets.VehicleSync VehicelPacket = new Packets.VehicleSync();
            public static Packets.ProjectileSync ProjectilePacket = new Packets.ProjectileSync();
        }

        /// <summary>
        /// Used to reslove entity handle in a <see cref="Packets.CustomEvent"/>
        /// </summary>
        private static readonly Func<byte, NetIncomingMessage, object> _resolveHandle = (t, reader) =>
           {
               switch (t)
               {
                   case 50:
                       return EntityPool.ServerProps[reader.ReadInt32()].MainProp?.Handle;
                   case 51:
                       return EntityPool.GetPedByID(reader.ReadInt32())?.MainPed?.Handle;
                   case 52:
                       return EntityPool.GetVehicleByID(reader.ReadInt32())?.MainVehicle?.Handle;
                   case 60:
                       return EntityPool.ServerBlips[reader.ReadInt32()].Handle;
                   default:
                       throw new ArgumentException("Cannot resolve server side argument: " + t);
               }
           };
        private static readonly AutoResetEvent _publicKeyReceived = new AutoResetEvent(false);
        private static bool _recycle;
        public static void ProcessMessage(NetIncomingMessage message)
        {
            if (message == null) { return; }
            _recycle = true;
            switch (message.MessageType)
            {
                case NetIncomingMessageType.StatusChanged:
                    NetConnectionStatus status = (NetConnectionStatus)message.ReadByte();
                    string reason = message.ReadString();
                    switch (status)
                    {
                        case NetConnectionStatus.InitiatedConnect:
                            if (message.SenderConnection == ServerConnection)
                            {
                                CoopMenu.InitiateConnectionMenuSetting();
                            }
                            break;
                        case NetConnectionStatus.Connected:
                            if (message.SenderConnection == ServerConnection)
                            {
                                var response = message.SenderConnection.RemoteHailMessage;
                                if ((PacketType)response.ReadByte() != PacketType.HandshakeSuccess)
                                {
                                    throw new Exception("Invalid handshake response!");
                                }
                                var p = new Packets.HandshakeSuccess();
                                p.Deserialize(response);
                                foreach (var player in p.Players)
                                {
                                    PlayerList.SetPlayer(player.ID, player.Username);
                                }
                                Main.Connected();
                            }
                            else
                            {
                                // Self-initiated connection
                                if (message.SenderConnection.RemoteHailMessage == null) { return; }

                                var p = message.SenderConnection.RemoteHailMessage.GetPacket<Packets.P2PConnect>();
                                if (PlayerList.Players.TryGetValue(p.ID, out var player))
                                {
                                    player.Connection = message.SenderConnection;
                                    Main.Logger.Debug($"Direct connection to {player.Username} established");
                                }
                                else
                                {
                                    Main.Logger.Info($"Unidentified peer connection from {message.SenderEndPoint} was rejected.");
                                    message.SenderConnection.Disconnect("eat poop");
                                }
                            }
                            break;
                        case NetConnectionStatus.Disconnected:
                            if (message.SenderConnection == ServerConnection)
                            {
                                Main.Disconnected(reason);
                            }
                            break;
                    }
                    break;
                case NetIncomingMessageType.Data:
                    {
                        if (message.LengthBytes == 0) { break; }
                        var packetType = PacketType.Unknown;
                        try
                        {
                            // Get packet type
                            packetType = (PacketType)message.ReadByte();
                            switch (packetType)
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
                                        var realType = (PacketType)message.ReadByte();
                                        if (RequestHandlers.TryGetValue(realType, out var handler))
                                        {
                                            var response = Peer.CreateMessage();
                                            response.Write((byte)PacketType.Response);
                                            response.Write(id);
                                            handler(message).Pack(response);
                                            Peer.SendMessage(response, ServerConnection, NetDeliveryMethod.ReliableOrdered, message.SequenceChannel);
                                            Peer.FlushSendQueue();
                                        }
                                        else
                                        {
                                            Main.Logger.Debug("Did not find a request handler of type: " + realType);
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
#if DEBUG
                            Main.QueueAction(() =>
                            {
                                GTA.UI.Notification.Show($"~r~~h~Packet Error {ex.Message}");
                                return true;
                            });
                            Main.Logger.Error($"[{packetType}] {ex.Message}");
                            Main.Logger.Error(ex);
                            Peer.Shutdown($"Packet Error [{packetType}]");
#endif
                            _recycle = false;
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
                                    if (message.SenderEndPoint.ToString() != _targetServerEP.ToString() || !IsConnecting) { break; }
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
                    Main.Logger.Trace(message.ReadString());
                    break;
                default:
                    break;
            }
            if (_recycle)
            {
                Peer.Recycle(message);
            }
        }
        private static void HandlePacket(PacketType packetType, NetIncomingMessage msg, NetConnection senderConnection, ref bool recycle)
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

                        Packets.ChatMessage packet = new Packets.ChatMessage((b) => Security.Decrypt(b));
                        packet.Deserialize(msg);

                        Main.QueueAction(() => { Main.MainChat.AddMessage(packet.Username, packet.Message); return true; });
                    }
                    break;

                case PacketType.Voice:
                    {
                        if (Main.Settings.Voice)
                        {
                            Packets.Voice packet = new Packets.Voice();
                            packet.Deserialize(msg);


                            SyncedPed player = EntityPool.GetPedByID(packet.ID);
                            player.IsSpeaking = true;
                            player.LastSpeakingTime = Main.Ticked;

                            Voice.AddVoiceData(packet.Buffer, packet.Recorded);
                        }
                    }
                    break;

                case PacketType.CustomEvent:
                    {
                        Packets.CustomEvent packet = new Packets.CustomEvent(_resolveHandle);
                        packet.Deserialize(msg);
                        Scripting.API.Events.InvokeCustomEventReceived(packet);
                    }
                    break;

                case PacketType.CustomEventQueued:
                    {
                        recycle = false;
                        Packets.CustomEvent packet = new Packets.CustomEvent(_resolveHandle);
                        Main.QueueAction(() =>
                        {
                            packet.Deserialize(msg);
                            Scripting.API.Events.InvokeCustomEventReceived(packet);
                            Peer.Recycle(msg);
                        });
                    }
                    break;

                case PacketType.FileTransferChunk:
                    {
                        Packets.FileTransferChunk packet = new Packets.FileTransferChunk();
                        packet.Deserialize(msg);
                        DownloadManager.Write(packet.ID, packet.FileChunk);
                    }
                    break;

                default:
                    if (packetType.IsSyncEvent())
                    {
                        recycle = false;
                        // Dispatch to script thread
                        Main.QueueAction(() => { SyncEvents.HandleEvent(packetType, msg); return true; });
                    }
                    break;
            }
        }

        private static void PedSync(Packets.PedSync packet)
        {
            SyncedPed c = EntityPool.GetPedByID(packet.ID);
            if (c == null)
            {
                if (EntityPool.PedsByID.Count(x => x.Value.OwnerID == packet.OwnerID) < Main.Settings.WorldPedSoftLimit / PlayerList.Players.Count ||
                    EntityPool.VehiclesByID.Any(x => x.Value.Position.DistanceTo(packet.Position) < 2) || packet.ID == packet.OwnerID)
                {
                    // Main.Logger.Debug($"Creating character for incoming sync:{packet.ID}");
                    EntityPool.ThreadSafe.Add(c = new SyncedPed(packet.ID));
                }
                else return;
            }
            c.ID = packet.ID;
            c.OwnerID = packet.OwnerID;
            c.Health = packet.Health;
            c.Rotation = packet.Rotation;
            c.Velocity = packet.Velocity;
            c.Speed = packet.Speed;
            c.Flags = packet.Flags;
            c.Heading = packet.Heading;
            c.Position = packet.Position;
            c.LastSyncedStopWatch.Restart();
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
            c.LastSynced = Main.Ticked;
            if (c.IsAiming)
            {
                c.AimCoords = packet.AimCoords;
            }
            if (packet.Flags.HasPedFlag(PedDataFlags.IsFullSync))
            {
                c.CurrentWeaponHash = packet.CurrentWeaponHash;
                c.Clothes = packet.Clothes;
                c.WeaponComponents = packet.WeaponComponents;
                c.WeaponTint = packet.WeaponTint;
                c.Model = packet.ModelHash;
                c.BlipColor = packet.BlipColor;
                c.BlipSprite = packet.BlipSprite;
                c.BlipScale = packet.BlipScale;
                c.LastFullSynced = Main.Ticked;
            }

        }
        private static void VehicleSync(Packets.VehicleSync packet)
        {
            SyncedVehicle v = EntityPool.GetVehicleByID(packet.ID);
            if (v == null)
            {
                if (EntityPool.VehiclesByID.Count(x => x.Value.OwnerID == packet.OwnerID) < Main.Settings.WorldVehicleSoftLimit / PlayerList.Players.Count ||
                    EntityPool.PedsByID.Any(x => x.Value.VehicleID == packet.ID || x.Value.Position.DistanceTo(packet.Position) < 2))
                {
                    // Main.Logger.Debug($"Creating vehicle for incoming sync:{packet.ID}");
                    EntityPool.ThreadSafe.Add(v = new SyncedVehicle(packet.ID));
                }
                else return;
            }
            if (v.IsLocal) { return; }
            v.ID = packet.ID;
            v.OwnerID = packet.OwnerID;
            v.Flags = packet.Flags;
            v.Position = packet.Position;
            v.Quaternion = packet.Quaternion;
            v.SteeringAngle = packet.SteeringAngle;
            v.ThrottlePower = packet.ThrottlePower;
            v.BrakePower = packet.BrakePower;
            v.Velocity = packet.Velocity;
            v.RotationVelocity = packet.RotationVelocity;
            v.DeluxoWingRatio = packet.DeluxoWingRatio;
            v.LastSynced = Main.Ticked;
            v.LastSyncedStopWatch.Restart();
            if (packet.Flags.HasVehFlag(VehicleDataFlags.IsFullSync))
            {
                v.DamageModel = packet.DamageModel;
                v.EngineHealth = packet.EngineHealth;
                v.Mods = packet.Mods;
                v.Model = packet.ModelHash;
                v.Colors = packet.Colors;
                v.LandingGear = packet.LandingGear;
                v.RoofState = (VehicleRoofState)packet.RoofState;
                v.LockStatus = packet.LockStatus;
                v.RadioStation = packet.RadioStation;
                v.LicensePlate = packet.LicensePlate;
                v.Livery = packet.Livery;
                v.LastFullSynced = Main.Ticked;
            }
        }
        private static void ProjectileSync(Packets.ProjectileSync packet)
        {
            var p = EntityPool.GetProjectileByID(packet.ID);
            if (p == null)
            {
                if (packet.Flags.HasProjDataFlag(ProjectileDataFlags.Exploded)) { return; }
                // Main.Logger.Debug($"Creating new projectile: {(WeaponHash)packet.WeaponHash}");
                EntityPool.ThreadSafe.Add(p = new SyncedProjectile(packet.ID));
            }
            p.Flags = packet.Flags;
            p.Position = packet.Position;
            p.Rotation = packet.Rotation;
            p.Velocity = packet.Velocity;
            p.WeaponHash = (WeaponHash)packet.WeaponHash;
            p.Shooter = packet.Flags.HasProjDataFlag(ProjectileDataFlags.IsShotByVehicle) ?
                (SyncedEntity)EntityPool.GetVehicleByID(packet.ShooterID) : EntityPool.GetPedByID(packet.ShooterID);
            p.LastSynced = Main.Ticked;
            p.LastSyncedStopWatch.Restart();
        }
    }
}
