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
                    ID = 0,
                    SocialClubName = Game.Player.Name,
                    Username = Main.MainSettings.Username,
                    ModVersion = Main.CurrentVersion,
                    NpcsAllowed = false
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
                                    Packet remoteHailMessagePacket;
                                    remoteHailMessagePacket = new HandshakePacket();
                                    remoteHailMessagePacket.NetIncomingMessageToPacket(message.SenderConnection.RemoteHailMessage);

                                    HandshakePacket handshakePacket = (HandshakePacket)remoteHailMessagePacket;
                                    Main.LocalClientID = handshakePacket.ID;
                                    Main.NpcsAllowed = handshakePacket.NpcsAllowed;

                                    Main.MainChat.Init();

                                    // Send player connect packet
                                    NetOutgoingMessage outgoingMessage = Client.CreateMessage();
                                    new PlayerConnectPacket()
                                    {
                                        ID = Main.LocalClientID,
                                        SocialClubName = string.Empty,
                                        Username = string.Empty
                                    }.PacketToNetOutGoingMessage(outgoingMessage);
                                    Client.SendMessage(outgoingMessage, NetDeliveryMethod.ReliableOrdered);
                                    Client.FlushSendQueue();

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

                                Main.NpcsAllowed = false;

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

                        Packet packet;

                        switch (packetType)
                        {
                            case (byte)PacketTypes.PlayerConnectPacket:
                                packet = new PlayerConnectPacket();
                                packet.NetIncomingMessageToPacket(message);
                                PlayerConnect((PlayerConnectPacket)packet);
                                break;
                            case (byte)PacketTypes.PlayerDisconnectPacket:
                                packet = new PlayerDisconnectPacket();
                                packet.NetIncomingMessageToPacket(message);
                                PlayerDisconnect((PlayerDisconnectPacket)packet);
                                break;
                            case (byte)PacketTypes.FullSyncPlayerPacket:
                                packet = new FullSyncPlayerPacket();
                                packet.NetIncomingMessageToPacket(message);
                                FullSyncPlayer((FullSyncPlayerPacket)packet);
                                break;
                            case (byte)PacketTypes.FullSyncPlayerVehPacket:
                                packet = new FullSyncPlayerVehPacket();
                                packet.NetIncomingMessageToPacket(message);
                                FullSyncPlayerVeh((FullSyncPlayerVehPacket)packet);
                                break;
                            case (byte)PacketTypes.LightSyncPlayerPacket:
                                packet = new LightSyncPlayerPacket();
                                packet.NetIncomingMessageToPacket(message);
                                LightSyncPlayer((LightSyncPlayerPacket)packet);
                                break;
                            case (byte)PacketTypes.LightSyncPlayerVehPacket:
                                packet = new LightSyncPlayerVehPacket();
                                packet.NetIncomingMessageToPacket(message);
                                LightSyncPlayerVeh((LightSyncPlayerVehPacket)packet);
                                break;
                            case (byte)PacketTypes.FullSyncNpcPacket:
                                packet = new FullSyncNpcPacket();
                                packet.NetIncomingMessageToPacket(message);
                                FullSyncNpc((FullSyncNpcPacket)packet);
                                break;
                            case (byte)PacketTypes.FullSyncNpcVehPacket:
                                packet = new FullSyncNpcVehPacket();
                                packet.NetIncomingMessageToPacket(message);
                                FullSyncNpcVeh((FullSyncNpcVehPacket)packet);
                                break;
                            case (byte)PacketTypes.SuperLightSyncPlayerPacket:
                                packet = new SuperLightSyncPlayerPacket();
                                packet.NetIncomingMessageToPacket(message);
                                SuperLightSyncPlayer((SuperLightSyncPlayerPacket)packet);
                                break;
                            case (byte)PacketTypes.ChatMessagePacket:
                                packet = new ChatMessagePacket();
                                packet.NetIncomingMessageToPacket(message);

                                ChatMessagePacket chatMessagePacket = (ChatMessagePacket)packet;
                                if (!COOPAPI.ChatMessageReceived(chatMessagePacket.Username, chatMessagePacket.Message))
                                {
                                    Main.MainChat.AddMessage(chatMessagePacket.Username, chatMessagePacket.Message);
                                }
                                break;
                            case (byte)PacketTypes.NativeCallPacket:
                                packet = new NativeCallPacket();
                                packet.NetIncomingMessageToPacket(message);
                                DecodeNativeCall((NativeCallPacket)packet);
                                break;
                            case (byte)PacketTypes.NativeResponsePacket:
                                packet = new NativeResponsePacket();
                                packet.NetIncomingMessageToPacket(message);
                                DecodeNativeResponse((NativeResponsePacket)packet);
                                break;
                            case (byte)PacketTypes.ModPacket:
                                packet = new ModPacket();
                                packet.NetIncomingMessageToPacket(message);
                                ModPacket modPacket = (ModPacket)packet;
                                COOPAPI.ModPacketReceived(modPacket.ID, modPacket.Mod, modPacket.CustomPacketID, modPacket.Bytes);
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
            EntitiesPlayer player = new EntitiesPlayer()
            {
                SocialClubName = packet.SocialClubName,
                Username = packet.Username,
                LastUpdateReceived = Util.GetTickCount64()
            };

            Main.Players.Add(packet.ID, player);
            COOPAPI.Connected(packet.ID);
        }

        private void PlayerDisconnect(PlayerDisconnectPacket packet)
        {
            if (Main.Players.ContainsKey(packet.ID))
            {
                EntitiesPlayer player = Main.Players[packet.ID];
                if (player.Character != null && player.Character.Exists())
                {
                    player.Character.Kill();
                    player.Character.Delete();
                }

                player.PedBlip?.Delete();

                COOPAPI.Disconnected(packet.ID);
                Main.Players.Remove(packet.ID);
            }
        }

        private void FullSyncPlayer(FullSyncPlayerPacket packet)
        {
            if (Main.Players.ContainsKey(packet.Extra.ID))
            {
                EntitiesPlayer player = Main.Players[packet.Extra.ID];

                player.ModelHash = packet.ModelHash;
                player.Props = packet.Props;
                player.Health = packet.Extra.Health;
                player.Position = packet.Extra.Position.ToVector();
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

                player.Latency = packet.Extra.Latency;
                player.LastUpdateReceived = Util.GetTickCount64();
            }
        }

        private void FullSyncPlayerVeh(FullSyncPlayerVehPacket packet)
        {
            if (Main.Players.ContainsKey(packet.Extra.ID))
            {
                EntitiesPlayer player = Main.Players[packet.Extra.ID];

                player.ModelHash = packet.ModelHash;
                player.Props = packet.Props;
                player.Health = packet.Extra.Health;
                player.Position = packet.Extra.Position.ToVector();
                player.VehicleModelHash = packet.VehModelHash;
                player.VehicleSeatIndex = packet.VehSeatIndex;
                player.VehiclePosition = packet.VehPosition.ToVector();
                player.VehicleRotation = packet.VehRotation.ToQuaternion();
                player.VehicleEngineHealth = packet.VehEngineHealth;
                player.VehRPM = packet.VehRPM;
                player.VehicleVelocity = packet.VehVelocity.ToVector();
                player.VehicleSpeed = packet.VehSpeed;
                player.VehicleSteeringAngle = packet.VehSteeringAngle;
                player.AimCoords = packet.VehAimCoords.ToVector();
                player.VehicleColors = packet.VehColors;
                player.VehicleMods = packet.VehMods;
                player.VehDoors = packet.VehDoors;
                player.VehTires = packet.VehTires;
                player.VehIsEngineRunning = (packet.Flag.Value & (byte)VehicleDataFlags.IsEngineRunning) > 0;
                player.VehAreLightsOn = (packet.Flag.Value & (byte)VehicleDataFlags.AreLightsOn) > 0;
                player.VehAreHighBeamsOn = (packet.Flag.Value & (byte)VehicleDataFlags.AreHighBeamsOn) > 0;
                player.VehIsSireneActive = (packet.Flag.Value & (byte)VehicleDataFlags.IsSirenActive) > 0;
                player.VehicleDead = (packet.Flag.Value & (byte)VehicleDataFlags.IsDead) > 0;
                player.IsHornActive = (packet.Flag.Value & (byte)VehicleDataFlags.IsHornActive) > 0;
                player.Transformed = (packet.Flag.Value & (byte)VehicleDataFlags.IsTransformed) > 0;
                player.IsInVehicle = true;
                player.LastSyncWasFull = false;

                player.Latency = packet.Extra.Latency;
                player.LastUpdateReceived = Util.GetTickCount64();
            }
        }

        private void LightSyncPlayer(LightSyncPlayerPacket packet)
        {
            if (Main.Players.ContainsKey(packet.Extra.ID))
            {
                EntitiesPlayer player = Main.Players[packet.Extra.ID];

                player.Health = packet.Extra.Health;
                player.Position = packet.Extra.Position.ToVector();
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

                player.Latency = packet.Extra.Latency;
                player.LastUpdateReceived = Util.GetTickCount64();
            }
        }

        private void LightSyncPlayerVeh(LightSyncPlayerVehPacket packet)
        {
            if (Main.Players.ContainsKey(packet.Extra.ID))
            {
                EntitiesPlayer player = Main.Players[packet.Extra.ID];

                player.Health = packet.Extra.Health;
                player.Position = packet.Extra.Position.ToVector();
                player.VehicleModelHash = packet.VehModelHash;
                player.VehicleSeatIndex = packet.VehSeatIndex;
                player.VehiclePosition = packet.VehPosition.ToVector();
                player.VehicleRotation = packet.VehRotation.ToQuaternion();
                player.VehicleVelocity = packet.VehVelocity.ToVector();
                player.VehicleSpeed = packet.VehSpeed;
                player.VehicleSteeringAngle = packet.VehSteeringAngle;
                player.VehIsEngineRunning = (packet.Flag.Value & (byte)VehicleDataFlags.IsEngineRunning) > 0;
                player.VehAreLightsOn = (packet.Flag.Value & (byte)VehicleDataFlags.AreLightsOn) > 0;
                player.VehAreHighBeamsOn = (packet.Flag.Value & (byte)VehicleDataFlags.AreHighBeamsOn) > 0;
                player.VehIsSireneActive = (packet.Flag.Value & (byte)VehicleDataFlags.IsSirenActive) > 0;
                player.VehicleDead = (packet.Flag.Value & (byte)VehicleDataFlags.IsDead) > 0;
                player.IsHornActive = (packet.Flag.Value & (byte)VehicleDataFlags.IsHornActive) > 0;
                player.Transformed = (packet.Flag.Value & (byte)VehicleDataFlags.IsTransformed) > 0;
                player.IsInVehicle = true;
                player.LastSyncWasFull = false;

                player.Latency = packet.Extra.Latency;
                player.LastUpdateReceived = Util.GetTickCount64();
            }
        }

        private void SuperLightSyncPlayer(SuperLightSyncPlayerPacket packet)
        {
            if (Main.Players.ContainsKey(packet.Extra.ID))
            {
                EntitiesPlayer player = Main.Players[packet.Extra.ID];

                player.Health = packet.Extra.Health;
                player.Position = packet.Extra.Position.ToVector();

                player.Latency = packet.Extra.Latency;
                player.LastUpdateReceived = Util.GetTickCount64();
            }
        }

        private void DecodeNativeCall(NativeCallPacket packet)
        {
            List<InputArgument> arguments = new List<InputArgument>();

            if (packet.Args != null && packet.Args.Count > 0)
            {
                packet.Args.ForEach(arg =>
                {
                    Type typeOf = arg.GetType();

                    if (typeOf == typeof(IntArgument))
                    {
                        arguments.Add(((IntArgument)arg).Data);
                    }
                    else if (typeOf == typeof(BoolArgument))
                    {
                        arguments.Add(((BoolArgument)arg).Data);
                    }
                    else if (typeOf == typeof(FloatArgument))
                    {
                        arguments.Add(((FloatArgument)arg).Data);
                    }
                    else if (typeOf == typeof(StringArgument))
                    {
                        arguments.Add(((StringArgument)arg).Data);
                    }
                    else if (typeOf == typeof(LVector3Argument))
                    {
                        arguments.Add(((LVector3Argument)arg).Data.X);
                        arguments.Add(((LVector3Argument)arg).Data.Y);
                        arguments.Add(((LVector3Argument)arg).Data.Z);
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
                packet.Args.ForEach(arg =>
                {
                    typeOf = arg.GetType();

                    if (typeOf == typeof(IntArgument))
                    {
                        arguments.Add(((IntArgument)arg).Data);
                    }
                    else if (typeOf == typeof(BoolArgument))
                    {
                        arguments.Add(((BoolArgument)arg).Data);
                    }
                    else if (typeOf == typeof(FloatArgument))
                    {
                        arguments.Add(((FloatArgument)arg).Data);
                    }
                    else if (typeOf == typeof(StringArgument))
                    {
                        arguments.Add(((StringArgument)arg).Data);
                    }
                    else if (typeOf == typeof(LVector3Argument))
                    {
                        arguments.Add(((LVector3Argument)arg).Data.X);
                        arguments.Add(((LVector3Argument)arg).Data.Y);
                        arguments.Add(((LVector3Argument)arg).Data.Z);
                    }
                    else
                    {
                        GTA.UI.Notification.Show("[DecodeNativeCall][" + packet.Hash + "]: Type of argument not found!");
                        return;
                    }
                });
            }

            NativeArgument result = null;

            typeOf = packet.Type.GetType();
            if (typeOf == typeof(IntArgument))
            {
                result = new IntArgument() { Data = Function.Call<int>((Hash)packet.Hash, arguments.ToArray()) };
            }
            else if (typeOf == typeof(BoolArgument))
            {
                result = new BoolArgument() { Data = Function.Call<bool>((Hash)packet.Hash, arguments.ToArray()) };
            }
            else if (typeOf == typeof(FloatArgument))
            {
                result = new FloatArgument() { Data = Function.Call<float>((Hash)packet.Hash, arguments.ToArray()) };
            }
            else if (typeOf == typeof(StringArgument))
            {
                result = new StringArgument() { Data = Function.Call<string>((Hash)packet.Hash, arguments.ToArray()) };
            }
            else if (typeOf == typeof(LVector3Argument))
            {
                result = new LVector3Argument() { Data = Function.Call<GTA.Math.Vector3>((Hash)packet.Hash, arguments.ToArray()).ToLVector() };
            }
            else
            {
                GTA.UI.Notification.Show("[DecodeNativeResponse][" + packet.Hash + "]: Type of argument not found!");
                return;
            }

            NetOutgoingMessage outgoingMessage = Client.CreateMessage();
            new NativeResponsePacket()
            {
                Hash = 0,
                Args = null,
                Type = result,
                ID = packet.ID
            }.PacketToNetOutGoingMessage(outgoingMessage);
            Client.SendMessage(outgoingMessage, NetDeliveryMethod.ReliableOrdered, (int)ConnectionChannel.Native);
            Client.FlushSendQueue();
        }
        #endregion // -- PLAYER --

        #region -- NPC --
        private void FullSyncNpc(FullSyncNpcPacket packet)
        {
            lock (Main.NPCs)
            {
                if (Main.NPCs.ContainsKey(packet.ID))
                {
                    EntitiesNpc npc = Main.NPCs[packet.ID];

                    npc.LastUpdateReceived = Util.GetTickCount64();

                    npc.ModelHash = packet.ModelHash;
                    npc.Props = packet.Props;
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
                }
                else
                {
                    Main.NPCs.Add(packet.ID, new EntitiesNpc()
                    {
                        LastUpdateReceived = Util.GetTickCount64(),

                        ModelHash = packet.ModelHash,
                        Props = packet.Props,
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
                        LastSyncWasFull = true
                    });
                }
            }
        }

        private void FullSyncNpcVeh(FullSyncNpcVehPacket packet)
        {
            lock (Main.NPCs)
            {
                if (Main.NPCs.ContainsKey(packet.ID))
                {
                    EntitiesNpc npc = Main.NPCs[packet.ID];

                    npc.LastUpdateReceived = Util.GetTickCount64();

                    npc.ModelHash = packet.ModelHash;
                    npc.Props = packet.Props;
                    npc.Health = packet.Health;
                    npc.Position = packet.Position.ToVector();
                    npc.VehicleModelHash = packet.VehModelHash;
                    npc.VehicleSeatIndex = packet.VehSeatIndex;
                    npc.VehiclePosition = packet.VehPosition.ToVector();
                    npc.VehicleRotation = packet.VehRotation.ToQuaternion();
                    npc.VehicleEngineHealth = packet.VehEngineHealth;
                    npc.VehRPM = packet.VehRPM;
                    npc.VehicleVelocity = packet.VehVelocity.ToVector();
                    npc.VehicleSpeed = packet.VehSpeed;
                    npc.VehicleSteeringAngle = packet.VehSteeringAngle;
                    npc.VehicleColors = packet.VehColors;
                    npc.VehDoors = packet.VehDoors;
                    npc.VehTires = packet.VehTires;
                    npc.VehIsEngineRunning = (packet.Flag.Value & (byte)VehicleDataFlags.IsEngineRunning) > 0;
                    npc.VehAreLightsOn = (packet.Flag.Value & (byte)VehicleDataFlags.AreLightsOn) > 0;
                    npc.VehAreHighBeamsOn = (packet.Flag.Value & (byte)VehicleDataFlags.AreHighBeamsOn) > 0;
                    npc.VehIsSireneActive = (packet.Flag.Value & (byte)VehicleDataFlags.IsSirenActive) > 0;
                    npc.VehicleDead = (packet.Flag.Value & (byte)VehicleDataFlags.IsDead) > 0;
                    npc.IsHornActive = (packet.Flag.Value & (byte)VehicleDataFlags.IsHornActive) > 0;
                    npc.Transformed = (packet.Flag.Value & (byte)VehicleDataFlags.IsTransformed) > 0;
                    npc.IsInVehicle = true;
                    npc.LastSyncWasFull = true;
                }
                else
                {
                    Main.NPCs.Add(packet.ID, new EntitiesNpc()
                    {
                        LastUpdateReceived = Util.GetTickCount64(),

                        ModelHash = packet.ModelHash,
                        Props = packet.Props,
                        Health = packet.Health,
                        Position = packet.Position.ToVector(),
                        VehicleModelHash = packet.VehModelHash,
                        VehicleSeatIndex = packet.VehSeatIndex,
                        VehiclePosition = packet.VehPosition.ToVector(),
                        VehicleRotation = packet.VehRotation.ToQuaternion(),
                        VehicleEngineHealth = packet.VehEngineHealth,
                        VehRPM = packet.VehRPM,
                        VehicleVelocity = packet.VehVelocity.ToVector(),
                        VehicleSpeed = packet.VehSpeed,
                        VehicleSteeringAngle = packet.VehSteeringAngle,
                        VehicleColors = packet.VehColors,
                        VehDoors = packet.VehDoors,
                        VehTires = packet.VehTires,
                        VehIsEngineRunning = (packet.Flag.Value & (byte)VehicleDataFlags.IsEngineRunning) > 0,
                        VehAreLightsOn = (packet.Flag.Value & (byte)VehicleDataFlags.AreLightsOn) > 0,
                        VehAreHighBeamsOn = (packet.Flag.Value & (byte)VehicleDataFlags.AreHighBeamsOn) > 0,
                        VehIsSireneActive = (packet.Flag.Value & (byte)VehicleDataFlags.IsSirenActive) > 0,
                        VehicleDead = (packet.Flag.Value & (byte)VehicleDataFlags.IsDead) > 0,
                        IsHornActive = (packet.Flag.Value & (byte)VehicleDataFlags.IsHornActive) > 0,
                        Transformed = (packet.Flag.Value & (byte)VehicleDataFlags.IsTransformed) > 0,
                        IsInVehicle = true,
                        LastSyncWasFull = true
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

            if ((Util.GetTickCount64() - LastPlayerFullSync) > 500)
            {
                messageType = NetDeliveryMethod.UnreliableSequenced;

                if (player.IsInVehicle())
                {
                    Vehicle veh = player.CurrentVehicle;

                    int primaryColor = 0;
                    int secondaryColor = 0;

                    unsafe
                    {
                        Function.Call<int>(Hash.GET_VEHICLE_COLOURS, veh, &primaryColor, &secondaryColor);
                    }

                    new FullSyncPlayerVehPacket()
                    {
                        Extra = new PlayerPacket()
                        {
                            ID = Main.LocalClientID,
                            Health = player.Health,
                            Position = player.Position.ToLVector()
                        },
                        ModelHash = player.Model.Hash,
                        Props = player.GetPedProps(),
                        VehModelHash = veh.Model.Hash,
                        VehSeatIndex = (int)player.SeatIndex,
                        VehPosition = veh.Position.ToLVector(),
                        VehRotation = veh.Quaternion.ToLQuaternion(),
                        VehEngineHealth = veh.EngineHealth,
                        VehRPM = veh.CurrentRPM,
                        VehVelocity = veh.Velocity.ToLVector(),
                        VehSpeed = veh.Speed,
                        VehSteeringAngle = veh.SteeringAngle,
                        VehAimCoords = veh.IsTurretSeat((int)player.SeatIndex) ? Util.GetVehicleAimCoords().ToLVector() : new LVector3(),
                        VehColors = new int[] { primaryColor, secondaryColor },
                        VehMods = veh.Mods.GetVehicleMods(),
                        VehDoors = veh.Doors.GetVehicleDoors(),
                        VehTires = veh.Wheels.GetBrokenTires(),
                        Flag = veh.GetVehicleFlags()
                    }.PacketToNetOutGoingMessage(outgoingMessage);
                }
                else
                {
                    new FullSyncPlayerPacket()
                    {
                        Extra = new PlayerPacket()
                        {
                            ID = Main.LocalClientID,
                            Health = player.Health,
                            Position = player.Position.ToLVector()
                        },
                        ModelHash = player.Model.Hash,
                        Props = player.GetPedProps(),
                        Rotation = player.Rotation.ToLVector(),
                        Velocity = player.Velocity.ToLVector(),
                        Speed = player.GetPedSpeed(),
                        AimCoords = player.GetPedAimCoords(false).ToLVector(),
                        CurrentWeaponHash = (int)player.Weapons.Current.Hash,
                        Flag = player.GetPedFlags(true)
                    }.PacketToNetOutGoingMessage(outgoingMessage);
                }

                LastPlayerFullSync = Util.GetTickCount64();
            }
            else
            {
                messageType = NetDeliveryMethod.ReliableSequenced;

                if (player.IsInVehicle())
                {
                    Vehicle veh = player.CurrentVehicle;

                    new LightSyncPlayerVehPacket()
                    {
                        Extra = new PlayerPacket()
                        {
                            ID = Main.LocalClientID,
                            Health = player.Health,
                            Position = player.Position.ToLVector()
                        },
                        VehModelHash = veh.Model.Hash,
                        VehSeatIndex = (int)player.SeatIndex,
                        VehPosition = veh.Position.ToLVector(),
                        VehRotation = veh.Quaternion.ToLQuaternion(),
                        VehVelocity = veh.Velocity.ToLVector(),
                        VehSpeed = veh.Speed,
                        VehSteeringAngle = veh.SteeringAngle,
                        Flag = veh.GetVehicleFlags()
                    }.PacketToNetOutGoingMessage(outgoingMessage);
                }
                else
                {
                    new LightSyncPlayerPacket()
                    {
                        Extra = new PlayerPacket()
                        {
                            ID = Main.LocalClientID,
                            Health = player.Health,
                            Position = player.Position.ToLVector()
                        },
                        Rotation = player.Rotation.ToLVector(),
                        Velocity = player.Velocity.ToLVector(),
                        Speed = player.GetPedSpeed(),
                        AimCoords = player.GetPedAimCoords(false).ToLVector(),
                        CurrentWeaponHash = (int)player.Weapons.Current.Hash,
                        Flag = player.GetPedFlags(true)
                    }.PacketToNetOutGoingMessage(outgoingMessage);
                }
            }

            Client.SendMessage(outgoingMessage, messageType, (int)ConnectionChannel.Player);
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

                int primaryColor = 0;
                int secondaryColor = 0;

                unsafe
                {
                    Function.Call<int>(Hash.GET_VEHICLE_COLOURS, npc.CurrentVehicle, &primaryColor, &secondaryColor);
                }

                new FullSyncNpcVehPacket()
                {
                    ID = Main.LocalClientID + npc.Handle,
                    ModelHash = npc.Model.Hash,
                    Props = npc.GetPedProps(),
                    Health = npc.Health,
                    Position = npc.Position.ToLVector(),
                    VehModelHash = veh.Model.Hash,
                    VehSeatIndex = (int)npc.SeatIndex,
                    VehPosition = veh.Position.ToLVector(),
                    VehRotation = veh.Quaternion.ToLQuaternion(),
                    VehEngineHealth = veh.EngineHealth,
                    VehRPM = veh.CurrentRPM,
                    VehVelocity = veh.Velocity.ToLVector(),
                    VehSpeed = veh.Speed,
                    VehSteeringAngle = veh.SteeringAngle,
                    VehColors = new int[] { primaryColor, secondaryColor },
                    VehMods = veh.Mods.GetVehicleMods(),
                    VehDoors = veh.Doors.GetVehicleDoors(),
                    VehTires = veh.Wheels.GetBrokenTires(),
                    Flag = veh.GetVehicleFlags()
                }.PacketToNetOutGoingMessage(outgoingMessage);
            }
            else
            {
                new FullSyncNpcPacket()
                {
                    ID = Main.LocalClientID + npc.Handle,
                    ModelHash = npc.Model.Hash,
                    Props = npc.GetPedProps(),
                    Health = npc.Health,
                    Position = npc.Position.ToLVector(),
                    Rotation = npc.Rotation.ToLVector(),
                    Velocity = npc.Velocity.ToLVector(),
                    Speed = npc.GetPedSpeed(),
                    AimCoords = npc.GetPedAimCoords(true).ToLVector(),
                    CurrentWeaponHash = (int)npc.Weapons.Current.Hash,
                    Flag = npc.GetPedFlags(true)
                }.PacketToNetOutGoingMessage(outgoingMessage);
            }

            Client.SendMessage(outgoingMessage, NetDeliveryMethod.Unreliable, (int)ConnectionChannel.NPC);
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
            new ChatMessagePacket()
            {
                Username = Main.MainSettings.Username,
                Message = message
            }.PacketToNetOutGoingMessage(outgoingMessage);
            Client.SendMessage(outgoingMessage, NetDeliveryMethod.ReliableOrdered, (int)ConnectionChannel.Chat);
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
                ID = Main.LocalClientID,
                Target = target,
                Mod = mod,
                CustomPacketID = customID,
                Bytes = bytes
            }.PacketToNetOutGoingMessage(outgoingMessage);
            Client.SendMessage(outgoingMessage, NetDeliveryMethod.ReliableOrdered, (int)ConnectionChannel.Mod);
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
