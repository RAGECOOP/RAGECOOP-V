using System;

using CoopClient.Entities;

using Lidgren.Network;

using GTA;
using GTA.Math;
using GTA.Native;

namespace CoopClient
{
    public class Networking
    {
        public NetClient Client;

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
                                Main.MainMenu.Items[0].Enabled = false;
                                Main.MainMenu.Items[1].Enabled = false;
                                Main.MainMenu.Items[2].Enabled = false;
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

                                    foreach (Ped entity in World.GetAllPeds())
                                    {
                                        if (entity.Handle != Game.Player.Character.Handle)
                                        {
                                            entity.Kill();
                                            entity.Delete();
                                        }
                                    }

                                    foreach (Vehicle vehicle in World.GetAllVehicles())
                                    {
                                        if (Game.Player.Character.CurrentVehicle?.Handle != vehicle.Handle)
                                        {
                                            vehicle.Delete();
                                        }
                                    }

                                    Function.Call(Hash.SET_GARBAGE_TRUCKS, 0);
                                    Function.Call(Hash.SET_RANDOM_BOATS, 0);
                                    Function.Call(Hash.SET_RANDOM_TRAINS, 0);

                                    Main.MainMenu.Items[2].Enabled = true;
                                    Main.MainMenu.Items[2].Title = "Disconnect";
                                    Main.MainSettingsMenu.Items[0].Enabled = Main.NpcsAllowed;

                                    Main.MainChat.Init();
                                    Main.MainPlayerList.Init(Main.MainSettings.Username);

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
                                }
                                break;
                            case NetConnectionStatus.Disconnected:
                                GTA.UI.Notification.Show("~r~Disconnected: " + reason);

                                // Reset all values
                                FullPlayerSync = true;

                                Main.NpcsAllowed = false;

                                if (Main.MainChat.Focused)
                                {
                                    Main.MainChat.Focused = false;
                                }

                                Main.MainChat.Clear();

                                Main.MainMenu.Items[0].Enabled = true;
                                Main.MainMenu.Items[1].Enabled = true;
                                Main.MainMenu.Items[2].Enabled = true;
                                Main.MainMenu.Items[2].Title = "Connect";
                                Main.MainSettingsMenu.Items[0].Enabled = false;

                                Main.Players.Clear();
                                Main.Npcs.Clear();

                                Vector3 pos = Game.Player.Character.Position;
                                Function.Call(Hash.CLEAR_AREA_OF_PEDS, pos.X, pos.Y, pos.Z, 300.0f, 0);
                                Function.Call(Hash.CLEAR_AREA_OF_VEHICLES, pos.X, pos.Y, pos.Z, 300.0f, 0);
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
                            case (byte)PacketTypes.FullSyncNpcPacket:
                                packet = new FullSyncNpcPacket();
                                packet.NetIncomingMessageToPacket(message);
                                FullSyncNpc((FullSyncNpcPacket)packet);
                                break;
                            case (byte)PacketTypes.LightSyncPlayerPacket:
                                packet = new LightSyncPlayerPacket();
                                packet.NetIncomingMessageToPacket(message);
                                LightSyncPlayer((LightSyncPlayerPacket)packet);
                                break;
                            case (byte)PacketTypes.ChatMessagePacket:
                                packet = new ChatMessagePacket();
                                packet.NetIncomingMessageToPacket(message);

                                ChatMessagePacket chatMessagePacket = (ChatMessagePacket)packet;
                                Main.MainChat.AddMessage(chatMessagePacket.Username, chatMessagePacket.Message);
                                break;
                        }
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

        #region GET
        private void PlayerConnect(PlayerConnectPacket packet)
        {
            EntitiesPlayer player = new EntitiesPlayer()
            {
                SocialClubName = packet.SocialClubName,
                Username = packet.Username
            };

            Main.Players.Add(packet.Player, player);

            Main.MainPlayerList.Update(Main.Players, Main.MainSettings.Username);
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

                Main.Players.Remove(packet.Player);

                Main.MainPlayerList.Update(Main.Players, Main.MainSettings.Username);
            }
        }

        private void FullSyncPlayer(FullSyncPlayerPacket packet)
        {
            if (Main.Players.ContainsKey(packet.Player))
            {
                EntitiesPlayer player = Main.Players[packet.Player];
                player.ModelHash = packet.ModelHash;
                player.Props = packet.Props;
                player.Health = packet.Health;
                player.Position = packet.Position.ToVector();
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
            }
        }

        private void FullSyncNpc(FullSyncNpcPacket packet)
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
                    IsOnFire = (packet.Flag.Value & (byte)PedDataFlags.IsOnFire) > 0
                });
            }
        }

        private void LightSyncPlayer(LightSyncPlayerPacket packet)
        {
            if (Main.Players.ContainsKey(packet.Player))
            {
                EntitiesPlayer player = Main.Players[packet.Player];
                player.Health = packet.Health;
                player.Position = packet.Position.ToVector();
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
            }
        }
        #endregion

        #region SEND
        private bool FullPlayerSync = true;
        public void SendPlayerData()
        {
            Ped player = Game.Player.Character;

            NetOutgoingMessage outgoingMessage = Client.CreateMessage();

            if (FullPlayerSync)
            {
                new FullSyncPlayerPacket()
                {
                    Player = Main.LocalPlayerID,
                    ModelHash = player.Model.Hash,
                    Props = Util.GetPedProps(player),
                    Health = player.Health,
                    Position = player.Position.ToLVector(),
                    Rotation = player.Rotation.ToLVector(),
                    Velocity = player.Velocity.ToLVector(),
                    Speed = Util.GetPedSpeed(player),
                    AimCoords = Util.GetPedAimCoords(player, false).ToLVector(),
                    CurrentWeaponHash = (int)player.Weapons.Current.Hash,
                    Flag = Util.GetPedFlags(player, true)
                }.PacketToNetOutGoingMessage(outgoingMessage);
            }
            else
            {
                new LightSyncPlayerPacket()
                {
                    Player = Main.LocalPlayerID,
                    Health = player.Health,
                    Position = player.Position.ToLVector(),
                    Rotation = player.Rotation.ToLVector(),
                    Velocity = player.Velocity.ToLVector(),
                    Speed = Util.GetPedSpeed(player),
                    AimCoords = Util.GetPedAimCoords(player, false).ToLVector(),
                    CurrentWeaponHash = (int)player.Weapons.Current.Hash,
                    Flag = Util.GetPedFlags(player, false)
                }.PacketToNetOutGoingMessage(outgoingMessage);
            }

            Client.SendMessage(outgoingMessage, NetDeliveryMethod.ReliableOrdered);
            Client.FlushSendQueue();

            FullPlayerSync = !FullPlayerSync;
        }

        public void SendNpcData(Ped npc)
        {
            NetOutgoingMessage outgoingMessage = Client.CreateMessage();

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

            Client.SendMessage(outgoingMessage, NetDeliveryMethod.ReliableOrdered);
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
