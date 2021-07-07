using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Text;

using Lidgren.Network;

using CoopServer.Entities;

namespace CoopServer
{
    class MasterServer
    {
        private Thread MainThread;

        public void Start()
        {
            MainThread = new Thread(Listen);
            MainThread.Start();
        }

        private void Listen()
        {
            try
            {
                IPHostEntry host = Dns.GetHostEntry(Server.MainSettings.MasterServer);
                IPAddress ipAddress = host.AddressList[0];
                IPEndPoint remoteEP = new(ipAddress, 11000);

                // Create a TCP/IP socket
                Socket sender = new(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                sender.Connect(remoteEP);

                Logging.Info("Server connected to MasterServer");

                while (sender.Connected)
                {
                    // Encode the data string into a byte array
                    byte[] msg = Encoding.ASCII.GetBytes(
                        "{ \"method\": \"POST\", \"data\": { " +
                        "\"Port\": \"" + Server.MainSettings.ServerPort + "\", " +
                        "\"Name\": \"" + Server.MainSettings.ServerName + "\", " +
                        "\"Version\": \"" + Server.CurrentModVersion.Replace("_", ".") + "\", " +
                        "\"Players\": " + Server.MainNetServer.ConnectionsCount + ", " +
                        "\"MaxPlayers\": " + Server.MainSettings.MaxPlayers + ", " +
                        "\"NpcsAllowed\": \"" + Server.MainSettings.NpcsAllowed + "\" } }");

                    // Send the data
                    sender.Send(msg);

                    // Sleep for 15 seconds
                    Thread.Sleep(15000);
                }
            }
            catch (SocketException se)
            {
                Logging.Error(se.Message);
            }
            catch (Exception e)
            {
                Logging.Error(e.Message);
            }
        }
    }

    class Server
    {
        public static readonly string CurrentModVersion = Enum.GetValues(typeof(ModVersion)).Cast<ModVersion>().Last().ToString();

        public static readonly Settings MainSettings = Util.Read<Settings>("CoopSettings.xml");
        private readonly Blocklist MainBlocklist = Util.Read<Blocklist>("Blocklist.xml");
        private readonly Allowlist MainAllowlist = Util.Read<Allowlist>("Allowlist.xml");

        public static NetServer MainNetServer;

        private readonly MasterServer MainMasterServer = new();

        private static readonly Dictionary<string, EntitiesPlayer> Players = new();

        public Server()
        {
            // 6d4ec318f1c43bd62fe13d5a7ab28650 = GTACOOP:R
            NetPeerConfiguration config = new("6d4ec318f1c43bd62fe13d5a7ab28650")
            {
                MaximumConnections = MainSettings.MaxPlayers,
                Port = MainSettings.ServerPort
            };

            config.EnableMessageType(NetIncomingMessageType.ConnectionApproval);

            MainNetServer = new NetServer(config);
            MainNetServer.Start();

            Logging.Info(string.Format("Server listening on {0}:{1}", config.LocalAddress.ToString(), config.Port));

            if (MainSettings.AnnounceSelf)
            {
                MainMasterServer.Start();
            }

            Listen();
        }

        private void Listen()
        {
            Logging.Info("Listening for clients");

            while (!Console.KeyAvailable || Console.ReadKey().Key != ConsoleKey.Escape)
            {
                // 16 milliseconds to sleep to reduce CPU usage
                Thread.Sleep(1000 / 60);

                NetIncomingMessage message;

                while ((message = MainNetServer.ReadMessage()) != null)
                {
                    switch (message.MessageType)
                    {
                        case NetIncomingMessageType.ConnectionApproval:
                            Logging.Info("New incoming connection from: " + message.SenderConnection.RemoteEndPoint.ToString());
                            if (message.ReadByte() != (byte)PacketTypes.HandshakePacket)
                            {
                                message.SenderConnection.Deny("Wrong packet!");
                            }
                            else
                            {
                                Packet approvalPacket;
                                approvalPacket = new HandshakePacket();
                                approvalPacket.NetIncomingMessageToPacket(message);
                                GetHandshake(message.SenderConnection, (HandshakePacket)approvalPacket);
                            }
                            break;
                        case NetIncomingMessageType.StatusChanged:
                            NetConnectionStatus status = (NetConnectionStatus)message.ReadByte();

                            string reason = message.ReadString();
                            string player = NetUtility.ToHexString(message.SenderConnection.RemoteUniqueIdentifier);
                            //Logging.Debug(NetUtility.ToHexString(message.SenderConnection.RemoteUniqueIdentifier) + " " + status + ": " + reason);

                            switch (status)
                            {
                                case NetConnectionStatus.Connected:
                                    //Logging.Info("New incoming connection from: " + message.SenderConnection.RemoteEndPoint.ToString());
                                    break;
                                case NetConnectionStatus.Disconnected:
                                    if (Players.ContainsKey(player))
                                    {
                                        SendPlayerDisconnectPacket(new PlayerDisconnectPacket() { Player = player }, reason);
                                    }
                                    break;
                            }
                            break;
                        case NetIncomingMessageType.Data:
                            // Get packet type
                            byte type = message.ReadByte();

                            // Create packet
                            Packet packet;

                            switch (type)
                            {
                                case (byte)PacketTypes.PlayerConnectPacket:
                                    packet = new PlayerConnectPacket();
                                    packet.NetIncomingMessageToPacket(message);
                                    SendPlayerConnectPacket(message.SenderConnection, (PlayerConnectPacket)packet);
                                    break;
                                case (byte)PacketTypes.PlayerDisconnectPacket:
                                    packet = new PlayerDisconnectPacket();
                                    packet.NetIncomingMessageToPacket(message);
                                    SendPlayerDisconnectPacket((PlayerDisconnectPacket)packet);
                                    break;
                                case (byte)PacketTypes.FullSyncPlayerPacket:
                                    packet = new FullSyncPlayerPacket();
                                    packet.NetIncomingMessageToPacket(message);
                                    FullSyncPlayer((FullSyncPlayerPacket)packet);
                                    break;
                                case (byte)PacketTypes.FullSyncNpcPacket:
                                    if (MainSettings.NpcsAllowed)
                                    {
                                        packet = new FullSyncNpcPacket();
                                        packet.NetIncomingMessageToPacket(message);
                                        FullSyncNpc(message.SenderConnection, (FullSyncNpcPacket)packet);
                                    }
                                    else
                                    {
                                        Logging.Warning(Players[NetUtility.ToHexString(message.SenderConnection.RemoteUniqueIdentifier)].Username + " tries to send Npcs!");
                                        message.SenderConnection.Disconnect("Npcs are not allowed!");
                                    }
                                    break;
                                case (byte)PacketTypes.LightSyncPlayerPacket:
                                    packet = new LightSyncPlayerPacket();
                                    packet.NetIncomingMessageToPacket(message);
                                    LightSyncPlayer((LightSyncPlayerPacket)packet);
                                    break;
                                case (byte)PacketTypes.ChatMessagePacket:
                                    packet = new ChatMessagePacket();
                                    packet.NetIncomingMessageToPacket(message);
                                    SendChatMessage((ChatMessagePacket)packet);
                                    break;
                                default:
                                    Logging.Error("Unhandled Data / Packet type");
                                    break;
                            }
                            break;
                        case NetIncomingMessageType.ErrorMessage:
                            Logging.Error(message.ReadString());
                            break;
                        case NetIncomingMessageType.WarningMessage:
                            Logging.Warning(message.ReadString());
                            break;
                        case NetIncomingMessageType.DebugMessage:
                        case NetIncomingMessageType.VerboseDebugMessage:
                            Logging.Debug(message.ReadString());
                            break;
                        default:
                            Logging.Error(string.Format("Unhandled type: {0} {1} bytes {2} | {3}", message.MessageType, message.LengthBytes, message.DeliveryMethod, message.SequenceChannel));
                            break;
                    }

                    MainNetServer.Recycle(message);
                }
            }
        }

        // Return a list of all connections but not the local connection
        private static List<NetConnection> FilterAllLocal(string local)
        {
            return new List<NetConnection>(MainNetServer.Connections.FindAll(e => !NetUtility.ToHexString(e.RemoteUniqueIdentifier).Equals(local)));
        }

        // Get all players in range of ...
        private static List<NetConnection> GetAllInRange(LVector3 position, float range)
        {
            return new List<NetConnection>(MainNetServer.Connections.FindAll(e => Players[NetUtility.ToHexString(e.RemoteUniqueIdentifier)].Ped.IsInRangeOf(position, range)));
        }
        private static List<NetConnection> GetAllInRange(LVector3 position, float range, string local)
        {
            return new List<NetConnection>(MainNetServer.Connections.FindAll(e =>
            {
                string target = NetUtility.ToHexString(e.RemoteUniqueIdentifier);
                return target != local && Players[target].Ped.IsInRangeOf(position, range);
            }));
        }

        // Before we approve the connection, we must shake hands
        private void GetHandshake(NetConnection local, HandshakePacket packet)
        {
            string localPlayerID = NetUtility.ToHexString(local.RemoteUniqueIdentifier);

            Logging.Debug("New handshake from: [" + packet.SocialClubName + " | " + packet.Username + "]");

            if (string.IsNullOrWhiteSpace(packet.Username))
            {
                local.Deny("Username is empty or contains spaces!");
                return;
            }
            else if (packet.Username.Any(p => !char.IsLetterOrDigit(p)))
            {
                local.Deny("Username contains special chars!");
                return;
            }

            if (MainSettings.Allowlist)
            {
                if (!MainAllowlist.SocialClubName.Contains(packet.SocialClubName))
                {
                    local.Deny("This Social Club name is not on the allow list!");
                    return;
                }
            }

            if (packet.ModVersion != CurrentModVersion)
            {
                local.Deny("Please update GTACoop:R to " + CurrentModVersion.Replace("_", "."));
                return;
            }

            if (MainBlocklist.SocialClubName.Contains(packet.SocialClubName))
            {
                local.Deny("This Social Club name has been blocked by this server!");
                return;
            }
            else if (MainBlocklist.Username.Contains(packet.Username))
            {
                local.Deny("This Username has been blocked by this server!");
                return;
            }
            else if (MainBlocklist.IP.Contains(local.RemoteEndPoint.ToString().Split(":")[0]))
            {
                local.Deny("This IP was blocked by this server!");
                return;
            }

            foreach (KeyValuePair<string, EntitiesPlayer> player in Players)
            {
                if (player.Value.SocialClubName == packet.SocialClubName)
                {
                    local.Deny("The name of the Social Club is already taken!");
                    return;
                }
                else if (player.Value.Username == packet.Username)
                {
                    local.Deny("Username is already taken!");
                    return;
                }
            }

            // Add the player to Players
            Players.Add(localPlayerID,
                new EntitiesPlayer()
                {
                    SocialClubName = packet.SocialClubName,
                    Username = packet.Username
                }
            );

            NetOutgoingMessage outgoingMessage = MainNetServer.CreateMessage();

            // Create a new handshake packet
            new HandshakePacket()
            {
                ID = localPlayerID,
                SocialClubName = string.Empty,
                Username = string.Empty,
                ModVersion = string.Empty,
                NpcsAllowed = MainSettings.NpcsAllowed
            }.PacketToNetOutGoingMessage(outgoingMessage);

            // Accept the connection and send back a new handshake packet with the connection ID
            local.Approve(outgoingMessage);

            Logging.Info("New player [" + packet.SocialClubName + " | " + packet.Username + "] connected!");
        }

        // The connection has been approved, now we need to send all other players to the new player and the new player to all players
        private static void SendPlayerConnectPacket(NetConnection local, PlayerConnectPacket packet)
        {
            if (!string.IsNullOrEmpty(MainSettings.WelcomeMessage))
            {
                SendChatMessage(new ChatMessagePacket() { Username = "Server", Message = MainSettings.WelcomeMessage }, new List<NetConnection>() { local });
            }

            List<NetConnection> playerList = FilterAllLocal(packet.Player);
            if (playerList.Count == 0)
            {
                return;
            }

            // Send all players to local
            playerList.ForEach(targetPlayer =>
            {
                string targetPlayerID = NetUtility.ToHexString(targetPlayer.RemoteUniqueIdentifier);

                EntitiesPlayer targetEntity = Players[targetPlayerID];

                NetOutgoingMessage outgoingMessage = MainNetServer.CreateMessage();
                new PlayerConnectPacket()
                {
                    Player = targetPlayerID,
                    SocialClubName = targetEntity.SocialClubName,
                    Username = targetEntity.Username
                }.PacketToNetOutGoingMessage(outgoingMessage);
                MainNetServer.SendMessage(outgoingMessage, local, NetDeliveryMethod.ReliableOrdered, 0);
            });

            // Send local to all players
            NetOutgoingMessage outgoingMessage = MainNetServer.CreateMessage();
            new PlayerConnectPacket()
            {
                Player = packet.Player,
                SocialClubName = Players[packet.Player].SocialClubName,
                Username = Players[packet.Player].Username
            }.PacketToNetOutGoingMessage(outgoingMessage);
            MainNetServer.SendMessage(outgoingMessage, playerList, NetDeliveryMethod.ReliableOrdered, 0);
        }

        // Send all players a message that someone has left the server
        private static void SendPlayerDisconnectPacket(PlayerDisconnectPacket packet, string reason = "Disconnected")
        {
            List<NetConnection> playerList = FilterAllLocal(packet.Player);

            if (playerList.Count != 0)
            {
                NetOutgoingMessage outgoingMessage = MainNetServer.CreateMessage();
                packet.PacketToNetOutGoingMessage(outgoingMessage);
                MainNetServer.SendMessage(outgoingMessage, playerList, NetDeliveryMethod.ReliableOrdered, 0);
            }

            Logging.Info(Players[packet.Player].Username + " left the server, reason: " + reason);
            Players.Remove(packet.Player);
        }

        private static void FullSyncPlayer(FullSyncPlayerPacket packet)
        {
            Players[packet.Player].Ped.Position = packet.Position;

            List<NetConnection> playerList = FilterAllLocal(packet.Player);

            if (playerList.Count == 0)
            {
                return;
            }

            NetOutgoingMessage outgoingMessage = MainNetServer.CreateMessage();
            new FullSyncPlayerPacket()
            {
                Player = packet.Player,
                ModelHash = packet.ModelHash,
                Props = packet.Props,
                Health = packet.Health,
                Position = packet.Position,
                Rotation = packet.Rotation,
                Velocity = packet.Velocity,
                Speed = packet.Speed,
                AimCoords = packet.AimCoords,
                CurrentWeaponHash = packet.CurrentWeaponHash,
                Flag = packet.Flag
            }.PacketToNetOutGoingMessage(outgoingMessage);
            MainNetServer.SendMessage(outgoingMessage, playerList, NetDeliveryMethod.ReliableOrdered, 0);
        }

        private static void FullSyncNpc(NetConnection local, FullSyncNpcPacket packet)
        {
            List<NetConnection> playerList = GetAllInRange(packet.Position, 300f, NetUtility.ToHexString(local.RemoteUniqueIdentifier));

            // No connection found in this area
            if (playerList.Count == 0)
            {
                return;
            }

            NetOutgoingMessage outgoingMessage = MainNetServer.CreateMessage();
            new FullSyncNpcPacket()
            {
                ID = packet.ID,
                ModelHash = packet.ModelHash,
                Props = packet.Props,
                Health = packet.Health,
                Position = packet.Position,
                Rotation = packet.Rotation,
                Velocity = packet.Velocity,
                Speed = packet.Speed,
                AimCoords = packet.AimCoords,
                CurrentWeaponHash = packet.CurrentWeaponHash,
                Flag = packet.Flag
            }.PacketToNetOutGoingMessage(outgoingMessage);
            MainNetServer.SendMessage(outgoingMessage, playerList, NetDeliveryMethod.ReliableOrdered, 0);
        }

        private static void LightSyncPlayer(LightSyncPlayerPacket packet)
        {
            Players[packet.Player].Ped.Position = packet.Position;

            List<NetConnection> playerList = FilterAllLocal(packet.Player);

            if (playerList.Count == 0)
            {
                return;
            }

            NetOutgoingMessage outgoingMessage = MainNetServer.CreateMessage();
            new FullSyncPlayerPacket()
            {
                Player = packet.Player,
                Health = packet.Health,
                Position = packet.Position,
                Rotation = packet.Rotation,
                Velocity = packet.Velocity,
                Speed = packet.Speed,
                AimCoords = packet.AimCoords,
                CurrentWeaponHash = packet.CurrentWeaponHash,
                Flag = packet.Flag
            }.PacketToNetOutGoingMessage(outgoingMessage);
            MainNetServer.SendMessage(outgoingMessage, playerList, NetDeliveryMethod.ReliableOrdered, 0);
        }

        // Send a message to targets or all players
        private static void SendChatMessage(ChatMessagePacket packet, List<NetConnection> targets = null)
        {
            string filteredMessage = packet.Message.Replace("~", "");

            Logging.Info(packet.Username + ": " + filteredMessage);

            NetOutgoingMessage outgoingMessage = MainNetServer.CreateMessage();
            new ChatMessagePacket()
            {
                Username = packet.Username,
                Message = filteredMessage
            }.PacketToNetOutGoingMessage(outgoingMessage);
            MainNetServer.SendMessage(outgoingMessage, targets ?? MainNetServer.Connections, NetDeliveryMethod.ReliableOrdered, 0);
        }
    }
}
