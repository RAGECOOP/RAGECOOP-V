using System;
using System.Collections.Generic;

using CoopClient.Entities;

using Lidgren.Network;

using GTA;
using GTA.Native;

namespace CoopClient
{
    public class Networking
    {
        public NetClient Client;
        public float Latency;

        public bool ShowNetworkInfo = false;

        public int BytesReceived = 0;
        public int BytesSend = 0;

        public void DisConnectFromServer(string address)
        {
            if (IsOnServer())
            {
                NetOutgoingMessage outgoingMessage = Client.CreateMessage();
                new PlayerDisconnectPacket() { ID = Main.LocalClientID }.PacketToNetOutGoingMessage(outgoingMessage);
                Client.SendMessage(outgoingMessage, NetDeliveryMethod.ReliableOrdered);
                Client.FlushSendQueue();
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

                string[] ip = address.Split(':');

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

        public bool IsOnServer()
        {
            return Client?.ConnectionStatus == NetConnectionStatus.Connected;
        }

        public void ReceiveMessages()
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

                                    Main.CleanUp();

                                    Function.Call(Hash.SET_GARBAGE_TRUCKS, 0);
                                    Function.Call(Hash.SET_RANDOM_BOATS, 0);
                                    Function.Call(Hash.SET_RANDOM_TRAINS, 0);

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

                                    Interface.Connected();
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

                                Interface.Disconnected(reason);
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
                                if (!Interface.ChatMessageReceived(chatMessagePacket.Username, chatMessagePacket.Message))
                                {
                                    Main.MainChat.AddMessage(chatMessagePacket.Username, chatMessagePacket.Message);
                                }
                                break;
                            case (byte)PacketTypes.NativeCallPacket:
                                packet = new NativeCallPacket();
                                packet.NetIncomingMessageToPacket(message);
                                DecodeNativeCall((NativeCallPacket)packet);
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
                        break;
                    default:
                        break;
                }

                Interface.MessageReceived(message);
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
                LastUpdateReceived = Environment.TickCount
            };

            Main.Players.Add(packet.ID, player);
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
                player.LastSyncWasFull = (packet.Flag.Value & (byte)PedDataFlags.LastSyncWasFull) > 0;
                player.IsAiming = (packet.Flag.Value & (byte)PedDataFlags.IsAiming) > 0;
                player.IsShooting = (packet.Flag.Value & (byte)PedDataFlags.IsShooting) > 0;
                player.IsReloading = (packet.Flag.Value & (byte)PedDataFlags.IsReloading) > 0;
                player.IsJumping = (packet.Flag.Value & (byte)PedDataFlags.IsJumping) > 0;
                player.IsRagdoll = (packet.Flag.Value & (byte)PedDataFlags.IsRagdoll) > 0;
                player.IsOnFire = (packet.Flag.Value & (byte)PedDataFlags.IsOnFire) > 0;
                player.IsInVehicle = (packet.Flag.Value & (byte)PedDataFlags.IsInVehicle) > 0;

                player.Latency = packet.Extra.Latency;
                player.LastUpdateReceived = Environment.TickCount;
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
                player.VehicleVelocity = packet.VehVelocity.ToVector();
                player.VehicleSpeed = packet.VehSpeed;
                player.VehicleSteeringAngle = packet.VehSteeringAngle;
                player.VehicleColors = packet.VehColors;
                player.VehicleMods = packet.VehMods;
                player.VehDoors = packet.VehDoors;
                player.LastSyncWasFull = (packet.Flag.Value & (byte)VehicleDataFlags.LastSyncWasFull) > 0;
                player.IsInVehicle = (packet.Flag.Value & (byte)VehicleDataFlags.IsInVehicle) > 0;
                player.VehIsEngineRunning = (packet.Flag.Value & (byte)VehicleDataFlags.IsEngineRunning) > 0;
                player.VehAreLightsOn = (packet.Flag.Value & (byte)VehicleDataFlags.AreLightsOn) > 0;
                player.VehAreHighBeamsOn = (packet.Flag.Value & (byte)VehicleDataFlags.AreHighBeamsOn) > 0;
                player.VehIsSireneActive = (packet.Flag.Value & (byte)VehicleDataFlags.IsSirenActive) > 0;
                player.VehicleDead = (packet.Flag.Value & (byte)VehicleDataFlags.IsDead) > 0;

                player.Latency = packet.Extra.Latency;
                player.LastUpdateReceived = Environment.TickCount;
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
                player.LastSyncWasFull = (packet.Flag.Value & (byte)PedDataFlags.LastSyncWasFull) > 0;
                player.IsAiming = (packet.Flag.Value & (byte)PedDataFlags.IsAiming) > 0;
                player.IsShooting = (packet.Flag.Value & (byte)PedDataFlags.IsShooting) > 0;
                player.IsReloading = (packet.Flag.Value & (byte)PedDataFlags.IsReloading) > 0;
                player.IsJumping = (packet.Flag.Value & (byte)PedDataFlags.IsJumping) > 0;
                player.IsRagdoll = (packet.Flag.Value & (byte)PedDataFlags.IsRagdoll) > 0;
                player.IsOnFire = (packet.Flag.Value & (byte)PedDataFlags.IsOnFire) > 0;
                player.IsInVehicle = (packet.Flag.Value & (byte)PedDataFlags.IsInVehicle) > 0;

                player.Latency = packet.Extra.Latency;
                player.LastUpdateReceived = Environment.TickCount;
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
                player.LastSyncWasFull = (packet.Flag.Value & (byte)VehicleDataFlags.LastSyncWasFull) > 0;
                player.IsInVehicle = (packet.Flag.Value & (byte)VehicleDataFlags.IsInVehicle) > 0;
                player.VehIsEngineRunning = (packet.Flag.Value & (byte)VehicleDataFlags.IsEngineRunning) > 0;
                player.VehAreLightsOn = (packet.Flag.Value & (byte)VehicleDataFlags.AreLightsOn) > 0;
                player.VehAreHighBeamsOn = (packet.Flag.Value & (byte)VehicleDataFlags.AreHighBeamsOn) > 0;
                player.VehIsSireneActive = (packet.Flag.Value & (byte)VehicleDataFlags.IsSirenActive) > 0;
                player.VehicleDead = (packet.Flag.Value & (byte)VehicleDataFlags.IsDead) > 0;

                player.Latency = packet.Extra.Latency;
                player.LastUpdateReceived = Environment.TickCount;
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
                player.LastUpdateReceived = Environment.TickCount;
            }
        }

        private void DecodeNativeCall(NativeCallPacket packet)
        {
            List<InputArgument> arguments = new List<InputArgument>();

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

            Function.Call((Hash)packet.Hash, arguments.ToArray());
        }
#endregion // -- PLAYER --

#region -- NPC --
        private void FullSyncNpc(FullSyncNpcPacket packet)
        {
            lock (Main.Npcs)
            {
                if (Main.Npcs.ContainsKey(packet.ID))
                {
                    EntitiesNpc npc = Main.Npcs[packet.ID];

                    npc.LastUpdateReceived = Environment.TickCount;

                    npc.ModelHash = packet.ModelHash;
                    npc.Props = packet.Props;
                    npc.Health = packet.Health;
                    npc.Position = packet.Position.ToVector();
                    npc.Rotation = packet.Rotation.ToVector();
                    npc.Velocity = packet.Velocity.ToVector();
                    npc.Speed = packet.Speed;
                    npc.CurrentWeaponHash = packet.CurrentWeaponHash;
                    npc.AimCoords = packet.AimCoords.ToVector();
                    npc.LastSyncWasFull = (packet.Flag.Value & (byte)PedDataFlags.LastSyncWasFull) > 0;
                    npc.IsAiming = (packet.Flag.Value & (byte)PedDataFlags.IsAiming) > 0;
                    npc.IsShooting = (packet.Flag.Value & (byte)PedDataFlags.IsShooting) > 0;
                    npc.IsReloading = (packet.Flag.Value & (byte)PedDataFlags.IsReloading) > 0;
                    npc.IsJumping = (packet.Flag.Value & (byte)PedDataFlags.IsJumping) > 0;
                    npc.IsRagdoll = (packet.Flag.Value & (byte)PedDataFlags.IsRagdoll) > 0;
                    npc.IsOnFire = (packet.Flag.Value & (byte)PedDataFlags.IsOnFire) > 0;
                    npc.IsInVehicle = (packet.Flag.Value & (byte)PedDataFlags.IsInVehicle) > 0;
                }
                else
                {
                    Main.Npcs.Add(packet.ID, new EntitiesNpc()
                    {
                        LastUpdateReceived = Environment.TickCount,

                        ModelHash = packet.ModelHash,
                        Props = packet.Props,
                        Health = packet.Health,
                        Position = packet.Position.ToVector(),
                        Rotation = packet.Rotation.ToVector(),
                        Velocity = packet.Velocity.ToVector(),
                        Speed = packet.Speed,
                        CurrentWeaponHash = packet.CurrentWeaponHash,
                        AimCoords = packet.AimCoords.ToVector(),
                        LastSyncWasFull = (packet.Flag.Value & (byte)PedDataFlags.LastSyncWasFull) > 0,
                        IsAiming = (packet.Flag.Value & (byte)PedDataFlags.IsAiming) > 0,
                        IsShooting = (packet.Flag.Value & (byte)PedDataFlags.IsShooting) > 0,
                        IsReloading = (packet.Flag.Value & (byte)PedDataFlags.IsReloading) > 0,
                        IsJumping = (packet.Flag.Value & (byte)PedDataFlags.IsJumping) > 0,
                        IsRagdoll = (packet.Flag.Value & (byte)PedDataFlags.IsRagdoll) > 0,
                        IsOnFire = (packet.Flag.Value & (byte)PedDataFlags.IsOnFire) > 0,
                        IsInVehicle = (packet.Flag.Value & (byte)PedDataFlags.IsInVehicle) > 0
                    });
                }
            }
        }

        private void FullSyncNpcVeh(FullSyncNpcVehPacket packet)
        {
            lock (Main.Npcs)
            {
                if (Main.Npcs.ContainsKey(packet.ID))
                {
                    EntitiesNpc npc = Main.Npcs[packet.ID];

                    npc.LastUpdateReceived = Environment.TickCount;

                    npc.ModelHash = packet.ModelHash;
                    npc.Props = packet.Props;
                    npc.Health = packet.Health;
                    npc.Position = packet.Position.ToVector();
                    npc.VehicleModelHash = packet.VehModelHash;
                    npc.VehicleSeatIndex = packet.VehSeatIndex;
                    npc.VehiclePosition = packet.VehPosition.ToVector();
                    npc.VehicleRotation = packet.VehRotation.ToQuaternion();
                    npc.VehicleEngineHealth = packet.VehEngineHealth;
                    npc.VehicleVelocity = packet.VehVelocity.ToVector();
                    npc.VehicleSpeed = packet.VehSpeed;
                    npc.VehicleSteeringAngle = packet.VehSteeringAngle;
                    npc.VehicleColors = packet.VehColors;
                    npc.VehDoors = packet.VehDoors;
                    npc.LastSyncWasFull = (packet.Flag.Value & (byte)VehicleDataFlags.LastSyncWasFull) > 0;
                    npc.IsInVehicle = (packet.Flag.Value & (byte)VehicleDataFlags.IsInVehicle) > 0;
                    npc.VehIsEngineRunning = (packet.Flag.Value & (byte)VehicleDataFlags.IsEngineRunning) > 0;
                    npc.VehAreLightsOn = (packet.Flag.Value & (byte)VehicleDataFlags.AreLightsOn) > 0;
                    npc.VehAreHighBeamsOn = (packet.Flag.Value & (byte)VehicleDataFlags.AreHighBeamsOn) > 0;
                    npc.VehIsSireneActive = (packet.Flag.Value & (byte)VehicleDataFlags.IsSirenActive) > 0;
                    npc.VehicleDead = (packet.Flag.Value & (byte)VehicleDataFlags.IsDead) > 0;
                }
                else
                {
                    Main.Npcs.Add(packet.ID, new EntitiesNpc()
                    {
                        LastUpdateReceived = Environment.TickCount,

                        ModelHash = packet.ModelHash,
                        Props = packet.Props,
                        Health = packet.Health,
                        Position = packet.Position.ToVector(),
                        VehicleModelHash = packet.VehModelHash,
                        VehicleSeatIndex = packet.VehSeatIndex,
                        VehiclePosition = packet.VehPosition.ToVector(),
                        VehicleRotation = packet.VehRotation.ToQuaternion(),
                        VehicleEngineHealth = packet.VehEngineHealth,
                        VehicleVelocity = packet.VehVelocity.ToVector(),
                        VehicleSpeed = packet.VehSpeed,
                        VehicleSteeringAngle = packet.VehSteeringAngle,
                        VehicleColors = packet.VehColors,
                        VehDoors = packet.VehDoors,
                        LastSyncWasFull = (packet.Flag.Value & (byte)VehicleDataFlags.LastSyncWasFull) > 0,
                        IsInVehicle = (packet.Flag.Value & (byte)VehicleDataFlags.IsInVehicle) > 0,
                        VehIsEngineRunning = (packet.Flag.Value & (byte)VehicleDataFlags.IsEngineRunning) > 0,
                        VehAreLightsOn = (packet.Flag.Value & (byte)VehicleDataFlags.AreLightsOn) > 0,
                        VehAreHighBeamsOn = (packet.Flag.Value & (byte)VehicleDataFlags.AreHighBeamsOn) > 0,
                        VehIsSireneActive = (packet.Flag.Value & (byte)VehicleDataFlags.IsSirenActive) > 0,
                        VehicleDead = (packet.Flag.Value & (byte)VehicleDataFlags.IsDead) > 0
                    });
                }
            }
        }
#endregion // -- NPC --
#endregion

#region -- SEND --
        private int LastPlayerFullSync = 0;
        public void SendPlayerData()
        {
            Ped player = Game.Player.Character;

            NetOutgoingMessage outgoingMessage = Client.CreateMessage();
            NetDeliveryMethod messageType;

            if ((Environment.TickCount - LastPlayerFullSync) > 500)
            {
                messageType = NetDeliveryMethod.UnreliableSequenced;

                if (!player.IsInVehicle())
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
                        Props = Util.GetPedProps(player),
                        Rotation = player.Rotation.ToLVector(),
                        Velocity = player.Velocity.ToLVector(),
                        Speed = Util.GetPedSpeed(player),
                        AimCoords = Util.GetPedAimCoords(player, false).ToLVector(),
                        CurrentWeaponHash = (int)player.Weapons.Current.Hash,
                        Flag = Util.GetPedFlags(player, true, true)
                    }.PacketToNetOutGoingMessage(outgoingMessage);
                }
                else
                {
                    bool isDriver = Util.GetResponsiblePedHandle(player.CurrentVehicle) == player.Handle;

                    int secondaryColor = 0;
                    int primaryColor = 0;

                    if (isDriver)
                    {
                        unsafe
                        {
                            Function.Call<int>(Hash.GET_VEHICLE_COLOURS, player.CurrentVehicle, &primaryColor, &secondaryColor);
                        }
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
                        Props = Util.GetPedProps(player),
                        VehModelHash = player.CurrentVehicle.Model.Hash,
                        VehSeatIndex = (int)player.SeatIndex,
                        VehPosition = isDriver ? player.CurrentVehicle.Position.ToLVector() : new LVector3(),
                        VehRotation = isDriver ? player.CurrentVehicle.Quaternion.ToLQuaternion() : new LQuaternion(),
                        VehEngineHealth = isDriver ? player.CurrentVehicle.EngineHealth : 0f,
                        VehVelocity = isDriver ? player.CurrentVehicle.Velocity.ToLVector() : new LVector3(),
                        VehSpeed = isDriver ? player.CurrentVehicle.Speed : 0f,
                        VehSteeringAngle = isDriver ? player.CurrentVehicle.SteeringAngle : 0f,
                        VehColors = isDriver ? new int[] { primaryColor, secondaryColor } : new int[0],
                        VehMods = isDriver ? Util.GetVehicleMods(player.CurrentVehicle) : null,
                        VehDoors = isDriver ? Util.GetVehicleDoors(player.CurrentVehicle.Doors) : null,
                        Flag = Util.GetVehicleFlags(player, player.CurrentVehicle, true)
                    }.PacketToNetOutGoingMessage(outgoingMessage);
                }

                LastPlayerFullSync = Environment.TickCount;
            }
            else
            {
                messageType = NetDeliveryMethod.ReliableSequenced;

                if (!player.IsInVehicle())
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
                        Speed = Util.GetPedSpeed(player),
                        AimCoords = Util.GetPedAimCoords(player, false).ToLVector(),
                        CurrentWeaponHash = (int)player.Weapons.Current.Hash,
                        Flag = Util.GetPedFlags(player, false, true)
                    }.PacketToNetOutGoingMessage(outgoingMessage);
                }
                else
                {
                    bool isDriver = Util.GetResponsiblePedHandle(player.CurrentVehicle) == player.Handle;

                    new LightSyncPlayerVehPacket()
                    {
                        Extra = new PlayerPacket()
                        {
                            ID = Main.LocalClientID,
                            Health = player.Health,
                            Position = player.Position.ToLVector()
                        },
                        VehModelHash = player.CurrentVehicle.Model.Hash,
                        VehSeatIndex = (int)player.SeatIndex,
                        VehPosition = isDriver ? player.CurrentVehicle.Position.ToLVector() : new LVector3(),
                        VehRotation = isDriver ? player.CurrentVehicle.Quaternion.ToLQuaternion() : new LQuaternion(),
                        VehVelocity = isDriver ? player.CurrentVehicle.Velocity.ToLVector() : new LVector3(),
                        VehSpeed = isDriver ? player.CurrentVehicle.Speed : 0f,
                        VehSteeringAngle = isDriver ? player.CurrentVehicle.SteeringAngle : 0f,
                        Flag = Util.GetVehicleFlags(player, player.CurrentVehicle, false)
                    }.PacketToNetOutGoingMessage(outgoingMessage);
                }
            }

            Client.SendMessage(outgoingMessage, messageType);
            Client.FlushSendQueue();

#if DEBUG
            if (ShowNetworkInfo)
            {
                BytesSend += outgoingMessage.LengthBytes;
            }
#endif
        }

        public void SendNpcData(Ped npc)
        {
            NetOutgoingMessage outgoingMessage = Client.CreateMessage();

            if (!npc.IsInVehicle())
            {
                new FullSyncNpcPacket()
                {
                    ID = Main.LocalClientID + npc.Handle,
                    ModelHash = npc.Model.Hash,
                    Props = Util.GetPedProps(npc),
                    Health = npc.Health,
                    Position = npc.Position.ToLVector(),
                    Rotation = npc.Rotation.ToLVector(),
                    Velocity = npc.Velocity.ToLVector(),
                    Speed = Util.GetPedSpeed(npc),
                    AimCoords = Util.GetPedAimCoords(npc, true).ToLVector(),
                    CurrentWeaponHash = (int)npc.Weapons.Current.Hash,
                    Flag = Util.GetPedFlags(npc, true)
                }.PacketToNetOutGoingMessage(outgoingMessage);
            }
            else
            {
                bool isDriver = Util.GetResponsiblePedHandle(npc.CurrentVehicle) == npc.Handle;

                int secondaryColor = 0;
                int primaryColor = 0;

                if (isDriver)
                {
                    unsafe
                    {
                        Function.Call<int>(Hash.GET_VEHICLE_COLOURS, npc.CurrentVehicle, &primaryColor, &secondaryColor);
                    }
                }

                new FullSyncNpcVehPacket()
                {
                    ID = Main.LocalClientID + npc.Handle,
                    ModelHash = npc.Model.Hash,
                    Props = Util.GetPedProps(npc),
                    Health = npc.Health,
                    Position = npc.Position.ToLVector(),
                    VehModelHash = npc.CurrentVehicle.Model.Hash,
                    VehSeatIndex = (int)npc.SeatIndex,
                    VehPosition = isDriver ? npc.CurrentVehicle.Position.ToLVector() : new LVector3(),
                    VehRotation = isDriver ? npc.CurrentVehicle.Quaternion.ToLQuaternion() : new LQuaternion(),
                    VehEngineHealth = isDriver ? npc.CurrentVehicle.EngineHealth : 0f,
                    VehVelocity = isDriver ? npc.CurrentVehicle.Velocity.ToLVector() : new LVector3(),
                    VehSpeed = isDriver ? npc.CurrentVehicle.Speed : 0f,
                    VehSteeringAngle = isDriver ? npc.CurrentVehicle.SteeringAngle : 0f,
                    VehColors = isDriver ? new int[] { primaryColor, secondaryColor } : new int[0],
                    VehMods = isDriver ? Util.GetVehicleMods(npc.CurrentVehicle) : null,
                    VehDoors = isDriver ? Util.GetVehicleDoors(npc.CurrentVehicle.Doors) : null,
                    Flag = Util.GetVehicleFlags(npc, npc.CurrentVehicle, true)
                }.PacketToNetOutGoingMessage(outgoingMessage);
            }

            Client.SendMessage(outgoingMessage, NetDeliveryMethod.Unreliable);
            Client.FlushSendQueue();

#if DEBUG
            if (ShowNetworkInfo)
            {
                BytesSend += outgoingMessage.LengthBytes;
            }
#endif
        }

        public void SendChatMessage(string message)
        {
            NetOutgoingMessage outgoingMessage = Client.CreateMessage();
            new ChatMessagePacket()
            {
                Username = Main.MainSettings.Username,
                Message = message
            }.PacketToNetOutGoingMessage(outgoingMessage);
            Client.SendMessage(outgoingMessage, NetDeliveryMethod.ReliableOrdered);
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
