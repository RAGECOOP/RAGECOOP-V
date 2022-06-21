using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using Lidgren.Network;
using RageCoop.Core;
using GTA;
using RageCoop.Client.Menus;
using GTA.Math;
using GTA.Native;

namespace RageCoop.Client
{
    internal static partial class Networking
    {
        public static void ProcessMessage(NetIncomingMessage message)
        {
            if(message == null) { return; }

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
                            Main.QueueAction(() => { GTA.UI.Notification.Show("~y~Trying to connect..."); return true; });
                            break;
                        case NetConnectionStatus.Connected:
                            if (message.SenderConnection.RemoteHailMessage.ReadByte() != (byte)PacketTypes.Handshake)
                            {
                                Client.Disconnect("Wrong packet!");
                            }
                            else
                            {
                                int len = message.SenderConnection.RemoteHailMessage.ReadInt32();
                                byte[] data = message.SenderConnection.RemoteHailMessage.ReadBytes(len);

                                Packets.Handshake handshakePacket = new Packets.Handshake();
                                handshakePacket.Unpack(data);

#if !NON_INTERACTIVE

#endif

                                Main.QueueAction(() => {
                                    CoopMenu.ConnectedMenuSetting();
                                    Main.MainChat.Init();
                                    PlayerList.Cleanup();
                                    GTA.UI.Notification.Show("~g~Connected!");
                                });

                                Main.Logger.Info(">> Connected <<");
                            }
                            break;
                        case NetConnectionStatus.Disconnected:
                            DownloadManager.Cleanup();

                            // Reset all values
                            Latency = 0;

                            Main.QueueAction(() => Main.CleanUpWorld());

                            if (Main.MainChat.Focused)
                            {
                                Main.MainChat.Focused = false;
                            }

                            Main.QueueAction(() => Main.CleanUp());

#if !NON_INTERACTIVE
                            CoopMenu.DisconnectedMenuSetting();
#endif

                            Main.QueueAction(() =>
                                GTA.UI.Notification.Show("~r~Disconnected: " + reason));

                            MapLoader.DeleteAll();
                            Main.Resources.Unload();

                            Main.Logger.Info($">> Disconnected << reason: {reason}");
                            break;
                    }
                    break;
                case NetIncomingMessageType.Data:
                    if (message.LengthBytes==0) { break; }

                    var packetType = (PacketTypes)message.ReadByte();
                    try
                    {

                        int len = message.ReadInt32();
                        byte[] data = message.ReadBytes(len);
                        switch (packetType)
                        {
                            case PacketTypes.CleanUpWorld:
                                {
                                    Main.QueueAction(() => { Main.CleanUpWorld(); return true; });
                                }
                                break;
                            case PacketTypes.PlayerConnect:
                                {

                                    Packets.PlayerConnect packet = new Packets.PlayerConnect();
                                    packet.Unpack(data);

                                    Main.QueueAction(() => PlayerConnect(packet));
                                }
                                break;
                            case PacketTypes.PlayerDisconnect:
                                {

                                    Packets.PlayerDisconnect packet = new Packets.PlayerDisconnect();
                                    packet.Unpack(data);
                                    Main.QueueAction(() => PlayerDisconnect(packet));

                                }
                                break;
                            case PacketTypes.PlayerInfoUpdate:
                                {
                                    var packet = new Packets.PlayerInfoUpdate();
                                    packet.Unpack(data);
                                    PlayerList.SetPlayer(packet.PedID, packet.Username, packet.Latency);
                                    break;
                                }
                            #region ENTITY SYNC
                            case PacketTypes.VehicleSync:
                                {

                                    Packets.VehicleSync packet = new Packets.VehicleSync();
                                    packet.Unpack(data);
                                    VehicleSync(packet);

                                }
                                break;
                            case PacketTypes.PedSync:
                                {

                                    Packets.PedSync packet = new Packets.PedSync();
                                    packet.Unpack(data);
                                    PedSync(packet);

                                }
                                break;
                            case PacketTypes.VehicleStateSync:
                                {

                                    Packets.VehicleStateSync packet = new Packets.VehicleStateSync();
                                    packet.Unpack(data);
                                    VehicleStateSync(packet);

                                }
                                break;
                            case PacketTypes.PedStateSync:
                                {


                                    Packets.PedStateSync packet = new Packets.PedStateSync();
                                    packet.Unpack(data);
                                    PedStateSync(packet);

                                }
                                break;
                            case PacketTypes.ProjectileSync:
                                {
                                    Packets.ProjectileSync packet = new Packets.ProjectileSync();
                                    packet.Unpack(data);
                                    ProjectileSync(packet);
                                    break;
                                }
                            #endregion
                            case PacketTypes.ChatMessage:
                                {

                                    Packets.ChatMessage packet = new Packets.ChatMessage();
                                    packet.Unpack(data);

                                    Main.QueueAction(() => { Main.MainChat.AddMessage(packet.Username, packet.Message); return true; });


                                }
                                break;
                            case PacketTypes.NativeCall:
                                {

                                    Packets.NativeCall packet = new Packets.NativeCall();
                                    packet.Unpack(data);

                                    DecodeNativeCall(packet.Hash, packet.Args, false);

                                }
                                break;
                            case PacketTypes.NativeResponse:
                                {

                                    Packets.NativeResponse packet = new Packets.NativeResponse();
                                    packet.Unpack(data);

                                    DecodeNativeResponse(packet);

                                }
                                break;
                            case PacketTypes.CustomEvent:
                                {
                                    Packets.CustomEvent packet = new Packets.CustomEvent();
                                    packet.Unpack(data);
                                    Scripting.API.Events.InvokeCustomEventReceived(packet.Hash, packet.Args);
                                }
                                break;
                            case PacketTypes.FileTransferChunk:
                                {
                                    Packets.FileTransferChunk packet = new Packets.FileTransferChunk();
                                    packet.Unpack(data);

                                    DownloadManager.Write(packet.ID, packet.FileChunk);

                                }
                                break;
                            case PacketTypes.FileTransferRequest:
                                {
                                    Packets.FileTransferRequest packet = new Packets.FileTransferRequest();
                                    packet.Unpack(data);

                                    DownloadManager.AddFile(packet.ID, packet.Name, packet.FileLength);

                                }
                                break;
                            case PacketTypes.FileTransferComplete:
                                {
                                    Packets.FileTransferComplete packet = new Packets.FileTransferComplete();
                                    packet.Unpack(data);

                                    Main.Logger.Debug($"Finalizing download:{packet.ID}");
                                    DownloadManager.Complete(packet.ID);

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
                    catch (Exception ex)
                    {
                        Main.QueueAction(() => {
                            GTA.UI.Notification.Show("~r~~h~Packet Error");
                            return true;
                        });
                        Main.Logger.Error($"[{packetType}] {ex.Message}");
                        Main.Logger.Error(ex);
                        Client.Disconnect($"Packet Error [{packetType}]");
                    }
                    break;
                case NetIncomingMessageType.ConnectionLatencyUpdated:
                    Latency = message.ReadFloat();
                    break;
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


        private static void PedSync(Packets.PedSync packet)
        {
            SyncedPed c = EntityPool.GetPedByID(packet.ID);
            if (c==null)
            {
                Main.Logger.Debug($"Creating character for incoming sync:{packet.ID}");
                EntityPool.ThreadSafe.Add(c=new SyncedPed(packet.ID));
            }
            PedDataFlags flags = packet.Flag;
            c.ID=packet.ID;
            //c.OwnerID=packet.OwnerID;
            c.Health = packet.Health;
            c.Position = packet.Position;
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
            c.LastSynced =  Main.Ticked;
            if (c.IsAiming)
            {
                c.AimCoords = packet.AimCoords;
            }
            if (c.IsRagdoll)
            {
                c.RotationVelocity=packet.RotationVelocity;
            }
        }
        private static void PedStateSync(Packets.PedStateSync packet)
        {
            SyncedPed c = EntityPool.GetPedByID(packet.ID);
            if (c==null) { return; }
            c.ID=packet.ID;
            c.OwnerID=packet.OwnerID;
            c.Clothes=packet.Clothes;
            c.WeaponComponents=packet.WeaponComponents;
            c.WeaponTint=packet.WeaponTint;
            c.ModelHash=packet.ModelHash;
            c.LastStateSynced = Main.Ticked;
        }
        private static void VehicleSync(Packets.VehicleSync packet)
        {
            SyncedVehicle v = EntityPool.GetVehicleByID(packet.ID); 
            if (v==null)
            {
                EntityPool.ThreadSafe.Add(v=new SyncedVehicle(packet.ID));
            }
            if (v.IsMine) { return; }
            v.ID= packet.ID;
            v.Position=packet.Position;
            v.Quaternion=packet.Quaternion;
            v.SteeringAngle=packet.SteeringAngle;
            v.ThrottlePower=packet.ThrottlePower;
            v.BrakePower=packet.BrakePower;
            v.Velocity=packet.Velocity;
            v.RotationVelocity=packet.RotationVelocity;
            v.DeluxoWingRatio=packet.DeluxoWingRatio;
            v.LastSynced=Main.Ticked;
        }
        private static void VehicleStateSync(Packets.VehicleStateSync packet)
        {
            SyncedVehicle v = EntityPool.GetVehicleByID(packet.ID);
            if (v==null||v.IsMine) { return; }
            v.ID= packet.ID;
            v.OwnerID= packet.OwnerID;
            v.DamageModel=packet.DamageModel;
            v.EngineHealth=packet.EngineHealth;
            v.OwnerID=packet.OwnerID;
            v.Mods=packet.Mods;
            v.ModelHash=packet.ModelHash;
            v.Colors=packet.Colors;
            v.LandingGear=packet.LandingGear;
            v.RoofState=(VehicleRoofState)packet.RoofState;
            v.EngineRunning = packet.Flag.HasVehFlag(VehicleDataFlags.IsEngineRunning);
            v.LightsOn = packet.Flag.HasVehFlag(VehicleDataFlags.AreLightsOn);
            v.BrakeLightsOn = packet.Flag.HasVehFlag(VehicleDataFlags.AreBrakeLightsOn);
            v.HighBeamsOn = packet.Flag.HasVehFlag(VehicleDataFlags.AreHighBeamsOn);
            v.SireneActive = packet.Flag.HasVehFlag(VehicleDataFlags.IsSirenActive);
            v.IsDead = packet.Flag.HasVehFlag(VehicleDataFlags.IsDead);
            v.HornActive = packet.Flag.HasVehFlag(VehicleDataFlags.IsHornActive);
            v.Transformed = packet.Flag.HasVehFlag(VehicleDataFlags.IsTransformed);
            v.Passengers=new Dictionary<VehicleSeat, SyncedPed>();
            v.LockStatus=packet.LockStatus;
            v.RadioStation=packet.RadioStation;
            v.LicensePlate=packet.LicensePlate;
            v.Flags=packet.Flag;
            foreach (KeyValuePair<int, int> pair in packet.Passengers)
            {
                if (EntityPool.PedExists(pair.Value))
                {
                    v.Passengers.Add((VehicleSeat)pair.Key, EntityPool.GetPedByID(pair.Value));
                }
            }
            v.LastStateSynced= Main.Ticked;
            
        }
        private static void ProjectileSync(Packets.ProjectileSync packet)
        {
            var p = EntityPool.GetProjectileByID(packet.ID);
            if (p==null)
            {
                if (packet.Exploded) { return; }
                Main.Logger.Debug($"Creating new projectile: {(WeaponHash)packet.WeaponHash}");
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
