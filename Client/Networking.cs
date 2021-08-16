using System;

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

        public void DisConnectFromServer(string address)
        {
            if (IsOnServer())
            {
                NetOutgoingMessage outgoingMessage = Client.CreateMessage();
                new PlayerDisconnectPacket() { Player = Main.LocalPlayerID }.PacketToNetOutGoingMessage(outgoingMessage);
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
                    ID = string.Empty,
                    SocialClubName = Game.Player.Name,
                    Username = Main.MainSettings.Username,
                    ModVersion = Main.CurrentModVersion,
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
                switch (message.MessageType)
                {
                    case NetIncomingMessageType.StatusChanged:
                        NetConnectionStatus status = (NetConnectionStatus)message.ReadByte();

                        string reason = message.ReadString();

                        switch (status)
                        {
                            case NetConnectionStatus.InitiatedConnect:
                                Main.MainMenu.MainMenu.Items[0].Enabled = false;
                                Main.MainMenu.MainMenu.Items[1].Enabled = false;
                                Main.MainMenu.MainMenu.Items[2].Enabled = false;
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
                                    Main.LocalPlayerID = handshakePacket.ID;
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
                                        Player = Main.LocalPlayerID,
                                        SocialClubName = string.Empty,
                                        Username = string.Empty
                                    }.PacketToNetOutGoingMessage(outgoingMessage);
                                    Client.SendMessage(outgoingMessage, NetDeliveryMethod.ReliableOrdered);
                                    Client.FlushSendQueue();

                                    GTA.UI.Notification.Show("~g~Connected!");

                                    Main.MainMenu.MainMenu.Items[2].Enabled = true;
                                    Main.MainMenu.MainMenu.Items[2].Title = "Disconnect";
                                    Main.MainMenu.SubSettings.MainMenu.Items[0].Enabled = Main.NpcsAllowed;

                                    Main.MainMenu.MainMenu.Visible = false;
                                    Main.MainMenu.MenuPool.RefreshAll();
                                }
                                break;
                            case NetConnectionStatus.Disconnected:
                                GTA.UI.Notification.Show("~r~Disconnected: " + reason);

                                // Reset all values
                                LastPlayerFullSync = 0;

                                Main.NpcsAllowed = false;

                                if (Main.MainChat.Focused)
                                {
                                    Main.MainChat.Focused = false;
                                }

                                Main.MainChat.Clear();

                                Main.CleanUp();

                                Main.MainMenu.MainMenu.Items[0].Enabled = true;
                                Main.MainMenu.MainMenu.Items[1].Enabled = true;
                                Main.MainMenu.MainMenu.Items[2].Enabled = true;
                                Main.MainMenu.MainMenu.Items[2].Title = "Connect";
                                Main.MainMenu.SubSettings.MainMenu.Items[0].Enabled = false;

                                Main.MainMenu.MenuPool.RefreshAll();
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
                            case (byte)PacketTypes.ChatMessagePacket:
                                packet = new ChatMessagePacket();
                                packet.NetIncomingMessageToPacket(message);

                                ChatMessagePacket chatMessagePacket = (ChatMessagePacket)packet;
                                Main.MainChat.AddMessage(chatMessagePacket.Username, chatMessagePacket.Message);
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

            Main.Players.Add(packet.Player, player);
        }

        private void PlayerDisconnect(PlayerDisconnectPacket packet)
        {
            if (Main.Players.ContainsKey(packet.Player))
            {
                EntitiesPlayer player = Main.Players[packet.Player];
                if (player.Character != null && player.Character.Exists())
                {
                    player.Character.Kill();
                    player.Character.Delete();
                }

                player.PedBlip?.Delete();

                Main.Players.Remove(packet.Player);
            }
        }

        private void FullSyncPlayer(FullSyncPlayerPacket packet)
        {
            if (Main.Players.ContainsKey(packet.Extra.Player))
            {
                EntitiesPlayer player = Main.Players[packet.Extra.Player];

                player.Latency = packet.Extra.Latency;
                player.LastUpdateReceived = Environment.TickCount;

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
            }
        }

        private void FullSyncPlayerVeh(FullSyncPlayerVehPacket packet)
        {
            if (Main.Players.ContainsKey(packet.Extra.Player))
            {
                EntitiesPlayer player = Main.Players[packet.Extra.Player];

                player.Latency = packet.Extra.Latency;
                player.LastUpdateReceived = Environment.TickCount;

                player.ModelHash = packet.ModelHash;
                player.Props = packet.Props;
                player.Health = packet.Extra.Health;
                player.Position = packet.Extra.Position.ToVector();
                player.VehicleModelHash = packet.VehModelHash;
                player.VehicleSeatIndex = packet.VehSeatIndex;
                player.VehiclePosition = packet.VehPosition.ToVector();
                player.VehicleRotation = packet.VehRotation.ToQuaternion();
                player.VehicleVelocity = packet.VehVelocity.ToVector();
                player.VehicleSpeed = packet.VehSpeed;
                player.VehicleSteeringAngle = packet.VehSteeringAngle;
                player.VehicleColors = packet.VehColors;
                player.VehDoors = packet.VehDoors;
                player.LastSyncWasFull = (packet.Flag.Value & (byte)VehicleDataFlags.LastSyncWasFull) > 0;
                player.IsInVehicle = (packet.Flag.Value & (byte)VehicleDataFlags.IsInVehicle) > 0;
                player.VehIsEngineRunning = (packet.Flag.Value & (byte)VehicleDataFlags.IsEngineRunning) > 0;
                player.VehAreLightsOn = (packet.Flag.Value & (byte)VehicleDataFlags.AreLightsOn) > 0;
                player.VehAreHighBeamsOn = (packet.Flag.Value & (byte)VehicleDataFlags.AreHighBeamsOn) > 0;
                player.VehIsInBurnout = (packet.Flag.Value & (byte)VehicleDataFlags.IsInBurnout) > 0;
                player.VehIsSireneActive = (packet.Flag.Value & (byte)VehicleDataFlags.IsSirenActive) > 0;
                player.VehicleDead = (packet.Flag.Value & (byte)VehicleDataFlags.IsDead) > 0;
            }
        }

        private void LightSyncPlayer(LightSyncPlayerPacket packet)
        {
            if (Main.Players.ContainsKey(packet.Extra.Player))
            {
                EntitiesPlayer player = Main.Players[packet.Extra.Player];

                player.Latency = packet.Extra.Latency;
                player.LastUpdateReceived = Environment.TickCount;

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
            }
        }

        private void LightSyncPlayerVeh(LightSyncPlayerVehPacket packet)
        {
            if (Main.Players.ContainsKey(packet.Extra.Player))
            {
                EntitiesPlayer player = Main.Players[packet.Extra.Player];

                player.Latency = packet.Extra.Latency;
                player.LastUpdateReceived = Environment.TickCount;

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
                player.VehIsInBurnout = (packet.Flag.Value & (byte)VehicleDataFlags.IsInBurnout) > 0;
                player.VehIsSireneActive = (packet.Flag.Value & (byte)VehicleDataFlags.IsSirenActive) > 0;
                player.VehicleDead = (packet.Flag.Value & (byte)VehicleDataFlags.IsDead) > 0;
            }
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
                    npc.VehIsInBurnout = (packet.Flag.Value & (byte)VehicleDataFlags.IsInBurnout) > 0;
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
                        VehIsInBurnout = (packet.Flag.Value & (byte)VehicleDataFlags.IsInBurnout) > 0,
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
                            Player = Main.LocalPlayerID,
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
                    int secondaryColor;
                    int primaryColor;

                    unsafe
                    {
                        Function.Call<int>(Hash.GET_VEHICLE_COLOURS, player.CurrentVehicle, &primaryColor, &secondaryColor);
                    }

                    new FullSyncPlayerVehPacket()
                    {
                        Extra = new PlayerPacket()
                        {
                            Player = Main.LocalPlayerID,
                            Health = player.Health,
                            Position = player.Position.ToLVector()
                        },
                        ModelHash = player.Model.Hash,
                        Props = Util.GetPedProps(player),
                        VehModelHash = player.CurrentVehicle.Model.Hash,
                        VehSeatIndex = (int)player.SeatIndex,
                        VehPosition = player.CurrentVehicle.Position.ToLVector(),
                        VehRotation = player.CurrentVehicle.Quaternion.ToLQuaternion(),
                        VehVelocity = player.CurrentVehicle.Velocity.ToLVector(),
                        VehSpeed = player.CurrentVehicle.Speed,
                        VehSteeringAngle = player.CurrentVehicle.SteeringAngle,
                        VehColors = new int[] { primaryColor, secondaryColor },
                        VehDoors = Util.GetVehicleDoors(player.CurrentVehicle.Doors),
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
                            Player = Main.LocalPlayerID,
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
                    new LightSyncPlayerVehPacket()
                    {
                        Extra = new PlayerPacket()
                        {
                            Player = Main.LocalPlayerID,
                            Health = player.Health,
                            Position = player.Position.ToLVector()
                        },
                        VehModelHash = player.CurrentVehicle.Model.Hash,
                        VehSeatIndex = (int)player.SeatIndex,
                        VehPosition = player.CurrentVehicle.Position.ToLVector(),
                        VehRotation = player.CurrentVehicle.Quaternion.ToLQuaternion(),
                        VehVelocity = player.CurrentVehicle.Velocity.ToLVector(),
                        VehSpeed = player.CurrentVehicle.Speed,
                        VehSteeringAngle = player.CurrentVehicle.SteeringAngle,
                        Flag = Util.GetVehicleFlags(player, player.CurrentVehicle, false)
                    }.PacketToNetOutGoingMessage(outgoingMessage);
                }
            }

            Client.SendMessage(outgoingMessage, messageType);
            Client.FlushSendQueue();
        }

        public void SendNpcData(Ped npc)
        {
            NetOutgoingMessage outgoingMessage = Client.CreateMessage();

            if (!npc.IsInVehicle())
            {
                new FullSyncNpcPacket()
                {
                    ID = Main.LocalPlayerID + npc.Handle,
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
                int secondaryColor;
                int primaryColor;

                unsafe
                {
                    Function.Call<int>(Hash.GET_VEHICLE_COLOURS, npc.CurrentVehicle, &primaryColor, &secondaryColor);
                }

                new FullSyncNpcVehPacket()
                {
                    ID = Main.LocalPlayerID + npc.Handle,
                    ModelHash = npc.Model.Hash,
                    Props = Util.GetPedProps(npc),
                    Health = npc.Health,
                    Position = npc.Position.ToLVector(),
                    VehModelHash = npc.CurrentVehicle.Model.Hash,
                    VehSeatIndex = (int)npc.SeatIndex,
                    VehPosition = npc.CurrentVehicle.Position.ToLVector(),
                    VehRotation = npc.CurrentVehicle.Quaternion.ToLQuaternion(),
                    VehVelocity = npc.CurrentVehicle.Velocity.ToLVector(),
                    VehSpeed = npc.CurrentVehicle.Speed,
                    VehSteeringAngle = npc.CurrentVehicle.SteeringAngle,
                    VehColors = new int[] { primaryColor, secondaryColor },
                    VehDoors = Util.GetVehicleDoors(npc.CurrentVehicle.Doors),
                    Flag = Util.GetVehicleFlags(npc, npc.CurrentVehicle, true)
                }.PacketToNetOutGoingMessage(outgoingMessage);
            }

            Client.SendMessage(outgoingMessage, NetDeliveryMethod.Unreliable);
            Client.FlushSendQueue();
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
        }
        #endregion
    }
}
