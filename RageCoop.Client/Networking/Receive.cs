using GTA;
using Lidgren.Network;
using RageCoop.Client.Menus;
using RageCoop.Core;
using System;
using System.Collections.Generic;
using System.Threading;

namespace RageCoop.Client
{
    internal static partial class Networking
    {
        private static readonly Func<byte, BitReader, object> _resolveHandle = (t, reader) =>
           {
               switch (t)
               {
                   case 50:
                       return EntityPool.ServerProps[reader.ReadInt()].MainProp?.Handle;
                   case 51:
                       return EntityPool.GetPedByID(reader.ReadInt())?.MainPed?.Handle;
                   case 52:
                       return EntityPool.GetVehicleByID(reader.ReadInt())?.MainVehicle?.Handle;
                   case 60:
                       return EntityPool.ServerBlips[reader.ReadInt()].Handle;
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
#if !NON_INTERACTIVE
                            CoopMenu.InitiateConnectionMenuSetting();
#endif
                            break;
                        case NetConnectionStatus.Connected:
                            Memory.ApplyPatches();
                            Main.QueueAction(() =>
                            {
                                CoopMenu.ConnectedMenuSetting();
                                Main.MainChat.Init();
                                GTA.UI.Notification.Show("~g~Connected!");
                            });

                            Main.Logger.Info(">> Connected <<");
                            break;
                        case NetConnectionStatus.Disconnected:
                            Memory.RestorePatches();
                            DownloadManager.Cleanup();

                            if (Main.MainChat.Focused)
                            {
                                Main.MainChat.Focused = false;
                            }

                            Main.QueueAction(() => Main.CleanUp());

#if !NON_INTERACTIVE
                            CoopMenu.DisconnectedMenuSetting();
#endif
                            Main.Logger.Info($">> Disconnected << reason: {reason}");
                            Main.QueueAction(() =>
                                GTA.UI.Notification.Show("~r~Disconnected: " + reason));
                            Main.Resources.Unload();


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
                                            var response = Client.CreateMessage();
                                            response.Write((byte)PacketType.Response);
                                            response.Write(id);
                                            handler(message.ReadBytes(len)).Pack(response);
                                            Client.SendMessage(response, NetDeliveryMethod.ReliableOrdered, message.SequenceChannel);
                                            Client.FlushSendQueue();
                                        }
                                        break;
                                    }
                                default:
                                    {
                                        byte[] data = message.ReadBytes(message.ReadInt32());

                                        HandlePacket(packetType, data);
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
                            Client.Disconnect($"Packet Error [{packetType}]");
                        }
                        break;
                    }
                case NetIncomingMessageType.UnconnectedData:
                    {
                        var packetType = (PacketType)message.ReadByte();
                        int len = message.ReadInt32();
                        byte[] data = message.ReadBytes(len);
                        if (packetType==PacketType.PublicKeyResponse)
                        {
                            var packet = new Packets.PublicKeyResponse();
                            packet.Unpack(data);
                            Security.SetServerPublicKey(packet.Modulus, packet.Exponent);
                            _publicKeyReceived.Set();
                        }

                        break;
                    }
                case NetIncomingMessageType.DebugMessage:
                case NetIncomingMessageType.ErrorMessage:
                case NetIncomingMessageType.WarningMessage:
                case NetIncomingMessageType.VerboseDebugMessage:
                    break;
                default:
                    break;
            }

            Client.Recycle(message);
        }
        private static void HandlePacket(PacketType packetType, byte[] data)
        {

            switch (packetType)
            {
                case PacketType.PlayerConnect:
                    {

                        Packets.PlayerConnect packet = new Packets.PlayerConnect();
                        packet.Unpack(data);

                        Main.QueueAction(() => PlayerConnect(packet));
                    }
                    break;
                case PacketType.PlayerDisconnect:
                    {

                        Packets.PlayerDisconnect packet = new Packets.PlayerDisconnect();
                        packet.Unpack(data);
                        Main.QueueAction(() => PlayerDisconnect(packet));

                    }
                    break;
                case PacketType.PlayerInfoUpdate:
                    {
                        var packet = new Packets.PlayerInfoUpdate();
                        packet.Unpack(data);
                        PlayerList.UpdatePlayer(packet);
                        break;
                    }
                #region ENTITY SYNC
                case PacketType.VehicleSync:
                    {

                        Packets.VehicleSync packet = new Packets.VehicleSync();
                        packet.Unpack(data);
                        VehicleSync(packet);

                    }
                    break;
                case PacketType.PedSync:
                    {

                        Packets.PedSync packet = new Packets.PedSync();
                        packet.Unpack(data);
                        PedSync(packet);

                    }
                    break;
                case PacketType.ProjectileSync:
                    {
                        Packets.ProjectileSync packet = new Packets.ProjectileSync();
                        packet.Unpack(data);
                        ProjectileSync(packet);
                        break;
                    }
                #endregion
                case PacketType.ChatMessage:
                    {

                        Packets.ChatMessage packet = new Packets.ChatMessage((b) =>
                        {
                            return Security.Decrypt(b);
                        });
                        packet.Unpack(data);

                        Main.QueueAction(() => { Main.MainChat.AddMessage(packet.Username, packet.Message); return true; });


                    }
                    break;
                case PacketType.CustomEvent:
                    {
                        Packets.CustomEvent packet = new Packets.CustomEvent(_resolveHandle);
                        packet.Unpack(data);
                        Scripting.API.Events.InvokeCustomEventReceived(packet);
                    }
                    break;
                case PacketType.CustomEventQueued:
                    {
                        Packets.CustomEvent packet = new Packets.CustomEvent(_resolveHandle);
                        Main.QueueAction(() =>
                        {
                            packet.Unpack(data);
                            Scripting.API.Events.InvokeCustomEventReceived(packet);
                        });
                    }
                    break;
                case PacketType.FileTransferChunk:
                    {
                        Packets.FileTransferChunk packet = new Packets.FileTransferChunk();
                        packet.Unpack(data);
                        DownloadManager.Write(packet.ID, packet.FileChunk);
                    }
                    break;
                default:
                    if (packetType.IsSyncEvent())
                    {
                        // Dispatch to main thread
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
            c.CurrentWeaponHash = packet.CurrentWeaponHash;
            c.IsAiming = flags.HasPedFlag(PedDataFlags.IsAiming);
            c.IsReloading = flags.HasPedFlag(PedDataFlags.IsReloading);
            c.IsJumping = flags.HasPedFlag(PedDataFlags.IsJumping);
            c.IsRagdoll = flags.HasPedFlag(PedDataFlags.IsRagdoll);
            c.IsOnFire = flags.HasPedFlag(PedDataFlags.IsOnFire);
            c.IsInParachuteFreeFall = flags.HasPedFlag(PedDataFlags.IsInParachuteFreeFall);
            c.IsParachuteOpen = flags.HasPedFlag(PedDataFlags.IsParachuteOpen);
            c.IsOnLadder = flags.HasPedFlag(PedDataFlags.IsOnLadder);
            c.IsVaulting = flags.HasPedFlag(PedDataFlags.IsVaulting);
            c.IsInCover = flags.HasPedFlag(PedDataFlags.IsInCover);
            c.IsInStealthMode = flags.HasPedFlag(PedDataFlags.IsInStealthMode);
            c.Heading=packet.Heading;
            c.Position = packet.Position;
            if (c.IsRagdoll)
            {
                c.HeadPosition=packet.HeadPosition;
                c.RightFootPosition=packet.RightFootPosition;
                c.LeftFootPosition=packet.LeftFootPosition;
            }
            c.LastSynced =  Main.Ticked;
            if (c.IsAiming)
            {
                c.AimCoords = packet.AimCoords;
            }
            if (packet.Flags.HasPedFlag(PedDataFlags.IsFullSync))
            {
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
            v.Acceleration=packet.Acceleration;
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
                v.Passengers=new Dictionary<VehicleSeat, SyncedPed>();
                v.LockStatus=packet.LockStatus;
                v.RadioStation=packet.RadioStation;
                v.LicensePlate=packet.LicensePlate;
                v.Livery=packet.Livery;
                foreach (KeyValuePair<int, int> pair in packet.Passengers)
                {
                    if (EntityPool.PedExists(pair.Value))
                    {
                        v.Passengers.Add((VehicleSeat)pair.Key, EntityPool.GetPedByID(pair.Value));
                    }
                }
                v.LastFullSynced= Main.Ticked;
            }
        }
        private static void ProjectileSync(Packets.ProjectileSync packet)
        {
            var p = EntityPool.GetProjectileByID(packet.ID);
            if (p==null)
            {
                if (packet.Exploded) { return; }
                // Main.Logger.Debug($"Creating new projectile: {(WeaponHash)packet.WeaponHash}");
                EntityPool.ThreadSafe.Add(p=new SyncedProjectile(packet.ID));
            }
            p.Position=packet.Position;
            p.Rotation=packet.Rotation;
            p.Velocity=packet.Velocity;
            p.Hash=(WeaponHash)packet.WeaponHash;
            p.ShooterID=packet.ShooterID;
            p.Exploded=packet.Exploded;
            p.LastSynced=Main.Ticked;
        }
    }
}
