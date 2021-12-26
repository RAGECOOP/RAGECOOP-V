using System;
using System.Text;
using System.Net;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Reflection;
using System.IO;
using System.Net.Http;

using Newtonsoft.Json;

using Lidgren.Network;

namespace CoopServer
{
    internal class IpInfo
    {
        public string ip { get; set; }
        public string country { get; set; }
    }

    internal class Server
    {
        private static readonly string CompatibleVersion = "V1_3";
        private static long CurrentTick = 0;

        public static readonly Settings MainSettings = Util.Read<Settings>("CoopSettings.xml");
        private readonly Blocklist MainBlocklist = Util.Read<Blocklist>("Blocklist.xml");
        private readonly Allowlist MainAllowlist = Util.Read<Allowlist>("Allowlist.xml");

        public static NetServer MainNetServer;

        public static List<Resource> Resources = new();
        public static Dictionary<Command, Action<CommandContext>> Commands;

        public static readonly List<Client> Clients = new();

        public Server()
        {
            Logging.Info("================");
            Logging.Info($"Server version: {Assembly.GetCallingAssembly().GetName().Version}");
            Logging.Info($"Compatible GTACoOp:R versions: {CompatibleVersion.Replace('_', '.')}.x");
            Logging.Info("================");

            // 6d4ec318f1c43bd62fe13d5a7ab28650 = GTACOOP:R
            NetPeerConfiguration config = new("6d4ec318f1c43bd62fe13d5a7ab28650")
            {
                MaximumConnections = MainSettings.MaxPlayers,
                Port = MainSettings.Port,
                EnableUPnP = MainSettings.UPnP
            };

            config.EnableMessageType(NetIncomingMessageType.ConnectionApproval);
            config.EnableMessageType(NetIncomingMessageType.ConnectionLatencyUpdated);

            MainNetServer = new NetServer(config);
            MainNetServer.Start();

            Logging.Info(string.Format("Server listening on {0}:{1}", config.LocalAddress.ToString(), config.Port));

            if (MainSettings.UPnP)
            {
                Logging.Info(string.Format("Attempting to forward port {0}", MainSettings.Port));

                if (MainNetServer.UPnP.ForwardPort(MainSettings.Port, "GTACOOP:R server"))
                {
                    Logging.Info(string.Format("Server available on {0}:{1}", MainNetServer.UPnP.GetExternalIP().ToString(), config.Port));
                }
                else
                {
                    Logging.Error("Port forwarding failed! Your router may not support UPnP.");
                    Logging.Warning("If you and your friends can join this server, please ignore this error or set UPnP in CoopSettings.xml to false!");
                }
            }

            if (MainSettings.AnnounceSelf)
            {
                Logging.Info("Announcing to master server...");

                #region -- MASTERSERVER --
                new Thread(async () =>
                {
                    try
                    {
                        // TLS only
                        ServicePointManager.Expect100Continue = true;
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls13 | SecurityProtocolType.Tls12;
                        ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

                        HttpClient httpClient = new();

                        IpInfo info;

                        try
                        {
                            string data = await httpClient.GetStringAsync("https://wimip.info/json");

                            info = JsonConvert.DeserializeObject<IpInfo>(data);
                        }
                        catch
                        {
                            info = new() { ip = MainNetServer.Configuration.LocalAddress.ToString(), country = "?" };
                        }

                        byte errorCounter = 3;

                        while (!Program.ReadyToStop)
                        {
                            string msg =
                                "{ " +
                                "\"address\": \"" + info.ip + "\", " +
                                "\"port\": \"" + MainSettings.Port + "\", " +
                                "\"name\": \"" + MainSettings.Name + "\", " +
                                "\"version\": \"" + CompatibleVersion.Replace("_", ".") + "\", " +
                                "\"players\": \"" + MainNetServer.ConnectionsCount + "\", " +
                                "\"maxPlayers\": \"" + MainSettings.MaxPlayers + "\", " +
                                "\"allowlist\": \"" + MainAllowlist.Username.Any() + "\", " +
                                "\"mods\": \"" + MainSettings.ModsAllowed + "\", " +
                                "\"npcs\": \"" + MainSettings.NpcsAllowed + "\", " +
                                "\"country\": \"" + info.country + "\"" +
                                " }";

                            HttpResponseMessage response = null;
                            try
                            {
                                response = await httpClient.PostAsync(MainSettings.MasterServer, new StringContent(msg, Encoding.UTF8, "application/json"));
                                if (response == null)
                                {
                                    Logging.Error("MasterServer: Something went wrong!");
                                    break;
                                }
                            }
                            catch (Exception ex)
                            {
                                Logging.Error(ex.Message);
                                break;
                            }

                            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                            {
                                Logging.Error($"MasterServer: [{(int)response.StatusCode}]{response.StatusCode}");

                                if (errorCounter != 0)
                                {
                                    Logging.Error($"MasterServer: Remaining attempts {errorCounter--} ...");

                                    // Wait 5 seconds before trying again
                                    Thread.Sleep(5000);
                                    continue;
                                }
                                
                                break;
                            }

                            // Reset errorCounter
                            errorCounter = 3;

                            // Sleep for 12.5s
                            Thread.Sleep(12500);
                        }
                    }
                    catch (HttpRequestException ex)
                    {
                        Logging.Error($"MasterServer: {ex.InnerException.Message}");
                    }
                    catch (Exception ex)
                    {
                        Logging.Error($"MasterServer: {ex.Message}");
                    }
                }).Start();
                #endregion
            }

            if (MainSettings.Resources.Any())
            {
                Commands = new();

                MainSettings.Resources.ForEach(x =>
                {
                    try
                    {
                        string resourcepath = AppDomain.CurrentDomain.BaseDirectory + "resources" + Path.DirectorySeparatorChar + x + ".dll";
                        Logging.Info($"Loading resource \"{x}.dll\"...");

                        Assembly asm = Assembly.LoadFrom(resourcepath);
                        Type[] types = asm.GetExportedTypes();
                        IEnumerable<Type> validTypes = types.Where(t => !t.IsInterface && !t.IsAbstract).Where(t => typeof(ServerScript).IsAssignableFrom(t));
                        Type[] enumerable = validTypes as Type[] ?? validTypes.ToArray();

                        if (!enumerable.Any())
                        {
                            Logging.Error("ERROR: No classes that inherit from ServerScript have been found in the assembly. Starting freeroam.");
                        }
                        else
                        {
                            if (Activator.CreateInstance(enumerable.ToArray()[0]) is ServerScript script)
                            {
                                Resources.Add(new(script));
                            }
                            else
                            {
                                Logging.Warning("Could not create resource: it is null.");
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Logging.Error(e.InnerException.Message);
                    }
                });
            }

            Listen();
        }

        private void Listen()
        {
            Logging.Info("Listening for clients");
            Logging.Info("Please use CTRL + C if you want to stop the server!");

            while (!Program.ReadyToStop)
            {
                if (Resources.Count != 0)
                {
                    Resources.ForEach(x =>
                    {
                        x.InvokeTick(++CurrentTick);
                    });
                }

                NetIncomingMessage message;

                while ((message = MainNetServer.ReadMessage()) != null)
                {
                    switch (message.MessageType)
                    {
                        case NetIncomingMessageType.ConnectionApproval:
                            Logging.Info($"New incoming connection from: [{message.SenderConnection.RemoteEndPoint}]");
                            if (message.ReadByte() != (byte)PacketTypes.HandshakePacket)
                            {
                                Logging.Info($"IP [{message.SenderConnection.RemoteEndPoint.Address}] was blocked, reason: Wrong packet!");
                                message.SenderConnection.Deny("Wrong packet!");
                            }
                            else
                            {
                                try
                                {
                                    int len = message.ReadInt32();
                                    byte[] data = message.ReadBytes(len);

                                    HandshakePacket packet = new HandshakePacket();
                                    packet.NetIncomingMessageToPacket(data);

                                    GetHandshake(message.SenderConnection, packet);
                                }
                                catch (Exception e)
                                {
                                    Logging.Info($"IP [{message.SenderConnection.RemoteEndPoint.Address}] was blocked, reason: {e.Message}");
                                    message.SenderConnection.Deny(e.Message);
                                }
                            }
                            break;
                        case NetIncomingMessageType.StatusChanged:
                            NetConnectionStatus status = (NetConnectionStatus)message.ReadByte();

                            if (status == NetConnectionStatus.Disconnected)
                            {
                                SendPlayerDisconnectPacket(message.SenderConnection.RemoteUniqueIdentifier);
                            }
                            else if (status == NetConnectionStatus.Connected)
                            {
                                SendPlayerConnectPacket(message.SenderConnection);
                            }
                            break;
                        case NetIncomingMessageType.Data:
                            // Get packet type
                            byte type = message.ReadByte();

                            switch (type)
                            {
                                case (byte)PacketTypes.FullSyncPlayerPacket:
                                    {
                                        try
                                        {
                                            int len = message.ReadInt32();
                                            byte[] data = message.ReadBytes(len);

                                            FullSyncPlayerPacket packet = new FullSyncPlayerPacket();
                                            packet.NetIncomingMessageToPacket(data);

                                            FullSyncPlayer(packet);
                                        }
                                        catch (Exception e)
                                        {
                                            message.SenderConnection.Disconnect(e.Message);
                                        }
                                    }
                                    break;
                                case (byte)PacketTypes.FullSyncPlayerVehPacket:
                                    {
                                        try
                                        {
                                            int len = message.ReadInt32();
                                            byte[] data = message.ReadBytes(len);

                                            FullSyncPlayerVehPacket packet = new FullSyncPlayerVehPacket();
                                            packet.NetIncomingMessageToPacket(data);

                                            FullSyncPlayerVeh(packet);
                                        }
                                        catch (Exception e)
                                        {
                                            message.SenderConnection.Disconnect(e.Message);
                                        }
                                    }
                                    break;
                                case (byte)PacketTypes.LightSyncPlayerPacket:
                                    {
                                        try
                                        {
                                            int len = message.ReadInt32();
                                            byte[] data = message.ReadBytes(len);

                                            LightSyncPlayerPacket packet = new LightSyncPlayerPacket();
                                            packet.NetIncomingMessageToPacket(data);

                                            LightSyncPlayer(packet);
                                        }
                                        catch (Exception e)
                                        {
                                            message.SenderConnection.Disconnect(e.Message);
                                        }
                                    }
                                    break;
                                case (byte)PacketTypes.LightSyncPlayerVehPacket:
                                    {
                                        try
                                        {
                                            int len = message.ReadInt32();
                                            byte[] data = message.ReadBytes(len);

                                            LightSyncPlayerVehPacket packet = new LightSyncPlayerVehPacket();
                                            packet.NetIncomingMessageToPacket(data);

                                            LightSyncPlayerVeh(packet);
                                        }
                                        catch (Exception e)
                                        {
                                            message.SenderConnection.Disconnect(e.Message);
                                        }
                                    }
                                    break;
                                case (byte)PacketTypes.ChatMessagePacket:
                                    {
                                        try
                                        {
                                            int len = message.ReadInt32();
                                            byte[] data = message.ReadBytes(len);

                                            ChatMessagePacket packet = new ChatMessagePacket();
                                            packet.NetIncomingMessageToPacket(data);

                                            SendChatMessage(packet);
                                        }
                                        catch (Exception e)
                                        {
                                            message.SenderConnection.Disconnect(e.Message);
                                        }
                                    }
                                    break;
                                case (byte)PacketTypes.FullSyncNpcPacket:
                                    {
                                        if (MainSettings.NpcsAllowed)
                                        {
                                            try
                                            {
                                                int len = message.ReadInt32();
                                                byte[] data = message.ReadBytes(len);

                                                FullSyncNpcPacket packet = new FullSyncNpcPacket();
                                                packet.NetIncomingMessageToPacket(data);

                                                FullSyncNpc(message.SenderConnection, packet);
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
                                    }
                                    break;
                                case (byte)PacketTypes.FullSyncNpcVehPacket:
                                    {
                                        if (MainSettings.NpcsAllowed)
                                        {
                                            try
                                            {
                                                int len = message.ReadInt32();
                                                byte[] data = message.ReadBytes(len);

                                                FullSyncNpcVehPacket packet = new FullSyncNpcVehPacket();
                                                packet.NetIncomingMessageToPacket(data);

                                                FullSyncNpcVeh(message.SenderConnection, packet);
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

                                            Client client = Clients.Find(x => x.NetHandle == message.SenderConnection.RemoteUniqueIdentifier);
                                            if (client != null)
                                            {
                                                if (client.Callbacks.ContainsKey(packet.ID))
                                                {
                                                    client.Callbacks[packet.ID].Invoke(packet.Args[0]);
                                                    client.Callbacks.Remove(packet.ID);
                                                }
                                            }
                                        }
                                        catch (Exception e)
                                        {
                                            message.SenderConnection.Disconnect(e.Message);
                                        }
                                    }
                                    break;
                                case (byte)PacketTypes.ModPacket:
                                    {
                                        if (MainSettings.ModsAllowed)
                                        {
                                            try
                                            {
                                                int len = message.ReadInt32();
                                                byte[] data = message.ReadBytes(len);

                                                ModPacket packet = new ModPacket();
                                                packet.NetIncomingMessageToPacket(data);

                                                bool resourceResult = false;
                                                if (Resources.Any())
                                                {
                                                    Resources.ForEach(x =>
                                                    {
                                                        if (x.InvokeModPacketReceived(packet.NetHandle, packet.Target, packet.Mod, packet.CustomPacketID, packet.Bytes))
                                                        {
                                                            resourceResult = true;
                                                        }
                                                    });
                                                }

                                                if (!resourceResult && packet.Target != -1)
                                                {
                                                    NetOutgoingMessage outgoingMessage = MainNetServer.CreateMessage();
                                                    packet.PacketToNetOutGoingMessage(outgoingMessage);

                                                    if (packet.Target != 0)
                                                    {
                                                        NetConnection target = MainNetServer.Connections.Find(x => x.RemoteUniqueIdentifier == packet.Target);
                                                        if (target == null)
                                                        {
                                                            Logging.Error($"[ModPacket] target \"{packet.Target}\" not found!");
                                                        }
                                                        else
                                                        {
                                                            // Send back to target
                                                            MainNetServer.SendMessage(outgoingMessage, target, NetDeliveryMethod.ReliableOrdered, (byte)ConnectionChannel.Mod);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        // Send back to all players
                                                        MainNetServer.SendMessage(outgoingMessage, MainNetServer.Connections, NetDeliveryMethod.ReliableOrdered, (byte)ConnectionChannel.Mod);
                                                    }
                                                }
                                            }
                                            catch (Exception e)
                                            {
                                                message.SenderConnection.Disconnect(e.Message);
                                            }
                                        }
                                        else
                                        {
                                            message.SenderConnection.Disconnect("Mods are not allowed!");
                                        }
                                    }
                                    break;
                                default:
                                    Logging.Error("Unhandled Data / Packet type");
                                    break;
                            }
                            break;
                        case NetIncomingMessageType.ConnectionLatencyUpdated:
                            {
                                Client client = Clients.Find(x => x.NetHandle == message.SenderConnection.RemoteUniqueIdentifier);
                                if (client != null)
                                {
                                    client.Latency = message.ReadFloat();
                                }
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

                // 16 milliseconds to sleep to reduce CPU usage
                Thread.Sleep(1000 / 60);
            }

            Logging.Warning("Server is shutting down!");
            if (Resources.Any())
            {
                Resources.ForEach(x =>
                {
                    // Waiting for resource...
                    while (!x.ReadyToStop)
                    {
                        // 16 milliseconds to sleep to reduce CPU usage
                        Thread.Sleep(1000 / 60);
                    }
                });
            }

            if (MainNetServer.Connections.Count > 0)
            {
                MainNetServer.Connections.ForEach(x => x.Disconnect("Server is shutting down!"));
                // We have to wait some time for all Disconnect() messages to be sent successfully
                // Sleep for 1 second
                Thread.Sleep(1000);
            }
        }

        #region -- PLAYER --
        // Before we approve the connection, we must shake hands
        private void GetHandshake(NetConnection local, HandshakePacket packet)
        {
            Logging.Debug("New handshake from: [Name: " + packet.Username + " | Address: " + local.RemoteEndPoint.Address.ToString() + "]");

            if (!packet.ModVersion.StartsWith(CompatibleVersion))
            {
                local.Deny($"GTACoOp:R version {CompatibleVersion.Replace('_', '.')}.x required!");
                return;
            }
            if (string.IsNullOrWhiteSpace(packet.Username))
            {
                local.Deny("Username is empty or contains spaces!");
                return;
            }
            if (packet.Username.Any(p => !char.IsLetterOrDigit(p)))
            {
                local.Deny("Username contains special chars!");
                return;
            }
            if (MainAllowlist.Username.Any() && !MainAllowlist.Username.Contains(packet.Username.ToLower()))
            {
                local.Deny("This Username is not on the allow list!");
                return;
            }
            if (MainBlocklist.Username.Contains(packet.Username.ToLower()))
            {
                local.Deny("This Username has been blocked by this server!");
                return;
            }
            if (MainBlocklist.IP.Contains(local.RemoteEndPoint.ToString().Split(":")[0]))
            {
                local.Deny("This IP was blocked by this server!");
                return;
            }
            if (Clients.Any(x => x.Player.Username.ToLower() == packet.Username.ToLower()))
            {
                local.Deny("Username is already taken!");
                return;
            }

            long localNetHandle = local.RemoteUniqueIdentifier;

            Client tmpClient;

            // Add the player to Players
            lock (Clients)
            {
                Clients.Add(
                    tmpClient = new Client()
                    {
                        NetHandle = localNetHandle,
                        Player = new()
                        {
                            Username = packet.Username
                        }
                    }
                );
            }

            NetOutgoingMessage outgoingMessage = MainNetServer.CreateMessage();

            // Create a new handshake packet
            new HandshakePacket()
            {
                NetHandle = localNetHandle,
                Username = string.Empty,
                ModVersion = string.Empty,
                NPCsAllowed = MainSettings.NpcsAllowed
            }.PacketToNetOutGoingMessage(outgoingMessage);

            // Accept the connection and send back a new handshake packet with the connection ID
            local.Approve(outgoingMessage);

            Resources.ForEach(x => x.InvokePlayerHandshake(tmpClient));
        }

        // The connection has been approved, now we need to send all other players to the new player and the new player to all players
        private static void SendPlayerConnectPacket(NetConnection local)
        {
            Client localClient = Clients.Find(x => x.NetHandle == local.RemoteUniqueIdentifier);
            if (localClient == null)
            {
                local.Disconnect("No data found!");
                return;
            }

            List<NetConnection> clients;
            if ((clients = Util.FilterAllLocal(local)).Count > 0)
            {
                // Send all players to local
                clients.ForEach(targetPlayer =>
                {
                    long targetNetHandle = targetPlayer.RemoteUniqueIdentifier;

                    Client targetClient = Clients.Find(x => x.NetHandle == targetNetHandle);
                    if (targetClient != null)
                    {
                        NetOutgoingMessage outgoingMessage = MainNetServer.CreateMessage();
                        new PlayerConnectPacket()
                        {
                            NetHandle = targetNetHandle,
                            Username = targetClient.Player.Username
                        }.PacketToNetOutGoingMessage(outgoingMessage);
                        MainNetServer.SendMessage(outgoingMessage, local, NetDeliveryMethod.ReliableOrdered, 0);
                    }
                });

                // Send local to all players
                NetOutgoingMessage outgoingMessage = MainNetServer.CreateMessage();
                new PlayerConnectPacket()
                {
                    NetHandle = local.RemoteUniqueIdentifier,
                    Username = localClient.Player.Username
                }.PacketToNetOutGoingMessage(outgoingMessage);
                MainNetServer.SendMessage(outgoingMessage, clients, NetDeliveryMethod.ReliableOrdered, 0);
            }

            if (Resources.Any())
            {
                Resources.ForEach(x => x.InvokePlayerConnected(localClient));
            }
            else
            {
                Logging.Info($"Player {localClient.Player.Username} connected!");
            }

            if (!string.IsNullOrEmpty(MainSettings.WelcomeMessage))
            {
                SendChatMessage(new ChatMessagePacket() { Username = "Server", Message = MainSettings.WelcomeMessage }, new List<NetConnection>() { local });
            }
        }

        // Send all players a message that someone has left the server
        private static void SendPlayerDisconnectPacket(long clientID)
        {
            List<NetConnection> clients = MainNetServer.Connections;
            if (clients.Count > 0)
            {
                NetOutgoingMessage outgoingMessage = MainNetServer.CreateMessage();
                new PlayerDisconnectPacket()
                {
                    NetHandle = clientID
                }.PacketToNetOutGoingMessage(outgoingMessage);
                MainNetServer.SendMessage(outgoingMessage, clients, NetDeliveryMethod.ReliableOrdered, 0);
            }

            Client localClient = Clients.Find(x => x.NetHandle == clientID);
            if (localClient == null)
            {
                return;
            }

            Clients.Remove(localClient);

            if (Resources.Any())
            {
                Resources.ForEach(x => x.InvokePlayerDisconnected(localClient));
            }
            else
            {
                Logging.Info($"Player {localClient.Player.Username} disconnected!");
            }
        }

        private static void FullSyncPlayer(FullSyncPlayerPacket packet)
        {
            Client client = Util.GetClientByNetHandle(packet.NetHandle);
            if (client == null)
            {
                return;
            }
            // Save the new data
            client.Player.PedHandle = packet.PedHandle;
            client.Player.IsInVehicle = false;
            client.Player.Position = packet.Position;
            client.Player.Health = packet.Health;

            // Override the latency
            packet.Latency = client.Latency;

            MainNetServer.Connections.FindAll(x => x.RemoteUniqueIdentifier != packet.NetHandle).ForEach(x =>
            {
                NetOutgoingMessage outgoingMessage = MainNetServer.CreateMessage();
                if (Clients.First(y => y.NetHandle == x.RemoteUniqueIdentifier).Player.IsInRangeOf(packet.Position, 550f))
                {
                    packet.PacketToNetOutGoingMessage(outgoingMessage);
                    MainNetServer.SendMessage(outgoingMessage, x, NetDeliveryMethod.UnreliableSequenced, (byte)ConnectionChannel.PlayerFull);
                }
                else
                {
                    new SuperLightSyncPacket()
                    {
                        NetHandle = packet.NetHandle,
                        Position = packet.Position,
                        Latency = packet.Latency
                    }.PacketToNetOutGoingMessage(outgoingMessage);
                    MainNetServer.SendMessage(outgoingMessage, x, NetDeliveryMethod.UnreliableSequenced, (byte)ConnectionChannel.PlayerSuperLight);
                }
            });

            if (Resources.Any())
            {
                Resources.ForEach(x => x.InvokePlayerUpdate(client));
            }
        }

        private static void FullSyncPlayerVeh(FullSyncPlayerVehPacket packet)
        {
            Client client = Util.GetClientByNetHandle(packet.NetHandle);
            if (client == null)
            {
                return;
            }
            // Save the new data
            client.Player.PedHandle = packet.PedHandle;
            client.Player.VehicleHandle = packet.VehicleHandle;
            client.Player.IsInVehicle = true;
            client.Player.Position = packet.Position;
            client.Player.Health = packet.Health;

            // Override the latency
            packet.Latency = client.Latency;

            MainNetServer.Connections.FindAll(x => x.RemoteUniqueIdentifier != packet.NetHandle).ForEach(x =>
            {
                NetOutgoingMessage outgoingMessage = MainNetServer.CreateMessage();

                if (Clients.First(y => y.NetHandle == x.RemoteUniqueIdentifier).Player.IsInRangeOf(packet.Position, 550f))
                {
                    packet.PacketToNetOutGoingMessage(outgoingMessage);
                    MainNetServer.SendMessage(outgoingMessage, x, NetDeliveryMethod.UnreliableSequenced, (byte)ConnectionChannel.PlayerFull);
                }
                else
                {
                    new SuperLightSyncPacket()
                    {
                        NetHandle = packet.NetHandle,
                        Position = packet.Position,
                        Latency = packet.Latency
                    }.PacketToNetOutGoingMessage(outgoingMessage);
                    MainNetServer.SendMessage(outgoingMessage, x, NetDeliveryMethod.UnreliableSequenced, (byte)ConnectionChannel.PlayerSuperLight);
                }
            });

            if (Resources.Any())
            {
                Resources.ForEach(x => x.InvokePlayerUpdate(client));
            }
        }

        private static void LightSyncPlayer(LightSyncPlayerPacket packet)
        {
            Client client = Util.GetClientByNetHandle(packet.NetHandle);
            if (client == null)
            {
                return;
            }
            // Save the new data
            client.Player.Position = packet.Position;
            client.Player.Health = packet.Health;

            // Override the latency
            packet.Latency = client.Latency;

            MainNetServer.Connections.FindAll(x => x.RemoteUniqueIdentifier != packet.NetHandle).ForEach(x =>
            {
                NetOutgoingMessage outgoingMessage = MainNetServer.CreateMessage();

                if (Clients.First(y => y.NetHandle == x.RemoteUniqueIdentifier).Player.IsInRangeOf(packet.Position, 550f))
                {
                    packet.PacketToNetOutGoingMessage(outgoingMessage);
                    MainNetServer.SendMessage(outgoingMessage, x, NetDeliveryMethod.UnreliableSequenced, (byte)ConnectionChannel.PlayerLight);
                }
                else
                {
                    new SuperLightSyncPacket()
                    {
                        NetHandle = packet.NetHandle,
                        Position = packet.Position,
                        Latency = packet.Latency
                    }.PacketToNetOutGoingMessage(outgoingMessage);
                    MainNetServer.SendMessage(outgoingMessage, x, NetDeliveryMethod.UnreliableSequenced, (byte)ConnectionChannel.PlayerSuperLight);
                }
            });

            if (Resources.Any())
            {
                Resources.ForEach(x => x.InvokePlayerUpdate(client));
            }
        }

        private static void LightSyncPlayerVeh(LightSyncPlayerVehPacket packet)
        {
            Client client = Util.GetClientByNetHandle(packet.NetHandle);
            if (client == null)
            {
                return;
            }
            // Save the new data
            client.Player.Position = packet.Position;
            client.Player.Health = packet.Health;

            // Override the latency
            packet.Latency = client.Latency;

            MainNetServer.Connections.FindAll(x => x.RemoteUniqueIdentifier != packet.NetHandle).ForEach(x =>
            {
                NetOutgoingMessage outgoingMessage = MainNetServer.CreateMessage();

                if (Clients.First(y => y.NetHandle == x.RemoteUniqueIdentifier).Player.IsInRangeOf(packet.Position, 550f))
                {
                    packet.PacketToNetOutGoingMessage(outgoingMessage);
                    MainNetServer.SendMessage(outgoingMessage, x, NetDeliveryMethod.UnreliableSequenced, (byte)ConnectionChannel.PlayerLight);
                }
                else
                {
                    new SuperLightSyncPacket()
                    {
                        NetHandle = packet.NetHandle,
                        Position = packet.Position,
                        Latency = packet.Latency
                    }.PacketToNetOutGoingMessage(outgoingMessage);
                    MainNetServer.SendMessage(outgoingMessage, x, NetDeliveryMethod.UnreliableSequenced, (byte)ConnectionChannel.PlayerSuperLight);
                }
            });

            if (Resources.Any())
            {
                Resources.ForEach(x => x.InvokePlayerUpdate(client));
            }
        }

        // Send a message to targets or all players
        private static void SendChatMessage(ChatMessagePacket packet, List<NetConnection> targets = null)
        {
            if (Resources.Any())
            {
                if (packet.Message.StartsWith('/'))
                {
                    string[] cmdArgs = packet.Message.Split(" ");
                    string cmdName = cmdArgs[0].Remove(0, 1);
                    if (Commands.Any(x => x.Key.Name == cmdName))
                    {
                        string[] argsWithoutCmd = cmdArgs.Skip(1).ToArray();

                        CommandContext ctx = new()
                        {
                            Client = Clients.Find(x => x.Player.Username == packet.Username),
                            Args = argsWithoutCmd
                        };

                        KeyValuePair<Command, Action<CommandContext>> command = Commands.First(x => x.Key.Name == cmdName);

                        if (command.Key.Usage != null && command.Key.ArgsLength != argsWithoutCmd.Length)
                        {
                            NetConnection userConnection = Util.GetConnectionByUsername(packet.Username);
                            if (userConnection == default)
                            {
                                return;
                            }

                            SendChatMessage("Server", command.Key.Usage, userConnection);
                            return;
                        }

                        command.Value.Invoke(ctx);
                    }
                    else
                    {
                        NetConnection userConnection = Util.GetConnectionByUsername(packet.Username);
                        if (userConnection == default)
                        {
                            return;
                        }

                        SendChatMessage("Server", "Command not found!", userConnection);
                    }

                    return;
                }

                if (Resources.Any())
                {
                    bool resourceResult = false;
                    Resources.ForEach(x =>
                    {
                        if (x.InvokeChatMessage(packet.Username, packet.Message))
                        {
                            resourceResult = true;
                        }
                    });

                    if (resourceResult)
                    {
                        return;
                    }
                }
            }

            packet.Message = packet.Message.Replace("~", "");

            SendChatMessage(packet.Username, packet.Message, targets);

            Logging.Info(packet.Username + ": " + packet.Message);
        }

        internal static void SendChatMessage(string username, string message, List<NetConnection> targets = null)
        {
            NetOutgoingMessage outgoingMessage = MainNetServer.CreateMessage();

            new ChatMessagePacket() { Username = username, Message = message }.PacketToNetOutGoingMessage(outgoingMessage);

            MainNetServer.SendMessage(outgoingMessage, targets ?? MainNetServer.Connections, NetDeliveryMethod.ReliableOrdered, (byte)ConnectionChannel.Chat);
        }
        internal static void SendChatMessage(string username, string message, NetConnection target)
        {
            SendChatMessage(username, message, new List<NetConnection>() { target });
        }
        #endregion

        #region -- NPC --
        private static void FullSyncNpc(NetConnection local, FullSyncNpcPacket packet)
        {
            List<NetConnection> clients;
            if ((clients = Util.GetAllInRange(packet.Position, 550f, local)).Count == 0)
            {
                return;
            }

            NetOutgoingMessage outgoingMessage = MainNetServer.CreateMessage();
            packet.PacketToNetOutGoingMessage(outgoingMessage);
            MainNetServer.SendMessage(outgoingMessage, clients, NetDeliveryMethod.UnreliableSequenced, (byte)ConnectionChannel.NPCFull);
        }

        private static void FullSyncNpcVeh(NetConnection local, FullSyncNpcVehPacket packet)
        {
            List<NetConnection> clients;
            if ((clients = Util.GetAllInRange(packet.Position, 550f, local)).Count == 0)
            {
                return;
            }

            NetOutgoingMessage outgoingMessage = MainNetServer.CreateMessage();
            packet.PacketToNetOutGoingMessage(outgoingMessage);
            MainNetServer.SendMessage(outgoingMessage, clients, NetDeliveryMethod.UnreliableSequenced, (byte)ConnectionChannel.NPCFull);
        }
        #endregion

        public static void RegisterCommand(string name, string usage, short argsLength, Action<CommandContext> callback)
        {
            Command command = new(name) { Usage = usage, ArgsLength = argsLength };

            if (Commands.ContainsKey(command))
            {
                throw new Exception("Command \"" + command.Name + "\" was already been registered!");
            }

            Commands.Add(command, callback);
        }
        public static void RegisterCommand(string name, Action<CommandContext> callback)
        {
            Command command = new(name);

            if (Commands.ContainsKey(command))
            {
                throw new Exception("Command \"" + command.Name + "\" was already been registered!");
            }

            Commands.Add(command, callback);
        }

        public static void RegisterCommands<T>()
        {
            IEnumerable<MethodInfo> commands = typeof(T).GetMethods().Where(method => method.GetCustomAttributes(typeof(Command), false).Any());

            foreach (MethodInfo method in commands)
            {
                Command attribute = method.GetCustomAttribute<Command>(true);

                RegisterCommand(attribute.Name, attribute.Usage, attribute.ArgsLength, (Action<CommandContext>)Delegate.CreateDelegate(typeof(Action<CommandContext>), method));
            }
        }
    }
}
