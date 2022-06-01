using System;
using System.Text;
using System.Net;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Reflection;
using System.IO;
using System.Net.Http;
using RageCoop.Core;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Lidgren.Network;

namespace RageCoop.Server
{
    public struct IpInfo
    {
        [JsonProperty("ip")]
        public string Address { get; set; }
    }

    public class Server
    {
        private static readonly string _compatibleVersion = "V0_3";
        private static long _currentTick = 0;

        public static readonly Settings MainSettings = Util.Read<Settings>("Settings.xml");
        private readonly Blocklist _mainBlocklist = Util.Read<Blocklist>("Blocklist.xml");
        public static NetServer MainNetServer;

        public static Resource RunningResource = null;
        public static readonly Dictionary<Command, Action<CommandContext>> Commands = new();
        public static readonly Dictionary<TriggerEvent, Action<EventContext>> TriggerEvents = new();
        private static Thread BackgroundThread;

        public static readonly Dictionary<long,Client> Clients = new();

        public Server()
        {
            Program.Logger.Info("================");
            Program.Logger.Info($"Server bound to: 0.0.0.0:{MainSettings.Port}");
            Program.Logger.Info($"Server version: {Assembly.GetCallingAssembly().GetName().Version}");
            Program.Logger.Info($"Compatible RAGECOOP versions: {_compatibleVersion.Replace('_', '.')}.x");
            Program.Logger.Info("================");

            // 623c92c287cc392406e7aaaac1c0f3b0 = RAGECOOP
            NetPeerConfiguration config = new("623c92c287cc392406e7aaaac1c0f3b0")
            {
                Port = MainSettings.Port,
                MaximumConnections = MainSettings.MaxPlayers,
                EnableUPnP = MainSettings.UPnP
            };

            config.EnableMessageType(NetIncomingMessageType.ConnectionApproval);
            config.EnableMessageType(NetIncomingMessageType.ConnectionLatencyUpdated);

            MainNetServer = new NetServer(config);
            MainNetServer.Start();

            Program.Logger.Info(string.Format("Server listening on {0}:{1}", config.LocalAddress.ToString(), config.Port));

            if (MainSettings.UPnP)
            {
                Program.Logger.Info(string.Format("Attempting to forward port {0}", MainSettings.Port));

                if (MainNetServer.UPnP.ForwardPort(MainSettings.Port, "RAGECOOP server"))
                {
                    Program.Logger.Info(string.Format("Server available on {0}:{1}", MainNetServer.UPnP.GetExternalIP().ToString(), config.Port));
                }
                else
                {
                    Program.Logger.Error("Port forwarding failed! Your router may not support UPnP.");
                    Program.Logger.Warning("If you and your friends can join this server, please ignore this error or set UPnP in Settings.xml to false!");
                }
            }

            if (MainSettings.AnnounceSelf)
            {

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
                            HttpResponseMessage response = await httpClient.GetAsync("https://ipinfo.io/json");
                            if (response.StatusCode != HttpStatusCode.OK)
                            {
                                throw new Exception($"IPv4 request failed! [{(int)response.StatusCode}/{response.ReasonPhrase}]");
                            }

                            string content = await response.Content.ReadAsStringAsync();
                            info = JsonConvert.DeserializeObject<IpInfo>(content);
                            Program.Logger.Info($"Your public IP is {info.Address}, announcing to master server...");
                        }
                        catch (Exception ex)
                        {
                            Program.Logger.Error(ex.InnerException?.Message ?? ex.Message);
                            return;
                        }

                        while (!Program.ReadyToStop)
                        {
                            string msg =
                                "{ " +
                                "\"address\": \"" + info.Address + "\", " +
                                "\"port\": \"" + MainSettings.Port + "\", " +
                                "\"name\": \"" + MainSettings.Name + "\", " +
                                "\"version\": \"" + _compatibleVersion.Replace("_", ".") + "\", " +
                                "\"players\": \"" + MainNetServer.ConnectionsCount + "\", " +
                                "\"maxPlayers\": \"" + MainSettings.MaxPlayers + "\"" +
                                " }";
                            HttpResponseMessage response = null;
                            try
                            {
                                response = await httpClient.PostAsync(MainSettings.MasterServer, new StringContent(msg, Encoding.UTF8, "application/json"));
                            }
                            catch (Exception ex)
                            {
                                Program.Logger.Error($"MasterServer: {ex.Message}");

                                // Sleep for 5s
                                Thread.Sleep(5000);
                                continue;
                            }

                            if (response == null)
                            {
                                Program.Logger.Error("MasterServer: Something went wrong!");
                            }
                            else if (response.StatusCode != HttpStatusCode.OK)
                            {
                                if (response.StatusCode == HttpStatusCode.BadRequest)
                                {
                                    string requestContent = await response.Content.ReadAsStringAsync();
                                    Program.Logger.Error($"MasterServer: [{(int)response.StatusCode}], {requestContent}");
                                }
                                else
                                {
                                    Program.Logger.Error($"MasterServer: [{(int)response.StatusCode}]");
                                    Program.Logger.Error($"MasterServer: [{await response.Content.ReadAsStringAsync()}]");
                                }
                            }

                            // Sleep for 10s
                            Thread.Sleep(10000);
                        }
                    }
                    catch (HttpRequestException ex)
                    {
                        Program.Logger.Error($"MasterServer: {ex.InnerException.Message}");
                    }
                    catch (Exception ex)
                    {
                        Program.Logger.Error($"MasterServer: {ex.Message}");
                    }
                }).Start();
                #endregion
            }

            if (!string.IsNullOrEmpty(MainSettings.Resource))
            {
                try
                {
                    string resourcepath = AppDomain.CurrentDomain.BaseDirectory + "resources" + Path.DirectorySeparatorChar + MainSettings.Resource + ".dll";
                    Program.Logger.Info($"Loading resource \"{MainSettings.Resource}.dll\"...");

                    Assembly asm = Assembly.LoadFrom(resourcepath);
                    Type[] types = asm.GetExportedTypes();
                    IEnumerable<Type> validTypes = types.Where(t => !t.IsInterface && !t.IsAbstract).Where(t => typeof(ServerScript).IsAssignableFrom(t));
                    Type[] enumerable = validTypes as Type[] ?? validTypes.ToArray();

                    if (!enumerable.Any())
                    {
                        Program.Logger.Error("ERROR: No classes that inherit from ServerScript have been found in the assembly. Starting freeroam.");
                    }
                    else
                    {
                        if (Activator.CreateInstance(enumerable.ToArray()[0]) is ServerScript script)
                        {
                            RunningResource = new(script);
                        }
                        else
                        {
                            Program.Logger.Warning("Could not create resource: it is null.");
                        }
                    }
                }
                catch (Exception e)
                {
                    Program.Logger.Error(e.InnerException.Message);
                }
            }

            Program.Logger.Info("Searching for client-side files...");
            DownloadManager.CheckForDirectoryAndFiles();

            Listen();

            BackgroundThread=new Thread(() => Background());
            BackgroundThread.Start();
        }

        private void Listen()
        {
            Program.Logger.Info("Listening for clients");
            Program.Logger.Info("Please use CTRL + C if you want to stop the server!");

            while (!Program.ReadyToStop)
            {
                if (RunningResource != null)
                {
                    RunningResource.InvokeTick(++_currentTick);
                }

                // Only new clients that did not receive files on connection will receive the current files in "clientside"
                if (DownloadManager.AnyFileExists)
                {
                    lock (Clients)
                    {
                        Clients.Values.ToList().ForEach(client =>
                        {
                            if (!client.FilesSent)
                            {
                                DownloadManager.InsertClient(client.NetID);
                                client.FilesSent = true;
                            }
                        });
                    }

                    DownloadManager.Tick();
                }
                
                NetIncomingMessage message;

                while ((message = MainNetServer.ReadMessage()) != null)
                {
                    switch (message.MessageType)
                    {
                        case NetIncomingMessageType.ConnectionApproval:
                            {
                                Program.Logger.Info($"New incoming connection from: [{message.SenderConnection.RemoteEndPoint}]");
                                if (message.ReadByte() != (byte)PacketTypes.Handshake)
                                {
                                    Program.Logger.Info($"IP [{message.SenderConnection.RemoteEndPoint.Address}] was blocked, reason: Wrong packet!");
                                    message.SenderConnection.Deny("Wrong packet!");
                                }
                                else
                                {
                                    try
                                    {
                                        int len = message.ReadInt32();
                                        byte[] data = message.ReadBytes(len);

                                        Packets.Handshake packet = new();
                                        packet.Unpack(data);

                                        GetHandshake(message.SenderConnection, packet);
                                    }
                                    catch (Exception e)
                                    {
                                        Program.Logger.Info($"IP [{message.SenderConnection.RemoteEndPoint.Address}] was blocked, reason: {e.Message}");
                                        message.SenderConnection.Deny(e.Message);
                                    }
                                }
                                break;
                            }
                        case NetIncomingMessageType.StatusChanged:
                            {
                                NetConnectionStatus status = (NetConnectionStatus)message.ReadByte();

                                if (status == NetConnectionStatus.Disconnected)
                                {
                                    long nethandle = message.SenderConnection.RemoteUniqueIdentifier;

                                    DownloadManager.RemoveClient(nethandle);

                                    SendPlayerDisconnectPacket(nethandle);
                                }
                                else if (status == NetConnectionStatus.Connected)
                                {
                                    SendPlayerConnectPacket(message.SenderConnection);
                                }
                                break;
                            }
                        case NetIncomingMessageType.Data:
                            // Get packet type
                            byte btype= message.ReadByte();
                            var type = (PacketTypes)btype;
                            
                            switch (type)
                            {

                                #region SyncData

                                case PacketTypes.PedStateSync:
                                    {
                                        try
                                        {
                                            int len = message.ReadInt32();
                                            byte[] data = message.ReadBytes(len);

                                            Packets.PedStateSync packet = new();
                                            packet.Unpack(data);

                                            PedStateSync(packet, message.SenderConnection.RemoteUniqueIdentifier);
                                        }
                                        catch (Exception e)
                                        {
                                            DisconnectAndLog(message.SenderConnection, type, e);
                                        }
                                    }
                                    break;
                                case PacketTypes.VehicleStateSync:
                                    {
                                        try
                                        {
                                            int len = message.ReadInt32();
                                            byte[] data = message.ReadBytes(len);

                                            Packets.VehicleStateSync packet = new();
                                            packet.Unpack(data);

                                            VehicleStateSync(packet, message.SenderConnection.RemoteUniqueIdentifier);
                                        }
                                        catch (Exception e)
                                        {
                                            DisconnectAndLog(message.SenderConnection, type, e);
                                        }
                                    }
                                    break;
                                case PacketTypes.PedSync:
                                    {
                                        try
                                        {
                                            int len = message.ReadInt32();
                                            byte[] data = message.ReadBytes(len);

                                            Packets.PedSync packet = new();
                                            packet.Unpack(data);

                                            PedSync(packet, message.SenderConnection.RemoteUniqueIdentifier);
                                        }
                                        catch (Exception e)
                                        {
                                            DisconnectAndLog(message.SenderConnection, type, e);
                                        }
                                    }
                                    break;
                                case PacketTypes.VehicleSync:
                                    {
                                        try
                                        {
                                            int len = message.ReadInt32();
                                            byte[] data = message.ReadBytes(len);

                                            Packets.VehicleSync packet = new();
                                            packet.Unpack(data);

                                            VehicleSync(packet,message.SenderConnection.RemoteUniqueIdentifier);
                                        }
                                        catch (Exception e)
                                        {
                                            DisconnectAndLog(message.SenderConnection, type, e);
                                        }
                                    }
                                    break;
                                case PacketTypes.ProjectileSync:
                                    {
                                        try
                                        {
                                            int len = message.ReadInt32();
                                            byte[] data = message.ReadBytes(len);

                                            Packets.ProjectileSync packet = new();
                                            packet.Unpack(data);
                                            ProjectileSync(packet, message.SenderConnection.RemoteUniqueIdentifier);
                                        }
                                        catch (Exception e)
                                        {
                                            DisconnectAndLog(message.SenderConnection, type, e);
                                        }
                                    }
                                    break;


                                #endregion

                                case PacketTypes.ChatMessage:
                                    {
                                        try
                                        {
                                            int len = message.ReadInt32();
                                            byte[] data = message.ReadBytes(len);

                                            Packets.ChatMessage packet = new();
                                            packet.Unpack(data);

                                            SendChatMessage(packet);
                                        }
                                        catch (Exception e)
                                        {
                                            DisconnectAndLog(message.SenderConnection, type, e);
                                        }
                                    }
                                    break;
                                
                                case PacketTypes.NativeResponse:
                                    {
                                        try
                                        {
                                            int len = message.ReadInt32();
                                            byte[] data = message.ReadBytes(len);

                                            Packets.NativeResponse packet = new();
                                            packet.Unpack(data);

                                            Client client = Util.GetClientByID(message.SenderConnection.RemoteUniqueIdentifier);
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
                                            DisconnectAndLog(message.SenderConnection, type, e);
                                        }
                                    }
                                    break;
                                case PacketTypes.FileTransferComplete:
                                    {
                                        try
                                        {
                                            if (DownloadManager.AnyFileExists)
                                            {
                                                int len = message.ReadInt32();
                                                byte[] data = message.ReadBytes(len);

                                                Packets.FileTransferComplete packet = new();
                                                packet.Unpack(data);

                                                Client client = Util.GetClientByID(message.SenderConnection.RemoteUniqueIdentifier); 
                                                if (client != null && !client.FilesReceived)
                                                {
                                                    DownloadManager.TryToRemoveClient(client.NetID, packet.ID);
                                                }
                                            }
                                        }
                                        catch (Exception e)
                                        {
                                            DisconnectAndLog(message.SenderConnection, type, e);
                                        }
                                    }
                                    break;
                                case PacketTypes.ServerClientEvent:
                                    {
                                        try
                                        {
                                            int len = message.ReadInt32();
                                            byte[] data = message.ReadBytes(len);

                                            Packets.ServerClientEvent packet = new Packets.ServerClientEvent();
                                            packet.Unpack(data);

                                            long senderNetHandle = message.SenderConnection.RemoteUniqueIdentifier;
                                            Client client = null;
                                            lock (Clients)
                                            {
                                                client = Util.GetClientByID(senderNetHandle);
                                            }

                                            if (client != null)
                                            {
                                                if (TriggerEvents.Any(x => x.Key.EventName == packet.EventName))
                                                {
                                                    EventContext ctx = new()
                                                    {
                                                        Client = client,
                                                        Args = packet.Args.ToArray()
                                                    };

                                                    TriggerEvents.FirstOrDefault(x => x.Key.EventName == packet.EventName).Value?.Invoke(ctx);
                                                }
                                                else
                                                {
                                                    Program.Logger.Warning($"Player \"{client.Player.Username}\" attempted to trigger an unknown event! [{packet.EventName}]");
                                                }
                                            }
                                        }
                                        catch (Exception e)
                                        {
                                            DisconnectAndLog(message.SenderConnection, type, e);
                                        }
                                    }
                                    break;
                                default:
                                    if (type.IsSyncEvent())
                                    {
                                        // Sync Events
                                        try
                                        {
                                            int len = message.ReadInt32();
                                            byte[] data = message.ReadBytes(len);
                                            NetOutgoingMessage outgoingMessage = MainNetServer.CreateMessage();
                                            outgoingMessage.Write(btype);
                                            outgoingMessage.Write(len);
                                            outgoingMessage.Write(data);
                                            MainNetServer.Connections.FindAll(x => x.RemoteUniqueIdentifier != message.SenderConnection.RemoteUniqueIdentifier).ForEach(x =>
                                            {

                                                MainNetServer.SendMessage(outgoingMessage, x, NetDeliveryMethod.UnreliableSequenced, (byte)ConnectionChannel.PedSync);

                                            });
                                        }
                                        catch (Exception e)
                                        {
                                            DisconnectAndLog(message.SenderConnection, type, e);
                                        }
                                    }
                                    else
                                    {
                                        Program.Logger.Error("Unhandled Data / Packet type");
                                    }
                                    break;
                            }
                            break;
                        case NetIncomingMessageType.ConnectionLatencyUpdated:
                            {
                                Client client = Util.GetClientByID(message.SenderConnection.RemoteUniqueIdentifier);
                                if (client != null)
                                {
                                    client.Latency = message.ReadFloat();
                                }
                            }
                            break;
                        case NetIncomingMessageType.ErrorMessage:
                            Program.Logger.Error(message.ReadString());
                            break;
                        case NetIncomingMessageType.WarningMessage:
                            Program.Logger.Warning(message.ReadString());
                            break;
                        case NetIncomingMessageType.DebugMessage:
                        case NetIncomingMessageType.VerboseDebugMessage:
                            Program.Logger.Debug(message.ReadString());
                            break;
                        default:
                            Program.Logger.Error(string.Format("Unhandled type: {0} {1} bytes {2} | {3}", message.MessageType, message.LengthBytes, message.DeliveryMethod, message.SequenceChannel));
                            break;
                    }

                    MainNetServer.Recycle(message);
                }

                // 16 milliseconds to sleep to reduce CPU usage
                Thread.Sleep(1000 / 60);
            }

            Program.Logger.Warning("Server is shutting down!");
            if (RunningResource != null)
            {
                // Waiting for resource...
                while (!RunningResource.ReadyToStop)
                {
                    // 16 milliseconds to sleep to reduce CPU usage
                    Thread.Sleep(1000 / 60);
                }
            }

            if (MainNetServer.Connections.Count > 0)
            {
                MainNetServer.Connections.ForEach(x => x.Disconnect("Server is shutting down!"));
                // We have to wait some time for all Disconnect() messages to be sent successfully
                // Sleep for 1 second
                Thread.Sleep(1000);
            }
        }
        private void Background()
        {
            while (true)
            {
                foreach(Client c in Clients.Values)
                {
                    MainNetServer.Connections.FindAll(x => x.RemoteUniqueIdentifier != c.NetID).ForEach(x =>
                    {
                        NetOutgoingMessage outgoingMessage = MainNetServer.CreateMessage();
                        new Packets.PlayerInfoUpdate()
                        {
                            PedID=c.Player.PedID,
                            Username=c.Player.Username,
                            Latency=c.Player.Latency=c.Latency,
                        }.Pack(outgoingMessage);
                        MainNetServer.SendMessage(outgoingMessage, x, NetDeliveryMethod.ReliableSequenced, (byte)ConnectionChannel.Default);
                    });
                }

                // Update Latency every 20 seconds.
                Thread.Sleep(1000*20);
            }
        }

        private void DisconnectAndLog(NetConnection senderConnection,PacketTypes type, Exception e)
        {
            Program.Logger.Error($"Error receiving a packet of type {type}");
            Program.Logger.Error(e.Message);
            Program.Logger.Error(e.StackTrace);
            senderConnection.Disconnect(e.Message);
        }

        #region -- PLAYER --
        // Before we approve the connection, we must shake hands
        private void GetHandshake(NetConnection local, Packets.Handshake packet)
        {
            Program.Logger.Debug("New handshake from: [Name: " + packet.Username + " | Address: " + local.RemoteEndPoint.Address.ToString() + "]");

            if (!packet.ModVersion.StartsWith(_compatibleVersion))
            {
                local.Deny($"RAGECOOP version {_compatibleVersion.Replace('_', '.')}.x required!");
                return;
            }
            if (string.IsNullOrWhiteSpace(packet.Username))
            {
                local.Deny("Username is empty or contains spaces!");
                return;
            }
            if (packet.Username.Any(p => !char.IsLetterOrDigit(p) && !(p == '_') && !(p=='-')))
            {
                local.Deny("Username contains special chars!");
                return;
            }
            if (_mainBlocklist.Username.Contains(packet.Username.ToLower()))
            {
                local.Deny("This Username has been blocked by this server!");
                return;
            }
            if (_mainBlocklist.IP.Contains(local.RemoteEndPoint.ToString().Split(":")[0]))
            {
                local.Deny("This IP was blocked by this server!");
                return;
            }
            if (Clients.Values.Any(x => x.Player.Username.ToLower() == packet.Username.ToLower()))
            {
                local.Deny("Username is already taken!");
                return;
            }

            

            Client tmpClient;

            // Add the player to Players
            lock (Clients)
            {
                Clients.Add(local.RemoteUniqueIdentifier,
                    tmpClient = new Client()
                    {
                        NetID = local.RemoteUniqueIdentifier,
                        Connection=local,
                        Player = new()
                        {
                            Username = packet.Username,
                            PedID=packet.PedID,
                        }
                    }
                );;
            }
            Program.Logger.Info($"HandShake sucess, Player:{packet.Username} PedID:{packet.PedID}");
            NetOutgoingMessage outgoingMessage = MainNetServer.CreateMessage();

            // Create a new handshake packet
            new Packets.Handshake()
            {
                PedID = packet.PedID,
                Username = string.Empty,
                ModVersion = string.Empty,
            }.Pack(outgoingMessage);

            // Accept the connection and send back a new handshake packet with the connection ID
            local.Approve(outgoingMessage);

            if (RunningResource != null)
            {
                RunningResource.InvokePlayerHandshake(tmpClient);
            }
        }

        // The connection has been approved, now we need to send all other players to the new player and the new player to all players
        private static void SendPlayerConnectPacket(NetConnection local)
        {
            Client localClient = Util.GetClientByID(local.RemoteUniqueIdentifier);
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

                    Client targetClient = Util.GetClientByID(targetNetHandle);
                    if (targetClient != null)
                    {
                        NetOutgoingMessage outgoingMessage = MainNetServer.CreateMessage();
                        new Packets.PlayerConnect()
                        {
                            // NetHandle = targetNetHandle,
                            Username = targetClient.Player.Username,
                            PedID=targetClient.Player.PedID,
                            
                        }.Pack(outgoingMessage);
                        MainNetServer.SendMessage(outgoingMessage, local, NetDeliveryMethod.ReliableOrdered, 0);
                    }
                });

                // Send local to all players
                NetOutgoingMessage outgoingMessage = MainNetServer.CreateMessage();
                new Packets.PlayerConnect()
                {
                    // NetHandle = local.RemoteUniqueIdentifier,
                    PedID=localClient.Player.PedID,
                    Username = localClient.Player.Username
                }.Pack(outgoingMessage);
                MainNetServer.SendMessage(outgoingMessage, clients, NetDeliveryMethod.ReliableOrdered, 0);
            }

            if (RunningResource != null)
            {
                RunningResource.InvokePlayerConnected(localClient);
            }
            else
            {
                Program.Logger.Info($"Player {localClient.Player.Username} connected!");
            }

            if (!string.IsNullOrEmpty(MainSettings.WelcomeMessage))
            {
                SendChatMessage(new Packets.ChatMessage() { Username = "Server", Message = MainSettings.WelcomeMessage }, new List<NetConnection>() { local });
            }
        }

        // Send all players a message that someone has left the server
        private static void SendPlayerDisconnectPacket(long nethandle)
        {
            List<NetConnection> clients = MainNetServer.Connections.FindAll(x => x.RemoteUniqueIdentifier != nethandle);
            int playerPedID = Clients[nethandle].Player.PedID;
            if (clients.Count > 0)
            {
                NetOutgoingMessage outgoingMessage = MainNetServer.CreateMessage();
                new Packets.PlayerDisconnect()
                {
                    PedID=playerPedID,
                    
                }.Pack(outgoingMessage);
                MainNetServer.SendMessage(outgoingMessage, clients, NetDeliveryMethod.ReliableOrdered, 0);
            }

            Client localClient = Util.GetClientByID( nethandle);
            if (localClient == null)
            {
                return;
            }

            Clients.Remove(localClient.NetID);

            if (RunningResource != null)
            {
                RunningResource.InvokePlayerDisconnected(localClient);
            }
            else
            {
                Program.Logger.Info($"Player {localClient.Player.Username} disconnected! ID:{playerPedID}");
            }
        }

        #region SyncEntities
        private static void PedStateSync(Packets.PedStateSync packet,long ClientID)
        {
            

            Client client = Util.GetClientByID(ClientID);
            if (client == null)
            {
                return;
            }


            NetOutgoingMessage outgoingMessage = MainNetServer.CreateMessage();
            packet.Pack(outgoingMessage);
            foreach (var c in Clients.Values)
            {
                if (c.NetID==client.NetID) { continue; }
                MainNetServer.SendMessage(outgoingMessage, c.Connection, NetDeliveryMethod.UnreliableSequenced, (byte)ConnectionChannel.PedSync);
            }
            if (RunningResource != null && packet.ID==client.Player.PedID)
            {
                RunningResource.InvokePlayerUpdate(client);
            }
        }
        private static void VehicleStateSync(Packets.VehicleStateSync packet, long ClientID)
        {
            Client client = Util.GetClientByID(ClientID);
            if (client == null)
            {
                return;
            }

            // Save the new data
            if (packet.Passengers.ContainsValue(client.Player.PedID))
            {
                client.Player.VehicleID = packet.ID;
            }

            foreach (var c in Clients.Values)
            {
                if (c.NetID==client.NetID) { continue; }
                NetOutgoingMessage outgoingMessage = MainNetServer.CreateMessage();
                packet.Pack(outgoingMessage);
                MainNetServer.SendMessage(outgoingMessage, c.Connection, NetDeliveryMethod.UnreliableSequenced, (byte)ConnectionChannel.PedSync);
            }
        }
        private static void PedSync(Packets.PedSync packet, long ClientID)
        {
            Client client = Util.GetClientByID(ClientID);
            if (client == null)
            {
                return;
            }
            bool isPlayer = packet.ID==client.Player.PedID;
            if (isPlayer) { client.Player.Position=packet.Position; }
            NetOutgoingMessage outgoingMessage = MainNetServer.CreateMessage();
            packet.Pack(outgoingMessage);
            foreach (var c in Clients.Values)
            {

                // Don't send data back
                if (c.NetID==client.NetID) { continue; }

                // Check streaming distance
                if (isPlayer)
                {
                    if ((MainSettings.PlayerStreamingDistance!=-1)&&(packet.Position.DistanceTo(c.Player.Position)>MainSettings.PlayerStreamingDistance))
                    {
                        continue;
                    }
                }
                else if ((MainSettings.NpcStreamingDistance!=-1)&&(packet.Position.DistanceTo(c.Player.Position)>MainSettings.NpcStreamingDistance))
                {
                    continue;
                }
                MainNetServer.SendMessage(outgoingMessage,c.Connection, NetDeliveryMethod.UnreliableSequenced, (byte)ConnectionChannel.PedSync);
            }

            if (RunningResource != null && isPlayer)
            {
                RunningResource.InvokePlayerUpdate(client);
            }
        }
        private static void VehicleSync(Packets.VehicleSync packet, long ClientID)
        {
            Client client = Util.GetClientByID(ClientID);
            if (client == null)
            {
                return;
            }
            bool isPlayer = packet.ID==client.Player.VehicleID;
            foreach (var c in Clients.Values)
            {
                if (c.NetID==client.NetID) { continue; }
                if (isPlayer)
                {
                    // Player's vehicle
                    if ((MainSettings.PlayerStreamingDistance!=-1)&&(packet.Position.DistanceTo(c.Player.Position)>MainSettings.PlayerStreamingDistance))
                    {
                        continue;
                    }

                }
                else if((MainSettings.NpcStreamingDistance!=-1)&&(packet.Position.DistanceTo(c.Player.Position)>MainSettings.NpcStreamingDistance))
                {
                    continue;
                }
                NetOutgoingMessage outgoingMessage = MainNetServer.CreateMessage();
                packet.Pack(outgoingMessage);
                MainNetServer.SendMessage(outgoingMessage, c.Connection, NetDeliveryMethod.UnreliableSequenced, (byte)ConnectionChannel.PedSync);
            }
        }
        private static void ProjectileSync(Packets.ProjectileSync packet, long ClientID)
        {
            Client client = Util.GetClientByID(ClientID);
            if (client == null)
            {
                return;
            }

            foreach (var c in Clients.Values)
            {
                if (c.NetID==client.NetID) { continue; }
                NetOutgoingMessage outgoingMessage = MainNetServer.CreateMessage();
                packet.Pack(outgoingMessage);
                MainNetServer.SendMessage(outgoingMessage, c.Connection, NetDeliveryMethod.UnreliableSequenced, (byte)ConnectionChannel.PedSync);
            }
        }

        #endregion
        // Send a message to targets or all players
        private static void SendChatMessage(Packets.ChatMessage packet, List<NetConnection> targets = null)
        {
            if (RunningResource != null)
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
                            Client = Clients.Values.Where(x => x.Player.Username == packet.Username).FirstOrDefault(),
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

                if (RunningResource.InvokeChatMessage(packet.Username, packet.Message))
                {
                    return;
                }
            }

            packet.Message = packet.Message.Replace("~", "");

            SendChatMessage(packet.Username, packet.Message, targets);

            Program.Logger.Info(packet.Username + ": " + packet.Message);
        }

        public static void SendChatMessage(string username, string message, List<NetConnection> targets = null)
        {
            NetOutgoingMessage outgoingMessage = MainNetServer.CreateMessage();

            new Packets.ChatMessage() { Username = username, Message = message }.Pack(outgoingMessage);

            MainNetServer.SendMessage(outgoingMessage, targets ?? MainNetServer.Connections, NetDeliveryMethod.ReliableOrdered, (byte)ConnectionChannel.Chat);
        }
        public static void SendChatMessage(string username, string message, NetConnection target)
        {
            SendChatMessage(username, message, new List<NetConnection>() { target });
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

        public static void RegisterEvent(string eventName, Action<EventContext> callback)
        {
            TriggerEvent ev = new(eventName);

            if (TriggerEvents.ContainsKey(ev))
            {
                throw new Exception("TriggerEvent \"" + ev.EventName + "\" was already been registered!");
            }

            TriggerEvents.Add(ev, callback);
        }
        public static void RegisterEvents<T>()
        {
            IEnumerable<MethodInfo> events = typeof(T).GetMethods().Where(method => method.GetCustomAttributes(typeof(TriggerEvent), false).Any());

            foreach (MethodInfo method in events)
            {
                TriggerEvent attribute = method.GetCustomAttribute<TriggerEvent>(true);

                RegisterEvent(attribute.EventName, (Action<EventContext>)Delegate.CreateDelegate(typeof(Action<EventContext>), method));
            }
        }
    }
}
