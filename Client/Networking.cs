using System;
using System.Collections.Generic;

using CoopClient.Entities;

using Lidgren.Network;

using GTA;
using GTA.Native;

namespace CoopClient
{
    /// <summary>
    /// Don't use it!
    /// </summary>
    public class Networking
    {
        internal NetClient Client;
        internal float Latency;

        internal bool ShowNetworkInfo = false;

        internal int BytesReceived = 0;
        internal int BytesSend = 0;

        internal void DisConnectFromServer(string address)
        {
            if (IsOnServer())
            {
                Client.Disconnect("Bye!");
            }
            else
            {
                // 6d4ec318f1c43bd62fe13d5a7ab28650 = GTACOOP:R
                NetPeerConfiguration config = new NetPeerConfiguration("6d4ec318f1c43bd62fe13d5a7ab28650")
                {
                    AutoFlushSendQueue = false
                };

                config.EnableMessageType(NetIncomingMessageType.ConnectionLatencyUpdated);

                Client = new NetClient(config);

                Client.Start();

                string[] ip = new string[2];

                int idx = address.LastIndexOf(':');
                if (idx != -1)
                {
                    ip[0] = address.Substring(0, idx);
                    ip[1] = address.Substring(idx + 1);
                }

                if (ip.Length != 2)
                {
                    throw new Exception("Malformed URL");
                }

                // Send HandshakePacket
                NetOutgoingMessage outgoingMessage = Client.CreateMessage();
                new HandshakePacket()
                {
                    NetHandle =  0,
                    Username = Main.MainSettings.Username,
                    ModVersion = Main.CurrentVersion,
                    NPCsAllowed = false
                }.PacketToNetOutGoingMessage(outgoingMessage);

                Client.Connect(ip[0], short.Parse(ip[1]), outgoingMessage);
            }
        }

        internal bool IsOnServer()
        {
            return Client?.ConnectionStatus == NetConnectionStatus.Connected;
        }

        internal void ReceiveMessages()
        {
            if (Client == null)
            {
                return;
            }

            NetIncomingMessage message;

            while ((message = Client.ReadMessage()) != null)
            {
                BytesReceived += message.LengthBytes;

                switch (message.MessageType)
                {
                    case NetIncomingMessageType.StatusChanged:
                        NetConnectionStatus status = (NetConnectionStatus)message.ReadByte();

                        string reason = message.ReadString();

                        switch (status)
                        {
                            case NetConnectionStatus.InitiatedConnect:
#if !NON_INTERACTIVE
                                Main.MainMenu.InitiateConnectionMenuSetting();
#endif
                                GTA.UI.Notification.Show("~y~Trying to connect...");
                                break;
                            case NetConnectionStatus.Connected:
                                if (message.SenderConnection.RemoteHailMessage.ReadByte() != (byte)PacketTypes.HandshakePacket)
                                {
                                    Client.Disconnect("Wrong packet!");
                                }
                                else
                                {
                                    int len = message.SenderConnection.RemoteHailMessage.ReadInt32();
                                    byte[] data = message.SenderConnection.RemoteHailMessage.ReadBytes(len);

                                    HandshakePacket handshakePacket = new HandshakePacket();
                                    handshakePacket.NetIncomingMessageToPacket(data);

                                    Main.LocalNetHandle = handshakePacket.NetHandle;
                                    Main.NPCsAllowed = handshakePacket.NPCsAllowed;

                                    Main.MainChat.Init();

#if !NON_INTERACTIVE
                                    Main.MainMenu.ConnectedMenuSetting();
#endif

                                    COOPAPI.Connected();
                                    GTA.UI.Notification.Show("~g~Connected!");
                                }
                                break;
                            case NetConnectionStatus.Disconnected:
                                // Reset all values
                                LastPlayerFullSync = 0;

                                Main.NPCsAllowed = false;

                                if (Main.MainChat.Focused)
                                {
                                    Main.MainChat.Focused = false;
                                }

                                Main.CleanUp();

#if !NON_INTERACTIVE
                                Main.MainMenu.DisconnectedMenuSetting();
#endif

                                COOPAPI.Disconnected(reason);
                                GTA.UI.Notification.Show("~r~Disconnected: " + reason);
                                break;
                        }
                        break;
                    case NetIncomingMessageType.Data:
                        byte packetType = message.ReadByte();

                        switch (packetType)
                        {
                            case (byte)PacketTypes.PlayerConnectPacket:
                                {
                                    int len = message.ReadInt32();
                                    byte[] data = message.ReadBytes(len);

                                    PlayerConnectPacket packet = new PlayerConnectPacket();
                                    packet.NetIncomingMessageToPacket(data);

                                    PlayerConnect(packet);
                                }
                                break;
                            case (byte)PacketTypes.PlayerDisconnectPacket:
                                {
                                    int len = message.ReadInt32();
                                    byte[] data = message.ReadBytes(len);

                                    PlayerDisconnectPacket packet = new PlayerDisconnectPacket();
                                    packet.NetIncomingMessageToPacket(data);

                                    PlayerDisconnect(packet);
                                }
                                break;
                            case (byte)PacketTypes.FullSyncPlayerPacket:
                                {
                                    int len = message.ReadInt32();
                                    byte[] data = message.ReadBytes(len);

                                    FullSyncPlayerPacket packet = new FullSyncPlayerPacket();
                                    packet.NetIncomingMessageToPacket(data);

                                    FullSyncPlayer(packet);
                                }
                                break;
                            case (byte)PacketTypes.FullSyncPlayerVehPacket:
                                {
                                    int len = message.ReadInt32();
                                    byte[] data = message.ReadBytes(len);

                                    FullSyncPlayerVehPacket packet = new FullSyncPlayerVehPacket();
                                    packet.NetIncomingMessageToPacket(data);

                                    FullSyncPlayerVeh(packet);
                                }
                                break;
                            case (byte)PacketTypes.LightSyncPlayerPacket:
                                {
                                    int len = message.ReadInt32();
                                    byte[] data = message.ReadBytes(len);

                                    LightSyncPlayerPacket packet = new LightSyncPlayerPacket();
                                    packet.NetIncomingMessageToPacket(data);

                                    LightSyncPlayer(packet);
                                }
                                break;
                            case (byte)PacketTypes.LightSyncPlayerVehPacket:
                                {
                                    int len = message.ReadInt32();
                                    byte[] data = message.ReadBytes(len);

                                    LightSyncPlayerVehPacket packet = new LightSyncPlayerVehPacket();
                                    packet.NetIncomingMessageToPacket(data);

                                    LightSyncPlayerVeh(packet);
                                }
                                break;
                            case (byte)PacketTypes.SuperLightSyncPacket:
                                {
                                    int len = message.ReadInt32();
                                    byte[] data = message.ReadBytes(len);

                                    SuperLightSyncPacket packet = new SuperLightSyncPacket();
                                    packet.NetIncomingMessageToPacket(data);

                                    if (Main.Players.ContainsKey(packet.NetHandle))
                                    {
                                        EntitiesPlayer player = Main.Players[packet.NetHandle];

                                        player.Position = packet.Position.ToVector();
                                        player.Latency = packet.Latency.HasValue ? packet.Latency.Value : 0;
                                    }
                                }
                                break;
                            case (byte)PacketTypes.FullSyncNpcPacket:
                                {
                                    int len = message.ReadInt32();
                                    byte[] data = message.ReadBytes(len);

                                    FullSyncNpcPacket packet = new FullSyncNpcPacket();
                                    packet.NetIncomingMessageToPacket(data);

                                    FullSyncNpc(packet);
                                }
                                break;
                            case (byte)PacketTypes.FullSyncNpcVehPacket:
                                {
                                    int len = message.ReadInt32();
                                    byte[] data = message.ReadBytes(len);

                                    FullSyncNpcVehPacket packet = new FullSyncNpcVehPacket();
                                    packet.NetIncomingMessageToPacket(data);

                                    FullSyncNpcVeh(packet);
                                }
                                break;
                            case (byte)PacketTypes.ChatMessagePacket:
                                {
                                    int len = message.ReadInt32();
                                    byte[] data = message.ReadBytes(len);

                                    ChatMessagePacket packet = new ChatMessagePacket();
                                    packet.NetIncomingMessageToPacket(data);

                                    if (!COOPAPI.ChatMessageReceived(packet.Username, packet.Message))
                                    {
                                        Main.MainChat.AddMessage(packet.Username, packet.Message);
                                    }
                                }
                                break;
                            case (byte)PacketTypes.NativeCallPacket:
                                {
                                    int len = message.ReadInt32();
                                    byte[] data = message.ReadBytes(len);

                                    NativeCallPacket packet = new NativeCallPacket();
                                    packet.NetIncomingMessageToPacket(data);

                                    DecodeNativeCall(packet);
                                }
                                break;
                            case (byte)PacketTypes.NativeResponsePacket:
                                {
                                    try
                                    {
                                        int len = message.ReadInt32();
                                        byte[] data = message.ReadBytes(len);

                                        NativeResponsePacket packet = new NativeResponsePacket();
                                        packet.NetIncomingMessageToPacket(data);

                                        DecodeNativeResponse(packet);
                                    }
                                    catch (Exception ex)
                                    {
                                        GTA.UI.Notification.Show($"{ex.Message}");
                                    }
                                }
                                break;
                            case (byte)PacketTypes.ModPacket:
                                {
                                    int len = message.ReadInt32();
                                    byte[] data = message.ReadBytes(len);

                                    ModPacket packet = new ModPacket();
                                    packet.NetIncomingMessageToPacket(data);

                                    COOPAPI.ModPacketReceived(packet.NetHandle, packet.Mod, packet.CustomPacketID, packet.Bytes);
                                }
                                break;
                        }
                        break;
                    case NetIncomingMessageType.ConnectionLatencyUpdated:
                        Latency = message.ReadFloat();
                        break;
                    case NetIncomingMessageType.DebugMessage:
                    case NetIncomingMessageType.ErrorMessage:
                    case NetIncomingMessageType.WarningMessage:
                    case NetIncomingMessageType.VerboseDebugMessage:
#if DEBUG
                        // TODO?
#endif
                        break;
                    default:
                        break;
                }

                Client.Recycle(message);
            }
        }

        #region -- GET --
        #region -- PLAYER --
        private void PlayerConnect(PlayerConnectPacket packet)
        {
            EntitiesPlayer player = new EntitiesPlayer() { Username = packet.Username };

            Main.Players.Add(packet.NetHandle, player);
            COOPAPI.Connected(packet.NetHandle);
        }

        private void PlayerDisconnect(PlayerDisconnectPacket packet)
        {
            if (Main.Players.ContainsKey(packet.NetHandle))
            {
                EntitiesPlayer player = Main.Players[packet.NetHandle];
                if (player.Character != null && player.Character.Exists())
                {
                    player.Character.Kill();
                    player.Character.Delete();
                }

                player.PedBlip?.Delete();

                COOPAPI.Disconnected(packet.NetHandle);
                Main.Players.Remove(packet.NetHandle);
            }
        }

        private void FullSyncPlayer(FullSyncPlayerPacket packet)
        {
            if (Main.Players.ContainsKey(packet.NetHandle))
            {
                EntitiesPlayer player = Main.Players[packet.NetHandle];

                player.ModelHash = packet.ModelHash;
                player.Clothes = packet.Clothes;
                player.Health = packet.Health;
                player.Position = packet.Position.ToVector();
                player.Rotation = packet.Rotation.ToVector();
                player.Velocity = packet.Velocity.ToVector();
                player.Speed = packet.Speed;
                player.CurrentWeaponHash = packet.CurrentWeaponHash;
                player.WeaponComponents = packet.WeaponComponents;
                player.AimCoords = packet.AimCoords.ToVector();
                player.IsAiming = (packet.Flag.Value & (byte)PedDataFlags.IsAiming) > 0;
                player.IsShooting = (packet.Flag.Value & (byte)PedDataFlags.IsShooting) > 0;
                player.IsReloading = (packet.Flag.Value & (byte)PedDataFlags.IsReloading) > 0;
                player.IsJumping = (packet.Flag.Value & (byte)PedDataFlags.IsJumping) > 0;
                player.IsRagdoll = (packet.Flag.Value & (byte)PedDataFlags.IsRagdoll) > 0;
                player.IsOnFire = (packet.Flag.Value & (byte)PedDataFlags.IsOnFire) > 0;
                player.IsInVehicle = false;
                player.LastSyncWasFull = true;

                player.Latency = packet.Latency.Value;
                player.LastUpdateReceived = Util.GetTickCount64();
            }
        }

        private void FullSyncPlayerVeh(FullSyncPlayerVehPacket packet)
        {
            if (Main.Players.ContainsKey(packet.NetHandle))
            {
                EntitiesPlayer player = Main.Players[packet.NetHandle];

                player.ModelHash = packet.ModelHash;
                player.Clothes = packet.Clothes;
                player.Health = packet.Health;
                player.VehicleModelHash = packet.VehModelHash;
                player.VehicleSeatIndex = packet.VehSeatIndex;
                player.Position = packet.Position.ToVector();
                player.VehicleRotation = packet.VehRotation.ToQuaternion();
                player.VehicleEngineHealth = packet.VehEngineHealth;
                player.VehRPM = packet.VehRPM;
                player.VehicleVelocity = packet.VehVelocity.ToVector();
                player.VehicleSpeed = packet.VehSpeed;
                player.VehicleSteeringAngle = packet.VehSteeringAngle;
                player.AimCoords = packet.VehAimCoords.ToVector();
                player.VehicleColors = packet.VehColors;
                player.VehicleMods = packet.VehMods;
                player.VehDamageModel = packet.VehDamageModel;
                player.VehLandingGear = packet.VehLandingGear;
                player.VehIsEngineRunning = (packet.Flag.Value & (ushort)VehicleDataFlags.IsEngineRunning) > 0;
                player.VehAreLightsOn = (packet.Flag.Value & (ushort)VehicleDataFlags.AreLightsOn) > 0;
                player.VehAreHighBeamsOn = (packet.Flag.Value & (ushort)VehicleDataFlags.AreHighBeamsOn) > 0;
                player.VehIsSireneActive = (packet.Flag.Value & (ushort)VehicleDataFlags.IsSirenActive) > 0;
                player.VehicleDead = (packet.Flag.Value & (ushort)VehicleDataFlags.IsDead) > 0;
                player.IsHornActive = (packet.Flag.Value & (ushort)VehicleDataFlags.IsHornActive) > 0;
                player.Transformed = (packet.Flag.Value & (ushort)VehicleDataFlags.IsTransformed) > 0;
                player.VehRoofOpened = (packet.Flag.Value & (ushort)VehicleDataFlags.RoofOpened) > 0;
                player.IsInVehicle = true;
                player.LastSyncWasFull = true;

                player.Latency = packet.Latency.Value;
                player.LastUpdateReceived = Util.GetTickCount64();
            }
        }

        private void LightSyncPlayer(LightSyncPlayerPacket packet)
        {
            if (Main.Players.ContainsKey(packet.NetHandle))
            {
                EntitiesPlayer player = Main.Players[packet.NetHandle];

                player.Health = packet.Health;
                player.Position = packet.Position.ToVector();
                player.Rotation = packet.Rotation.ToVector();
                player.Velocity = packet.Velocity.ToVector();
                player.Speed = packet.Speed;
                player.CurrentWeaponHash = packet.CurrentWeaponHash;
                player.AimCoords = packet.AimCoords.ToVector();
                player.IsAiming = (packet.Flag.Value & (byte)PedDataFlags.IsAiming) > 0;
                player.IsShooting = (packet.Flag.Value & (byte)PedDataFlags.IsShooting) > 0;
                player.IsReloading = (packet.Flag.Value & (byte)PedDataFlags.IsReloading) > 0;
                player.IsJumping = (packet.Flag.Value & (byte)PedDataFlags.IsJumping) > 0;
                player.IsRagdoll = (packet.Flag.Value & (byte)PedDataFlags.IsRagdoll) > 0;
                player.IsOnFire = (packet.Flag.Value & (byte)PedDataFlags.IsOnFire) > 0;
                player.IsInVehicle = false;
                player.LastSyncWasFull = false;

                if (packet.Flag.HasValue)
                {
                    player.Latency = packet.Latency.Value;
                }
                player.LastUpdateReceived = Util.GetTickCount64();
            }
        }

        private void LightSyncPlayerVeh(LightSyncPlayerVehPacket packet)
        {
            if (Main.Players.ContainsKey(packet.NetHandle))
            {
                EntitiesPlayer player = Main.Players[packet.NetHandle];

                player.Health = packet.Health;
                player.VehicleModelHash = packet.VehModelHash;
                player.VehicleSeatIndex = packet.VehSeatIndex;
                player.Position = packet.Position.ToVector();
                player.VehicleRotation = packet.VehRotation.ToQuaternion();
                player.VehicleVelocity = packet.VehVelocity.ToVector();
                player.VehicleSpeed = packet.VehSpeed;
                player.VehicleSteeringAngle = packet.VehSteeringAngle;
                player.VehIsEngineRunning = (packet.Flag.Value & (ushort)VehicleDataFlags.IsEngineRunning) > 0;
                player.VehAreLightsOn = (packet.Flag.Value & (ushort)VehicleDataFlags.AreLightsOn) > 0;
                player.VehAreHighBeamsOn = (packet.Flag.Value & (ushort)VehicleDataFlags.AreHighBeamsOn) > 0;
                player.VehIsSireneActive = (packet.Flag.Value & (ushort)VehicleDataFlags.IsSirenActive) > 0;
                player.VehicleDead = (packet.Flag.Value & (ushort)VehicleDataFlags.IsDead) > 0;
                player.IsHornActive = (packet.Flag.Value & (ushort)VehicleDataFlags.IsHornActive) > 0;
                player.Transformed = (packet.Flag.Value & (ushort)VehicleDataFlags.IsTransformed) > 0;
                player.VehRoofOpened = (packet.Flag.Value & (ushort)VehicleDataFlags.RoofOpened) > 0;
                player.IsInVehicle = true;
                player.LastSyncWasFull = false;

                player.Latency = packet.Latency.Value;
                player.LastUpdateReceived = Util.GetTickCount64();
            }
        }

        private void DecodeNativeCall(NativeCallPacket packet)
        {
            List<InputArgument> arguments = new List<InputArgument>();

            if (packet.Args != null && packet.Args.Count > 0)
            {
                packet.Args.ForEach(x =>
                {
                    Type type = x.GetType();

                    if (type == typeof(int))
                    {
                        arguments.Add((int)x);
                    }
                    else if (type == typeof(bool))
                    {
                        arguments.Add((bool)x);
                    }
                    else if (type == typeof(float))
                    {
                        arguments.Add((float)x);
                    }
                    else if (type == typeof(string))
                    {
                        arguments.Add((string)x);
                    }
                    else if (type == typeof(LVector3))
                    {
                        LVector3 vector = (LVector3)x;
                        arguments.Add((float)vector.X);
                        arguments.Add((float)vector.Y);
                        arguments.Add((float)vector.Z);
                    }
                    else
                    {
                        GTA.UI.Notification.Show("[DecodeNativeCall][" + packet.Hash + "]: Type of argument not found!");
                        return;
                    }
                });
            }

            Function.Call((Hash)packet.Hash, arguments.ToArray());
        }

        private void DecodeNativeResponse(NativeResponsePacket packet)
        {
            List<InputArgument> arguments = new List<InputArgument>();
            Type typeOf = null;

            if (packet.Args != null && packet.Args.Count > 0)
            {
                packet.Args.ForEach(x =>
                {
                    typeOf = x.GetType();

                    if (typeOf == typeof(int))
                    {
                        arguments.Add((int)x);
                    }
                    else if (typeOf == typeof(bool))
                    {
                        arguments.Add((bool)x);
                    }
                    else if (typeOf == typeof(float))
                    {
                        arguments.Add((float)x);
                    }
                    else if (typeOf == typeof(string))
                    {
                        arguments.Add((string)x);
                    }
                    else if (typeOf == typeof(LVector3))
                    {
                        LVector3 vector = (LVector3)x;
                        arguments.Add((float)vector.X);
                        arguments.Add((float)vector.Y);
                        arguments.Add((float)vector.Z);
                    }
                    else
                    {
                        GTA.UI.Notification.Show("[DecodeNativeResponse][" + packet.Hash + "]: Type of argument not found!");
                        return;
                    }
                });
            }

            object result;
            switch (packet.ResultType.Value)
            {
                case 0x00: // int
                    result = Function.Call<int>((Hash)packet.Hash, arguments.ToArray());
                    break;
                case 0x01: // bool
                    result = Function.Call<bool>((Hash)packet.Hash, arguments.ToArray());
                    break;
                case 0x02: // float
                    result = Function.Call<float>((Hash)packet.Hash, arguments.ToArray());
                    break;
                case 0x03: // string
                    result = Function.Call<string>((Hash)packet.Hash, arguments.ToArray());
                    break;
                case 0x04: // vector3
                    result = Function.Call<GTA.Math.Vector3>((Hash)packet.Hash, arguments.ToArray()).ToLVector();
                    break;
                default:
                    GTA.UI.Notification.Show("[DecodeNativeResponse][" + packet.Hash + "]: Type of return not found!");
                    return;
            }

            NetOutgoingMessage outgoingMessage = Client.CreateMessage();
            new NativeResponsePacket()
            {
                Hash = 0,
                Args = new List<object>() { result },
                ID =  packet.ID
            }.PacketToNetOutGoingMessage(outgoingMessage);
            Client.SendMessage(outgoingMessage, NetDeliveryMethod.ReliableOrdered, (byte)ConnectionChannel.Native);
            Client.FlushSendQueue();
        }
        #endregion // -- PLAYER --

        #region -- NPC --
        private void FullSyncNpc(FullSyncNpcPacket packet)
        {
            lock (Main.NPCs)
            {
                if (Main.NPCs.ContainsKey(packet.NetHandle))
                {
                    EntitiesPed npc = Main.NPCs[packet.NetHandle];

                    // "if" this NPC has left a vehicle
                    npc.NPCVehHandle = 0;

                    npc.ModelHash = packet.ModelHash;
                    npc.Clothes = packet.Clothes;
                    npc.Health = packet.Health;
                    npc.Position = packet.Position.ToVector();
                    npc.Rotation = packet.Rotation.ToVector();
                    npc.Velocity = packet.Velocity.ToVector();
                    npc.Speed = packet.Speed;
                    npc.CurrentWeaponHash = packet.CurrentWeaponHash;
                    npc.AimCoords = packet.AimCoords.ToVector();
                    npc.IsAiming = (packet.Flag.Value & (byte)PedDataFlags.IsAiming) > 0;
                    npc.IsShooting = (packet.Flag.Value & (byte)PedDataFlags.IsShooting) > 0;
                    npc.IsReloading = (packet.Flag.Value & (byte)PedDataFlags.IsReloading) > 0;
                    npc.IsJumping = (packet.Flag.Value & (byte)PedDataFlags.IsJumping) > 0;
                    npc.IsRagdoll = (packet.Flag.Value & (byte)PedDataFlags.IsRagdoll) > 0;
                    npc.IsOnFire = (packet.Flag.Value & (byte)PedDataFlags.IsOnFire) > 0;
                    npc.IsInVehicle = false;
                    npc.LastSyncWasFull = true;

                    npc.LastUpdateReceived = Util.GetTickCount64();
                }
                else
                {
                    Main.NPCs.Add(packet.NetHandle, new EntitiesPed()
                    {
                        ModelHash = packet.ModelHash,
                        Clothes = packet.Clothes,
                        Health = packet.Health,
                        Position = packet.Position.ToVector(),
                        Rotation = packet.Rotation.ToVector(),
                        Velocity = packet.Velocity.ToVector(),
                        Speed = packet.Speed,
                        CurrentWeaponHash = packet.CurrentWeaponHash,
                        AimCoords = packet.AimCoords.ToVector(),
                        IsAiming = (packet.Flag.Value & (byte)PedDataFlags.IsAiming) > 0,
                        IsShooting = (packet.Flag.Value & (byte)PedDataFlags.IsShooting) > 0,
                        IsReloading = (packet.Flag.Value & (byte)PedDataFlags.IsReloading) > 0,
                        IsJumping = (packet.Flag.Value & (byte)PedDataFlags.IsJumping) > 0,
                        IsRagdoll = (packet.Flag.Value & (byte)PedDataFlags.IsRagdoll) > 0,
                        IsOnFire = (packet.Flag.Value & (byte)PedDataFlags.IsOnFire) > 0,
                        IsInVehicle = false,
                        LastSyncWasFull = true,

                        LastUpdateReceived = Util.GetTickCount64()
                    });
                }
            }
        }

        private void FullSyncNpcVeh(FullSyncNpcVehPacket packet)
        {
            lock (Main.NPCs)
            {
                if (Main.NPCs.ContainsKey(packet.NetHandle))
                {
                    EntitiesPed npc = Main.NPCs[packet.NetHandle];

                    npc.NPCVehHandle = packet.VehHandle;

                    npc.ModelHash = packet.ModelHash;
                    npc.Clothes = packet.Clothes;
                    npc.Health = packet.Health;
                    npc.VehicleModelHash = packet.VehModelHash;
                    npc.VehicleSeatIndex = packet.VehSeatIndex;
                    npc.Position = packet.Position.ToVector();
                    npc.VehicleRotation = packet.VehRotation.ToQuaternion();
                    npc.VehicleEngineHealth = packet.VehEngineHealth;
                    npc.VehRPM = packet.VehRPM;
                    npc.VehicleVelocity = packet.VehVelocity.ToVector();
                    npc.VehicleSpeed = packet.VehSpeed;
                    npc.VehicleSteeringAngle = packet.VehSteeringAngle;
                    npc.VehicleColors = packet.VehColors;
                    npc.VehDamageModel = packet.VehDamageModel;
                    npc.VehLandingGear = packet.VehLandingGear;
                    npc.VehIsEngineRunning = (packet.Flag.Value & (ushort)VehicleDataFlags.IsEngineRunning) > 0;
                    npc.VehAreLightsOn = (packet.Flag.Value & (ushort)VehicleDataFlags.AreLightsOn) > 0;
                    npc.VehAreHighBeamsOn = (packet.Flag.Value & (ushort)VehicleDataFlags.AreHighBeamsOn) > 0;
                    npc.VehIsSireneActive = (packet.Flag.Value & (ushort)VehicleDataFlags.IsSirenActive) > 0;
                    npc.VehicleDead = (packet.Flag.Value & (ushort)VehicleDataFlags.IsDead) > 0;
                    npc.IsHornActive = (packet.Flag.Value & (ushort)VehicleDataFlags.IsHornActive) > 0;
                    npc.Transformed = (packet.Flag.Value & (ushort)VehicleDataFlags.IsTransformed) > 0;
                    npc.VehRoofOpened = (packet.Flag.Value & (ushort)VehicleDataFlags.RoofOpened) > 0;
                    npc.IsInVehicle = true;
                    npc.LastSyncWasFull = true;

                    npc.LastUpdateReceived = Util.GetTickCount64();
                }
                else
                {
                    Main.NPCs.Add(packet.NetHandle, new EntitiesPed()
                    {
                        NPCVehHandle = packet.VehHandle,

                        ModelHash = packet.ModelHash,
                        Clothes = packet.Clothes,
                        Health = packet.Health,
                        VehicleModelHash = packet.VehModelHash,
                        VehicleSeatIndex = packet.VehSeatIndex,
                        Position = packet.Position.ToVector(),
                        VehicleRotation = packet.VehRotation.ToQuaternion(),
                        VehicleEngineHealth = packet.VehEngineHealth,
                        VehRPM = packet.VehRPM,
                        VehicleVelocity = packet.VehVelocity.ToVector(),
                        VehicleSpeed = packet.VehSpeed,
                        VehicleSteeringAngle = packet.VehSteeringAngle,
                        VehicleColors = packet.VehColors,
                        VehDamageModel = packet.VehDamageModel,
                        VehLandingGear = packet.VehLandingGear,
                        VehIsEngineRunning = (packet.Flag.Value & (ushort)VehicleDataFlags.IsEngineRunning) > 0,
                        VehAreLightsOn = (packet.Flag.Value & (ushort)VehicleDataFlags.AreLightsOn) > 0,
                        VehAreHighBeamsOn = (packet.Flag.Value & (ushort)VehicleDataFlags.AreHighBeamsOn) > 0,
                        VehIsSireneActive = (packet.Flag.Value & (ushort)VehicleDataFlags.IsSirenActive) > 0,
                        VehicleDead = (packet.Flag.Value & (ushort)VehicleDataFlags.IsDead) > 0,
                        IsHornActive = (packet.Flag.Value & (ushort)VehicleDataFlags.IsHornActive) > 0,
                        Transformed = (packet.Flag.Value & (ushort)VehicleDataFlags.IsTransformed) > 0,
                        VehRoofOpened = (packet.Flag.Value & (ushort)VehicleDataFlags.RoofOpened) > 0,
                        IsInVehicle = true,
                        LastSyncWasFull = true,

                        LastUpdateReceived = Util.GetTickCount64()
                    });
                }
            }
        }
        #endregion // -- NPC --
        #endregion

        #region -- SEND --
        private ulong LastPlayerFullSync = 0;
        internal void SendPlayerData()
        {
            Ped player = Game.Player.Character;

            NetOutgoingMessage outgoingMessage = Client.CreateMessage();
            NetDeliveryMethod messageType;
            int connectionChannel = 0;

            if ((Util.GetTickCount64() - LastPlayerFullSync) > 500)
            {
                messageType = NetDeliveryMethod.UnreliableSequenced;
                connectionChannel = (byte)ConnectionChannel.PlayerFull;

                if (player.IsInVehicle())
                {
                    Vehicle veh = player.CurrentVehicle;

                    byte primaryColor = 0;
                    byte secondaryColor = 0;
                    unsafe
                    {
                        Function.Call<byte>(Hash.GET_VEHICLE_COLOURS, veh, &primaryColor, &secondaryColor);
                    }

                    new FullSyncPlayerVehPacket()
                    {
                        NetHandle = Main.LocalNetHandle,
                        Health = player.Health,
                        ModelHash = player.Model.Hash,
                        Clothes = player.GetPedClothes(),
                        VehModelHash = veh.Model.Hash,
                        VehSeatIndex = (short)player.SeatIndex,
                        Position = veh.Position.ToLVector(),
                        VehRotation = veh.Quaternion.ToLQuaternion(),
                        VehEngineHealth = veh.EngineHealth,
                        VehRPM = veh.CurrentRPM,
                        VehVelocity = veh.Velocity.ToLVector(),
                        VehSpeed = veh.Speed,
                        VehSteeringAngle = veh.SteeringAngle,
                        VehAimCoords = veh.IsTurretSeat((int)player.SeatIndex) ? Util.GetVehicleAimCoords().ToLVector() : new LVector3(),
                        VehColors = new byte[] { primaryColor, secondaryColor },
                        VehMods = veh.Mods.GetVehicleMods(),
                        VehDamageModel = veh.GetVehicleDamageModel(),
                        VehLandingGear = veh.IsPlane ? (byte)veh.LandingGearState : (byte)0,
                        Flag = player.GetVehicleFlags(veh)
                    }.PacketToNetOutGoingMessage(outgoingMessage);
                }
                else
                {
                    new FullSyncPlayerPacket()
                    {
                        NetHandle = Main.LocalNetHandle,
                        Health = player.Health,
                        ModelHash = player.Model.Hash,
                        Clothes = player.GetPedClothes(),
                        Position = player.Position.ToLVector(),
                        Rotation = player.Rotation.ToLVector(),
                        Velocity = player.Velocity.ToLVector(),
                        Speed = player.GetPedSpeed(),
                        AimCoords = player.GetPedAimCoords(false).ToLVector(),
                        CurrentWeaponHash = (uint)player.Weapons.Current.Hash,
                        WeaponComponents = player.Weapons.Current.GetWeaponComponents(),
                        Flag = player.GetPedFlags(true)
                    }.PacketToNetOutGoingMessage(outgoingMessage);
                }

                LastPlayerFullSync = Util.GetTickCount64();
            }
            else
            {
                messageType = NetDeliveryMethod.ReliableSequenced;
                connectionChannel = (byte)ConnectionChannel.PlayerLight;

                if (player.IsInVehicle())
                {
                    Vehicle veh = player.CurrentVehicle;

                    new LightSyncPlayerVehPacket()
                    {
                        NetHandle = Main.LocalNetHandle,
                        Health = player.Health,
                        VehModelHash = veh.Model.Hash,
                        VehSeatIndex = (short)player.SeatIndex,
                        Position = veh.Position.ToLVector(),
                        VehRotation = veh.Quaternion.ToLQuaternion(),
                        VehVelocity = veh.Velocity.ToLVector(),
                        VehSpeed = veh.Speed,
                        VehSteeringAngle = veh.SteeringAngle,
                        Flag = player.GetVehicleFlags(veh)
                    }.PacketToNetOutGoingMessage(outgoingMessage);
                }
                else
                {
                    new LightSyncPlayerPacket()
                    {
                        NetHandle = Main.LocalNetHandle,
                        Health = player.Health,
                        Position = player.Position.ToLVector(),
                        Rotation = player.Rotation.ToLVector(),
                        Velocity = player.Velocity.ToLVector(),
                        Speed = player.GetPedSpeed(),
                        AimCoords = player.GetPedAimCoords(false).ToLVector(),
                        CurrentWeaponHash = (uint)player.Weapons.Current.Hash,
                        Flag = player.GetPedFlags(true)
                    }.PacketToNetOutGoingMessage(outgoingMessage);
                }
            }

            Client.SendMessage(outgoingMessage, messageType, connectionChannel);
            Client.FlushSendQueue();

            #if DEBUG
            if (ShowNetworkInfo)
            {
                BytesSend += outgoingMessage.LengthBytes;
            }
            #endif
        }

        internal void SendNpcData(Ped npc)
        {
            NetOutgoingMessage outgoingMessage = Client.CreateMessage();

            if (npc.IsInVehicle())
            {
                Vehicle veh = npc.CurrentVehicle;

                byte primaryColor = 0;
                byte secondaryColor = 0;
                unsafe
                {
                    Function.Call<byte>(Hash.GET_VEHICLE_COLOURS, npc.CurrentVehicle, &primaryColor, &secondaryColor);
                }

                new FullSyncNpcVehPacket()
                {
                    NetHandle =  Main.LocalNetHandle + npc.Handle,
                    VehHandle = Main.LocalNetHandle + veh.Handle,
                    ModelHash = npc.Model.Hash,
                    Clothes = npc.GetPedClothes(),
                    Health = npc.Health,
                    VehModelHash = veh.Model.Hash,
                    VehSeatIndex = (short)npc.SeatIndex,
                    Position = veh.Position.ToLVector(),
                    VehRotation = veh.Quaternion.ToLQuaternion(),
                    VehEngineHealth = veh.EngineHealth,
                    VehRPM = veh.CurrentRPM,
                    VehVelocity = veh.Velocity.ToLVector(),
                    VehSpeed = veh.Speed,
                    VehSteeringAngle = veh.SteeringAngle,
                    VehColors = new byte[] { primaryColor, secondaryColor },
                    VehMods = veh.Mods.GetVehicleMods(),
                    VehDamageModel = veh.GetVehicleDamageModel(),
                    VehLandingGear = veh.IsPlane ? (byte)veh.LandingGearState : (byte)0,
                    Flag = npc.GetVehicleFlags(veh)
                }.PacketToNetOutGoingMessage(outgoingMessage);
            }
            else
            {
                new FullSyncNpcPacket()
                {
                    NetHandle =  Main.LocalNetHandle + npc.Handle,
                    ModelHash = npc.Model.Hash,
                    Clothes = npc.GetPedClothes(),
                    Health = npc.Health,
                    Position = npc.Position.ToLVector(),
                    Rotation = npc.Rotation.ToLVector(),
                    Velocity = npc.Velocity.ToLVector(),
                    Speed = npc.GetPedSpeed(),
                    AimCoords = npc.GetPedAimCoords(true).ToLVector(),
                    CurrentWeaponHash = (uint)npc.Weapons.Current.Hash,
                    Flag = npc.GetPedFlags(true)
                }.PacketToNetOutGoingMessage(outgoingMessage);
            }

            Client.SendMessage(outgoingMessage, NetDeliveryMethod.Unreliable, (byte)ConnectionChannel.NPCFull);
            Client.FlushSendQueue();

            #if DEBUG
            if (ShowNetworkInfo)
            {
                BytesSend += outgoingMessage.LengthBytes;
            }
            #endif
        }

        internal void SendChatMessage(string message)
        {
            NetOutgoingMessage outgoingMessage = Client.CreateMessage();

            new ChatMessagePacket() { Username = Main.MainSettings.Username, Message = message }.PacketToNetOutGoingMessage(outgoingMessage);

            Client.SendMessage(outgoingMessage, NetDeliveryMethod.ReliableOrdered, (byte)ConnectionChannel.Chat);
            Client.FlushSendQueue();

            #if DEBUG
            if (ShowNetworkInfo)
            {
                BytesSend += outgoingMessage.LengthBytes;
            }
            #endif
        }

        internal void SendModData(long target, string mod, byte customID, byte[] bytes)
        {
            NetOutgoingMessage outgoingMessage = Client.CreateMessage();
            new ModPacket()
            {
                NetHandle =  Main.LocalNetHandle,
                Target = target,
                Mod = mod,
                CustomPacketID =  customID,
                Bytes = bytes
            }.PacketToNetOutGoingMessage(outgoingMessage);
            Client.SendMessage(outgoingMessage, NetDeliveryMethod.ReliableOrdered, (byte)ConnectionChannel.Mod);
            Client.FlushSendQueue();

            #if DEBUG
            if (ShowNetworkInfo)
            {
                BytesSend += outgoingMessage.LengthBytes;
            }
            #endif
        }
        #endregion
    }
}
