using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using Lidgren.Network;
using RageCoop.Core;
using GTA;
using RageCoop.Client.Menus;
using System.Threading;
using GTA.Math;
using GTA.Native;
using System.Net;

namespace RageCoop.Client
{
    internal static partial class Networking
    {
        private static AutoResetEvent PublicKeyReceived=new AutoResetEvent(false);

        private static Dictionary<int, Action<PacketType, byte[]>> PendingResponses = new Dictionary<int, Action<PacketType, byte[]>>();
        internal static Dictionary<PacketType, Func< byte[], Packet>> RequestHandlers = new Dictionary<PacketType, Func< byte[], Packet>>();
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
                            break;
                        case NetConnectionStatus.Connected:
                            Main.QueueAction(() => {
                                CoopMenu.ConnectedMenuSetting();
                                Main.MainChat.Init();
                                PlayerList.Cleanup();
                                GTA.UI.Notification.Show("~g~Connected!");
                            });

                            Main.Logger.Info(">> Connected <<");
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
                                        Main.Logger.Debug($"{id},{realType},{len}");
                                        if (RequestHandlers.TryGetValue(realType, out var handler))
                                        {
                                            var response = Client.CreateMessage();
                                            response.Write((byte)PacketType.Response);
                                            response.Write(id);
                                            handler(message.ReadBytes(len)).Pack(response);
                                            Client.SendMessage(response, NetDeliveryMethod.ReliableOrdered);
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
                            Main.QueueAction(() => {
                                GTA.UI.Notification.Show("~r~~h~Packet Error");
                                return true;
                            });
                            Main.Logger.Error($"[{packetType}] {ex.Message}");
                            Main.Logger.Error(ex);
                            Client.Disconnect($"Packet Error [{packetType}]");
                        }
                        break;
                    }
                case NetIncomingMessageType.ConnectionLatencyUpdated:
                    Latency = message.ReadFloat();
                    break;
                case NetIncomingMessageType.UnconnectedData:
                    {
                        var packetType = (PacketType)message.ReadByte();
                        int len = message.ReadInt32();
                        byte[] data = message.ReadBytes(len);
                        if (packetType==PacketType.PublicKeyResponse)
                        {
                            var packet=new Packets.PublicKeyResponse();
                            packet.Unpack(data);
                            Security.SetServerPublicKey(packet.Modulus,packet.Exponent);
                            PublicKeyReceived.Set();
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
                case PacketType.VehicleStateSync:
                    {

                        Packets.VehicleStateSync packet = new Packets.VehicleStateSync();
                        packet.Unpack(data);
                        VehicleStateSync(packet);

                    }
                    break;
                case PacketType.PedStateSync:
                    {


                        Packets.PedStateSync packet = new Packets.PedStateSync();
                        packet.Unpack(data);
                        PedStateSync(packet);

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

                        Packets.ChatMessage packet = new Packets.ChatMessage();
                        packet.Unpack(data);

                        Main.QueueAction(() => { Main.MainChat.AddMessage(packet.Username, packet.Message); return true; });


                    }
                    break;
                case PacketType.CustomEvent:
                    {
                        Packets.CustomEvent packet = new Packets.CustomEvent();
                        packet.Unpack(data);
                        Scripting.API.Events.InvokeCustomEventReceived(packet);
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
