using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Text;
using System.Net.Http;
using System.Reflection;
using System.IO;

using Newtonsoft.Json;
using Lidgren.Network;

using CoopServer.Entities;

namespace CoopServer
{
    class IpInfo
    {
        public string Country { get; set; }
    }

    class Server
    {
        public static readonly string CurrentModVersion = "V0_5_0";

        public static readonly Settings MainSettings = Util.Read<Settings>("CoopSettings.xml");
        private readonly Blocklist MainBlocklist = Util.Read<Blocklist>("Blocklist.xml");
        private readonly Allowlist MainAllowlist = Util.Read<Allowlist>("Allowlist.xml");

        public static NetServer MainNetServer;

        public static readonly Dictionary<long, EntitiesPlayer> Players = new();

        private static ServerScript GameMode;

        public Server()
        {
            // 6d4ec318f1c43bd62fe13d5a7ab28650 = GTACOOP:R
            NetPeerConfiguration config = new("6d4ec318f1c43bd62fe13d5a7ab28650")
            {
                MaximumConnections = MainSettings.MaxPlayers,
                Port = MainSettings.ServerPort,
                EnableUPnP = MainSettings.UPnP
            };

            config.EnableMessageType(NetIncomingMessageType.ConnectionApproval);
            config.EnableMessageType(NetIncomingMessageType.ConnectionLatencyUpdated);

            MainNetServer = new NetServer(config);
            MainNetServer.Start();

            Logging.Info(string.Format("Server listening on {0}:{1}", config.LocalAddress.ToString(), config.Port));

            if (MainSettings.UPnP)
            {
                Logging.Info(string.Format("Attempting to forward port {0}", MainSettings.ServerPort));

                if (MainNetServer.UPnP.ForwardPort(MainSettings.ServerPort, "GTACOOP:R server"))
                {
                    Logging.Info(string.Format("Server available on {0}:{1}", MainNetServer.UPnP.GetExternalIP().ToString(), config.Port));
                }
                else
                {
                    Logging.Error("Port forwarding failed!");
                    Logging.Warning("If you and your friends can join this server, please ignore this error or set UPnP in CoopSettings.xml to \"false\"!");
                }
            }

            if (MainSettings.AnnounceSelf)
            {
                #region -- MASTERSERVER --
                new Thread(async () =>
                {
                    try
                    {
                        HttpClient httpClient = new();

                        IpInfo info;

                        try
                        {
                            string data = await httpClient.GetStringAsync("https://ipinfo.io/json");

                            info = JsonConvert.DeserializeObject<IpInfo>(data);
                        }
                        catch
                        {
                            info = new() { Country = "?" };
                        }

                        bool responseError = false;

                        while (!responseError)
                        {
                            string msg =
                                "{ " +
                                "\"port\": \"" + MainSettings.ServerPort + "\", " +
                                "\"name\": \"" + MainSettings.ServerName + "\", " +
                                "\"version\": \"" + CurrentModVersion.Replace("_", ".") + "\", " +
                                "\"players\": \"" + MainNetServer.ConnectionsCount + "\", " +
                                "\"maxPlayers\": \"" + MainSettings.MaxPlayers + "\", " +
                                "\"allowlist\": \"" + MainSettings.Allowlist + "\", " +
                                "\"country\": \"" + info.Country + "\"" +
                                " }";

                            HttpResponseMessage response = await httpClient.PostAsync(MainSettings.MasterServer, new StringContent(msg, Encoding.UTF8, "application/json"));

                            string responseContent = await response.Content.ReadAsStringAsync();

                            if (responseContent != "OK!")
                            {
                                Logging.Error(responseContent);
                                responseError = true;
                            }
                            else
                            {
                                // Sleep for 12.5s
                                Thread.Sleep(12500);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logging.Error(ex.Message);
                    }
                }).Start();
                #endregion
            }

            if (MainSettings.DefaultGameMode)
            {
                GameMode = new DefaultScript();
                GameMode.Start();
            }
            else if (!string.IsNullOrEmpty(MainSettings.GameMode))
            {
                try
                {
                    Logging.Info("Loading gamemode...");

                    Assembly asm = Assembly.LoadFrom(AppDomain.CurrentDomain.BaseDirectory + "gamemodes" + Path.DirectorySeparatorChar + MainSettings.GameMode + ".dll");
                    Type[] types = asm.GetExportedTypes();
                    IEnumerable<Type> validTypes = types.Where(t => !t.IsInterface && !t.IsAbstract).Where(t => typeof(ServerScript).IsAssignableFrom(t));
                    Type[] enumerable = validTypes as Type[] ?? validTypes.ToArray();

                    if (!enumerable.Any())
                    {
                        Logging.Error("ERROR: No classes that inherit from ServerScript have been found in the assembly. Starting freeroam.");
                    }
                    else
                    {
                        GameMode = Activator.CreateInstance(enumerable.ToArray()[0]) as ServerScript;
                        if (GameMode == null)
                        {
                            Logging.Warning("Could not create gamemode: it is null.");
                        }
                        else
                        {
                            GameMode.Start();
                        }
                    }
                }
                catch (Exception e)
                {
                    Logging.Error(e.Message);
                }
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
                    long player;

                    switch (message.MessageType)
                    {
                        case NetIncomingMessageType.ConnectionApproval:
                            Logging.Info("New incoming connection from: " + message.SenderConnection.RemoteEndPoint.ToString());
                            if (message.ReadByte() != (byte)PacketTypes.HandshakePacket)
                            {
                                Logging.Info(string.Format("Player with IP {0} blocked, reason: Wrong packet!", message.SenderConnection.RemoteEndPoint.ToString()));
                                message.SenderConnection.Deny("Wrong packet!");
                            }
                            else
                            {
                                try
                                {
                                    Packet approvalPacket;
                                    approvalPacket = new HandshakePacket();
                                    approvalPacket.NetIncomingMessageToPacket(message);
                                    GetHandshake(message.SenderConnection, (HandshakePacket)approvalPacket);
                                }
                                catch (Exception e)
                                {
                                    Logging.Info(string.Format("Player with IP {0} blocked, reason: {1}", message.SenderConnection.RemoteEndPoint.ToString(), e.Message));
                                    message.SenderConnection.Deny(e.Message);
                                }
                            }
                            break;
                        case NetIncomingMessageType.StatusChanged:
                            NetConnectionStatus status = (NetConnectionStatus)message.ReadByte();

                            player = message.SenderConnection.RemoteUniqueIdentifier;

                            if (status == NetConnectionStatus.Disconnected && Players.ContainsKey(player))
                            {
                                SendPlayerDisconnectPacket(new PlayerDisconnectPacket() { Player = player }, message.ReadString());
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
                                    try
                                    {
                                        packet = new PlayerConnectPacket();
                                        packet.NetIncomingMessageToPacket(message);
                                        SendPlayerConnectPacket(message.SenderConnection, (PlayerConnectPacket)packet);
                                    }
                                    catch (Exception e)
                                    {
                                        message.SenderConnection.Disconnect(e.Message);
                                    }
                                    break;
                                case (byte)PacketTypes.PlayerDisconnectPacket:
                                    try
                                    {
                                        packet = new PlayerDisconnectPacket();
                                        packet.NetIncomingMessageToPacket(message);
                                        SendPlayerDisconnectPacket((PlayerDisconnectPacket)packet);
                                    }
                                    catch (Exception e)
                                    {
                                        message.SenderConnection.Disconnect(e.Message);
                                    }
                                    break;
                                case (byte)PacketTypes.FullSyncPlayerPacket:
                                    try
                                    {
                                        packet = new FullSyncPlayerPacket();
                                        packet.NetIncomingMessageToPacket(message);
                                        FullSyncPlayer((FullSyncPlayerPacket)packet);
                                    }
                                    catch (Exception e)
                                    {
                                        message.SenderConnection.Disconnect(e.Message);
                                    }
                                    break;
                                case (byte)PacketTypes.FullSyncPlayerVehPacket:
                                    try
                                    {
                                        packet = new FullSyncPlayerVehPacket();
                                        packet.NetIncomingMessageToPacket(message);
                                        FullSyncPlayerVeh((FullSyncPlayerVehPacket)packet);
                                    }
                                    catch (Exception e)
                                    {
                                        message.SenderConnection.Disconnect(e.Message);
                                    }
                                    break;
                                case (byte)PacketTypes.LightSyncPlayerPacket:
                                    try
                                    {
                                        packet = new LightSyncPlayerPacket();
                                        packet.NetIncomingMessageToPacket(message);
                                        LightSyncPlayer((LightSyncPlayerPacket)packet);
                                    }
                                    catch (Exception e)
                                    {
                                        message.SenderConnection.Disconnect(e.Message);
                                    }
                                    break;
                                case (byte)PacketTypes.LightSyncPlayerVehPacket:
                                    try
                                    {
                                        packet = new LightSyncPlayerVehPacket();
                                        packet.NetIncomingMessageToPacket(message);
                                        LightSyncPlayerVeh((LightSyncPlayerVehPacket)packet);
                                    }
                                    catch (Exception e)
                                    {
                                        message.SenderConnection.Disconnect(e.Message);
                                    }
                                    break;
                                case (byte)PacketTypes.FullSyncNpcPacket:
                                    if (MainSettings.NpcsAllowed)
                                    {
                                        try
                                        {
                                            packet = new FullSyncNpcPacket();
                                            packet.NetIncomingMessageToPacket(message);
                                            FullSyncNpc(message.SenderConnection, (FullSyncNpcPacket)packet);
                                        }
                                        catch (Exception e)
                                        {
                                            message.SenderConnection.Disconnect(e.Message);
                                        }
                                    }
                                    else
                                    {
                                        message.SenderConnection.Disconnect("Npcs are not allowed!");
                                    }
                                    break;
                                case (byte)PacketTypes.FullSyncNpcVehPacket:
                                    if (MainSettings.NpcsAllowed)
                                    {
                                        try
                                        {
                                            packet = new FullSyncNpcVehPacket();
                                            packet.NetIncomingMessageToPacket(message);
                                            FullSyncNpcVeh(message.SenderConnection, (FullSyncNpcVehPacket)packet);
                                        }
                                        catch (Exception e)
                                        {
                                            message.SenderConnection.Disconnect(e.Message);
                                        }
                                    }
                                    else
                                    {
                                        message.SenderConnection.Disconnect("Npcs are not allowed!");
                                    }
                                    break;
                                case (byte)PacketTypes.ChatMessagePacket:
                                    try
                                    {
                                        packet = new ChatMessagePacket();
                                        packet.NetIncomingMessageToPacket(message);
                                        SendChatMessage((ChatMessagePacket)packet);
                                    }
                                    catch (Exception e)
                                    {
                                        message.SenderConnection.Disconnect(e.Message);
                                    }
                                    break;
                                default:
                                    Logging.Error("Unhandled Data / Packet type");
                                    break;
                            }
                            break;
                        case NetIncomingMessageType.ConnectionLatencyUpdated:
                            Players[message.SenderConnection.RemoteUniqueIdentifier].Latency = message.ReadFloat();
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
        private static List<NetConnection> FilterAllLocal(NetConnection local)
        {
            return new(MainNetServer.Connections.Where(e => e != local));
        }
        private static List<NetConnection> FilterAllLocal(long local)
        {
            return new(MainNetServer.Connections.Where(e => e.RemoteUniqueIdentifier != local));
        }

        // Return a list of players within range of ...
        private static List<NetConnection> GetAllInRange(LVector3 position, float range)
        {
            return new(MainNetServer.Connections.FindAll(e => Players[e.RemoteUniqueIdentifier].Ped.IsInRangeOf(position, range)));
        }
        // Return a list of players within range of ... but not the local one
        private static List<NetConnection> GetAllInRange(LVector3 position, float range, NetConnection local)
        {
            return new(MainNetServer.Connections.Where(e => e != local && Players[e.RemoteUniqueIdentifier].Ped.IsInRangeOf(position, range)));
        }

        #region -- PLAYER --
        // Before we approve the connection, we must shake hands
        private void GetHandshake(NetConnection local, HandshakePacket packet)
        {
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

            foreach (KeyValuePair<long, EntitiesPlayer> player in Players)
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

            long localPlayerID = local.RemoteUniqueIdentifier;

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
        }

        // The connection has been approved, now we need to send all other players to the new player and the new player to all players
        private static void SendPlayerConnectPacket(NetConnection local, PlayerConnectPacket packet)
        {
            if (!string.IsNullOrEmpty(MainSettings.WelcomeMessage))
            {
                SendChatMessage(new ChatMessagePacket() { Username = "Server", Message = MainSettings.WelcomeMessage }, new List<NetConnection>() { local });
            }

            if (GameMode != null)
            {
                GameMode.OnPlayerConnect(Players[packet.Player]);
            }

            List<NetConnection> playerList = FilterAllLocal(local);
            if (playerList.Count == 0)
            {
                return;
            }

            // Send all players to local
            playerList.ForEach(targetPlayer =>
            {
                long targetPlayerID = targetPlayer.RemoteUniqueIdentifier;

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
            if (GameMode != null)
            {
                GameMode.OnPlayerDisconnect(Players[packet.Player], reason);
            }

            List<NetConnection> playerList = FilterAllLocal(packet.Player);
            if (playerList.Count != 0)
            {
                NetOutgoingMessage outgoingMessage = MainNetServer.CreateMessage();
                packet.PacketToNetOutGoingMessage(outgoingMessage);
                MainNetServer.SendMessage(outgoingMessage, playerList, NetDeliveryMethod.ReliableOrdered, 0);
            }

            Players.Remove(packet.Player);
        }

        private static void FullSyncPlayer(FullSyncPlayerPacket packet)
        {
            EntitiesPlayer player = Players[packet.Extra.Player];

            player.Ped.Position = packet.Extra.Position;

            List<NetConnection> playerList = FilterAllLocal(packet.Extra.Player);
            if (playerList.Count == 0)
            {
                return;
            }

            PlayerPacket localPacket = packet.Extra;
            localPacket.Latency = player.Latency;

            packet.Extra = localPacket;

            NetOutgoingMessage outgoingMessage = MainNetServer.CreateMessage();
            packet.PacketToNetOutGoingMessage(outgoingMessage);
            MainNetServer.SendMessage(outgoingMessage, playerList, NetDeliveryMethod.UnreliableSequenced, 0);
        }

        private static void FullSyncPlayerVeh(FullSyncPlayerVehPacket packet)
        {
            EntitiesPlayer player = Players[packet.Extra.Player];

            player.Ped.Position = packet.Extra.Position;

            List<NetConnection> playerList = FilterAllLocal(packet.Extra.Player);
            if (playerList.Count == 0)
            {
                return;
            }

            PlayerPacket localPacket = packet.Extra;
            localPacket.Latency = player.Latency;

            packet.Extra = localPacket;

            NetOutgoingMessage outgoingMessage = MainNetServer.CreateMessage();
            packet.PacketToNetOutGoingMessage(outgoingMessage);
            MainNetServer.SendMessage(outgoingMessage, playerList, NetDeliveryMethod.UnreliableSequenced, 0);
        }

        private static void LightSyncPlayer(LightSyncPlayerPacket packet)
        {
            EntitiesPlayer player = Players[packet.Extra.Player];

            player.Ped.Position = packet.Extra.Position;

            List<NetConnection> playerList = FilterAllLocal(packet.Extra.Player);
            if (playerList.Count == 0)
            {
                return;
            }

            PlayerPacket localPacket = packet.Extra;
            localPacket.Latency = player.Latency;

            packet.Extra = localPacket;

            NetOutgoingMessage outgoingMessage = MainNetServer.CreateMessage();
            packet.PacketToNetOutGoingMessage(outgoingMessage);
            MainNetServer.SendMessage(outgoingMessage, playerList, NetDeliveryMethod.ReliableSequenced, 0);
        }

        private static void LightSyncPlayerVeh(LightSyncPlayerVehPacket packet)
        {
            EntitiesPlayer player = Players[packet.Extra.Player];

            player.Ped.Position = packet.Extra.Position;

            List<NetConnection> playerList = FilterAllLocal(packet.Extra.Player);
            if (playerList.Count == 0)
            {
                return;
            }

            PlayerPacket localPacket = packet.Extra;
            localPacket.Latency = player.Latency;

            packet.Extra = localPacket;

            NetOutgoingMessage outgoingMessage = MainNetServer.CreateMessage();
            packet.PacketToNetOutGoingMessage(outgoingMessage);
            MainNetServer.SendMessage(outgoingMessage, playerList, NetDeliveryMethod.ReliableSequenced, 0);
        }

        // Send a message to targets or all players
        private static void SendChatMessage(ChatMessagePacket packet, List<NetConnection> targets = null)
        {
            if (GameMode != null && GameMode.OnChatMessage(packet.Username, packet.Message))
            {
                return;
            }

            packet.Message = packet.Message.Replace("~", "");

            Logging.Info(packet.Username + ": " + packet.Message);

            NetOutgoingMessage outgoingMessage = MainNetServer.CreateMessage();
            packet.PacketToNetOutGoingMessage(outgoingMessage);
            MainNetServer.SendMessage(outgoingMessage, targets ?? MainNetServer.Connections, NetDeliveryMethod.ReliableOrdered, 0);
        }
        #endregion

        #region -- NPC --
        private static void FullSyncNpc(NetConnection local, FullSyncNpcPacket packet)
        {
            List<NetConnection> playerList = GetAllInRange(packet.Position, 300f, local);
            if (playerList.Count == 0)
            {
                return;
            }

            NetOutgoingMessage outgoingMessage = MainNetServer.CreateMessage();
            packet.PacketToNetOutGoingMessage(outgoingMessage);
            MainNetServer.SendMessage(outgoingMessage, playerList, NetDeliveryMethod.UnreliableSequenced, 0);
        }

        private static void FullSyncNpcVeh(NetConnection local, FullSyncNpcVehPacket packet)
        {
            List<NetConnection> playerList = GetAllInRange(packet.Position, 300f, local);
            if (playerList.Count == 0)
            {
                return;
            }

            NetOutgoingMessage outgoingMessage = MainNetServer.CreateMessage();
            packet.PacketToNetOutGoingMessage(outgoingMessage);
            MainNetServer.SendMessage(outgoingMessage, playerList, NetDeliveryMethod.UnreliableSequenced, 0);
        }
        #endregion
    }
}
