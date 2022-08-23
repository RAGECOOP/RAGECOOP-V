using GTA;
using Lidgren.Network;
using RageCoop.Client.Menus;
using RageCoop.Core;
using System;
using System.Collections.Generic;
using System.Threading;
using GTA.Native;

namespace RageCoop.Client
{
    internal static partial class Networking
    {

        /// <summary>
        /// Reduce GC pressure by reusing frequently used packets
        /// </summary>
        static class ReceivedPackets
        {
            public static Packets.PedSync PedPacket = new Packets.PedSync();
            public static Packets.VehicleSync VehicelPacket = new Packets.VehicleSync();
            public static Packets.ProjectileSync ProjectilePacket = new Packets.ProjectileSync();
        }

        /// <summary>
        /// Used to reslove entity handle in a <see cref="Packets.CustomEvent"/>
        /// </summary>
        private static readonly Func<byte, BitReader, object> _resolveHandle = (t, reader) =>
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
                       throw new ArgumentException("Cannot resolve server side argument: "+t);
               }
           };
        private static readonly AutoResetEvent _publicKeyReceived = new AutoResetEvent(false);
        public static void ProcessMessage(NetIncomingMessage message)
        {
            if (message == null) { return; }

            switch (message.MessageType)
            {
                case NetIncomingMessageType.StatusChanged:
                    NetConnectionStatus status = (NetConnectionStatus)message.ReadByte();
                    string reason = message.ReadString();
                    switch (status)
                    {
                        case NetConnectionStatus.InitiatedConnect:
                            if (message.SenderConnection==ServerConnection)
                            {
                                CoopMenu.InitiateConnectionMenuSetting();
                            }
                            break;
                        case NetConnectionStatus.Connected:
                            if (message.SenderConnection==ServerConnection)
                            {
                                Memory.ApplyPatches();
                                var response = message.SenderConnection.RemoteHailMessage;
                                if ((PacketType)response.ReadByte()!=PacketType.HandshakeSuccess)
                                {
                                    throw new Exception("Invalid handshake response!");
                                }
                                var p = new Packets.HandshakeSuccess();
                                p.Deserialize(response.ReadBytes(response.ReadInt32()));
                                foreach(var player in p.Players)
                                {
                                    PlayerList.SetPlayer(player.ID,player.Username);
                                }
                                Main.QueueAction(() =>
                                {
                                    WorldThread.Traffic(!Main.Settings.DisableTraffic);
                                    Function.Call(Hash.SET_ENABLE_VEHICLE_SLIPSTREAMING, true);
                                    CoopMenu.ConnectedMenuSetting();
                                    Main.MainChat.Init();
                                    if (Main.Settings.Voice && !Voice.WasInitialized())
                                    {
                                        Voice.Init();
                                    }
                                    GTA.UI.Notification.Show("~g~Connected!");
                                });
                                
                                Main.Logger.Info(">> Connected <<");
                            }
                            else
                            {
                                // Self-initiated connection
                                if (message.SenderConnection.RemoteHailMessage==null) { return; }
                                
                                var p = message.SenderConnection.RemoteHailMessage.GetPacket<Packets.P2PConnect>();
                                if (PlayerList.Players.TryGetValue(p.ID,out var player))
                                {
                                    player.Connection=message.SenderConnection;
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
                            if (message.SenderConnection==ServerConnection)
                            {
                                Memory.RestorePatches();
                                DownloadManager.Cleanup();

                                if (Main.MainChat.Focused)
                                {
                                    Main.MainChat.Focused = false;
                                }

                                Main.QueueAction(() => Main.CleanUp());
                                CoopMenu.DisconnectedMenuSetting();
                                Main.Logger.Info($">> Disconnected << reason: {reason}");
                                Main.QueueAction(() => GTA.UI.Notification.Show("~r~Disconnected: " + reason));
                                Main.Resources.Unload();
                            }
                            break;
                    }
                    break;
                case NetIncomingMessageType.Data:
                    {
                        if (message.LengthBytes==0) { break; }
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
                                            callback((PacketType)message.ReadByte(), message.ReadBytes(message.ReadInt32()));
                                            PendingResponses.Remove(id);
                                        }
                                        break;
                                    }
                                case PacketType.Request:
                                    {
                                        int id = message.ReadInt32();
                                        var realType = (PacketType)message.ReadByte();
                                        int len = message.ReadInt32();
                                        if (RequestHandlers.TryGetValue(realType, out var handler))
                                        {
                                            var response = Peer.CreateMessage();
                                            response.Write((byte)PacketType.Response);
                                            response.Write(id);
                                            handler(message.ReadBytes(len)).Pack(response);
                                            Peer.SendMessage(response,ServerConnection, NetDeliveryMethod.ReliableOrdered, message.SequenceChannel);
                                            Peer.FlushSendQueue();
                                        }
                                        break;
                                    }
                                default:
                                    {
                                        byte[] data = message.ReadBytes(message.ReadInt32());

                                        HandlePacket(packetType, data,message.SenderConnection);
                                        break;
                                    }
                            }
                        }
                        catch (Exception ex)
                        {
                            Main.QueueAction(() =>
                            {
                                GTA.UI.Notification.Show("~r~~h~Packet Error");
                                return true;
                            });
                            Main.Logger.Error($"[{packetType}] {ex.Message}");
                            Main.Logger.Error(ex);
                            Peer.Shutdown($"Packet Error [{packetType}]");
                        }
                        break;
                    }
                case NetIncomingMessageType.UnconnectedData:
                    {
                        var packetType = (PacketType)message.ReadByte();
                        int len = message.ReadInt32();
                        byte[] data = message.ReadBytes(len);
                        switch (packetType)
                        {

                            case PacketType.HolePunch:
                                {
                                    HolePunch.Punched(data.GetPacket<Packets.HolePunch>(), message.SenderEndPoint);
                                    break;
                                }
                            case PacketType.PublicKeyResponse:
                                { 
                                    if(message.SenderEndPoint.ToString()!=_targetServerEP.ToString() ||!IsConnecting){break;}
                                    var packet = data.GetPacket<Packets.PublicKeyResponse>();
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

            Peer.Recycle(message);
        }
        private static void HandlePacket(PacketType packetType, byte[] data, NetConnection senderConnection)
        {

            switch (packetType)
            {
                case PacketType.HolePunchInit:
                    HolePunch.Add(data.GetPacket<Packets.HolePunchInit>());
                    break;

                case PacketType.PlayerConnect:
                    PlayerConnect(data.GetPacket<Packets.PlayerConnect>());
                    break;

                case PacketType.PlayerDisconnect:
                    PlayerDisconnect(data.GetPacket<Packets.PlayerDisconnect>());
                    break;

                case PacketType.PlayerInfoUpdate:
                    PlayerList.UpdatePlayer(data.GetPacket<Packets.PlayerInfoUpdate>());
                    break;

                case PacketType.VehicleSync:
                    ReceivedPackets.VehicelPacket.Deserialize(data);
                    VehicleSync(ReceivedPackets.VehicelPacket);
                    break;

                case PacketType.PedSync:
                    ReceivedPackets.PedPacket.Deserialize(data);
                    PedSync(ReceivedPackets.PedPacket);
                    break;
                case PacketType.ProjectileSync:
                    ReceivedPackets.ProjectilePacket.Deserialize(data);
                    ProjectileSync(ReceivedPackets.ProjectilePacket);
                    break;

                case PacketType.ChatMessage:
                    {

                        Packets.ChatMessage packet = new Packets.ChatMessage((b) => Security.Decrypt(b));
                        packet.Deserialize(data);

                        Main.QueueAction(() => { Main.MainChat.AddMessage(packet.Username, packet.Message); return true; });
                    }
                    break;

                case PacketType.Voice:
                    {
                        if (Main.Settings.Voice)
                        {
                            Packets.Voice packet = new Packets.Voice();
                            packet.Deserialize(data);


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
                        packet.Deserialize(data);
                        Scripting.API.Events.InvokeCustomEventReceived(packet);
                    }
                    break;

                case PacketType.CustomEventQueued:
                    {
                        Packets.CustomEvent packet = new Packets.CustomEvent(_resolveHandle);
                        Main.QueueAction(() =>
                        {
                            packet.Deserialize(data);
                            Scripting.API.Events.InvokeCustomEventReceived(packet);
                        });
                    }
                    break;

                case PacketType.FileTransferChunk:
                    {
                        Packets.FileTransferChunk packet = new Packets.FileTransferChunk();
                        packet.Deserialize(data);
                        DownloadManager.Write(packet.ID, packet.FileChunk);
                    }
                    break;

                default:
                    if (packetType.IsSyncEvent())
                    {
                        // Dispatch to script thread
                        Main.QueueAction(() => { SyncEvents.HandleEvent(packetType, data); return true; });
                    }
                    break;
            }
        }

        private static void PedSync(Packets.PedSync packet)
        {
            SyncedPed c = EntityPool.GetPedByID(packet.ID);
            if (c==null)
            {
                // Main.Logger.Debug($"Creating character for incoming sync:{packet.ID}");
                EntityPool.ThreadSafe.Add(c=new SyncedPed(packet.ID));
            }
            PedDataFlags flags = packet.Flags;
            c.ID=packet.ID;
            c.OwnerID=packet.OwnerID;
            c.Health = packet.Health;
            c.Rotation = packet.Rotation;
            c.Velocity = packet.Velocity;
            c.Speed = packet.Speed;
            c.Flags=packet.Flags;
            c.Heading=packet.Heading;
            c.Position = packet.Position;
            c.LastSyncedStopWatch.Restart();
            if (c.IsRagdoll)
            {
                c.HeadPosition=packet.HeadPosition;
                c.RightFootPosition=packet.RightFootPosition;
                c.LeftFootPosition=packet.LeftFootPosition;
            }
            else if (c.Speed>=4)
            {
                c.VehicleID=packet.VehicleID;
                c.Seat=packet.Seat;
            }
            c.LastSynced =  Main.Ticked;
            if (c.IsAiming)
            {
                c.AimCoords = packet.AimCoords;
            }
            if (packet.Flags.HasPedFlag(PedDataFlags.IsFullSync))
            {
                c.CurrentWeaponHash = packet.CurrentWeaponHash;
                c.Clothes=packet.Clothes;
                c.WeaponComponents=packet.WeaponComponents;
                c.WeaponTint=packet.WeaponTint;
                c.Model=packet.ModelHash;
                c.BlipColor=packet.BlipColor;
                c.BlipSprite=packet.BlipSprite;
                c.BlipScale=packet.BlipScale;
                c.LastFullSynced = Main.Ticked;
            }

        }
        private static void VehicleSync(Packets.VehicleSync packet)
        {
            SyncedVehicle v = EntityPool.GetVehicleByID(packet.ID);
            if (v==null)
            {
                EntityPool.ThreadSafe.Add(v=new SyncedVehicle(packet.ID));
            }
            if (v.IsLocal) { return; }
            v.ID= packet.ID;
            v.OwnerID= packet.OwnerID;
            v.Flags=packet.Flags;
            v.Position=packet.Position;
            v.Quaternion=packet.Quaternion;
            v.SteeringAngle=packet.SteeringAngle;
            v.ThrottlePower=packet.ThrottlePower;
            v.BrakePower=packet.BrakePower;
            v.Velocity=packet.Velocity;
            v.RotationVelocity=packet.RotationVelocity;
            v.DeluxoWingRatio=packet.DeluxoWingRatio;
            v.LastSynced=Main.Ticked;
            v.LastSyncedStopWatch.Restart();
            if (packet.Flags.HasVehFlag(VehicleDataFlags.IsFullSync))
            {
                v.DamageModel=packet.DamageModel;
                v.EngineHealth=packet.EngineHealth;
                v.Mods=packet.Mods;
                v.Model=packet.ModelHash;
                v.Colors=packet.Colors;
                v.LandingGear=packet.LandingGear;
                v.RoofState=(VehicleRoofState)packet.RoofState;
                v.LockStatus=packet.LockStatus;
                v.RadioStation=packet.RadioStation;
                v.LicensePlate=packet.LicensePlate;
                v.Livery=packet.Livery;
                v.LastFullSynced= Main.Ticked;
            }
        }
        private static void ProjectileSync(Packets.ProjectileSync packet)
        {

            var p = EntityPool.GetProjectileByID(packet.ID);
            if (p==null)
            {
                if (packet.Flags.HasProjDataFlag(ProjectileDataFlags.Exploded)) { return; }
                // Main.Logger.Debug($"Creating new projectile: {(WeaponHash)packet.WeaponHash}");
                EntityPool.ThreadSafe.Add(p=new SyncedProjectile(packet.ID));
            }
            p.Flags=packet.Flags;
            p.Position=packet.Position;
            p.Rotation=packet.Rotation;
            p.Velocity=packet.Velocity;
            p.WeaponHash=(WeaponHash)packet.WeaponHash;
            p.Shooter= packet.Flags.HasProjDataFlag(ProjectileDataFlags.IsShotByVehicle) ?
                (SyncedEntity)EntityPool.GetVehicleByID(packet.ShooterID) : EntityPool.GetPedByID(packet.ShooterID);
            p.LastSynced=Main.Ticked;
        }
    }
}
