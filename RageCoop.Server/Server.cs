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
using Lidgren.Network;
using System.Timers;
using System.Security.Cryptography;
using RageCoop.Server.Scripting;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace RageCoop.Server
{
    public struct IpInfo
    {
        [JsonProperty("ip")]
        public string Address { get; set; }
    }

    internal class Server
    {
        private static readonly string _compatibleVersion = "V0_4";

        public static readonly Settings MainSettings = Util.Read<Settings>("Settings.xml");
        public static NetServer MainNetServer;

        public static readonly Dictionary<Command, Action<CommandContext>> Commands = new();
        public static readonly Dictionary<TriggerEvent, Action<EventContext>> TriggerEvents = new();

        public static readonly Dictionary<long,Client> Clients = new();
        private static System.Timers.Timer SendLatencyTimer = new System.Timers.Timer(5000);
        
        private static Dictionary<int,FileTransfer> InProgressFileTransfers=new();
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
                EnableUPnP = false,
                AutoFlushSendQueue = true,
            };

            config.EnableMessageType(NetIncomingMessageType.ConnectionApproval);
            config.EnableMessageType(NetIncomingMessageType.ConnectionLatencyUpdated);

            MainNetServer = new NetServer(config);
            MainNetServer.Start();
            SendLatencyTimer.Elapsed+=((s,e) => { SendLatency(); });
            SendLatencyTimer.AutoReset=true;
            SendLatencyTimer.Enabled=true;
            Program.Logger.Info(string.Format("Server listening on {0}:{1}", config.LocalAddress.ToString(), config.Port));
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
                        var realMaster  = MainSettings.MasterServer=="[AUTO]" ? Util.DownloadString("https://ragecoop.online/stuff/masterserver") : MainSettings.MasterServer;
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
                                response = await httpClient.PostAsync(realMaster, new StringContent(msg, Encoding.UTF8, "application/json"));
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

            Resources.LoadAll();
            Listen();
        }

        private void Listen()
        {
            Program.Logger.Info("Listening for clients");
            Program.Logger.Info("Please use CTRL + C if you want to stop the server!");
            
            
            while (!Program.ReadyToStop)
            {
                while (true)
                {
                    ProcessMessage(MainNetServer.WaitMessage(10));
                    MainNetServer.FlushSendQueue();
                }
            }

            Program.Logger.Warning("Server is shutting down!");
            Resources.UnloadAll();
            MainNetServer.Shutdown("Server is shutting down!");
            Program.Logger.Dispose();
        }

        private void ProcessMessage(NetIncomingMessage message)
        {
            if(message == null) { return; }
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


                            SendPlayerDisconnectPacket(nethandle);
                        }
                        else if (status == NetConnectionStatus.Connected)
                        {
                            SendPlayerConnectPacket(message.SenderConnection);
                            Resources.SendTo(Util.GetClientByNetID(message.SenderConnection.RemoteUniqueIdentifier));
                        }
                        break;
                    }
                case NetIncomingMessageType.Data:

                    {
                        // Get packet type
                        byte btype = message.ReadByte();
                        var type = (PacketTypes)btype;
                        int len = message.ReadInt32();
                        byte[] data = message.ReadBytes(len);
                        try
                        {

                            switch (type)
                            {

                                #region SyncData

                                case PacketTypes.PedStateSync:
                                    {
                                        Packets.PedStateSync packet = new();
                                        packet.Unpack(data);

                                        PedStateSync(packet, message.SenderConnection.RemoteUniqueIdentifier);
                                        break;
                                    }
                                case PacketTypes.VehicleStateSync:
                                    {
                                        Packets.VehicleStateSync packet = new();
                                        packet.Unpack(data);

                                        VehicleStateSync(packet, message.SenderConnection.RemoteUniqueIdentifier);

                                        break;
                                    }
                                case PacketTypes.PedSync:
                                    {

                                        Packets.PedSync packet = new();
                                        packet.Unpack(data);

                                        PedSync(packet, message.SenderConnection.RemoteUniqueIdentifier);

                                    }
                                    break;
                                case PacketTypes.VehicleSync:
                                    {
                                        Packets.VehicleSync packet = new();
                                        packet.Unpack(data);

                                        VehicleSync(packet, message.SenderConnection.RemoteUniqueIdentifier);

                                    }
                                    break;
                                case PacketTypes.ProjectileSync:
                                    {

                                        Packets.ProjectileSync packet = new();
                                        packet.Unpack(data);
                                        ProjectileSync(packet, message.SenderConnection.RemoteUniqueIdentifier);

                                    }
                                    break;


                                #endregion

                                case PacketTypes.ChatMessage:
                                    {
                                        try
                                        {

                                            Packets.ChatMessage packet = new();
                                            packet.Unpack(data);

                                            API.Events.InvokeOnChatMessage(packet, message.SenderConnection);
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
                                        Packets.NativeResponse packet = new();
                                        packet.Unpack(data);

                                        Client client = Util.GetClientByNetID(message.SenderConnection.RemoteUniqueIdentifier);
                                        if (client != null)
                                        {
                                            if (client.Callbacks.ContainsKey(packet.ID))
                                            {
                                                client.Callbacks[packet.ID].Invoke(packet.Args[0]);
                                                client.Callbacks.Remove(packet.ID);
                                            }
                                        }
                                    }
                                    break;
                                case PacketTypes.ServerClientEvent:
                                    {
                                        Packets.ServerClientEvent packet = new Packets.ServerClientEvent();
                                        packet.Unpack(data);

                                        long senderNetHandle = message.SenderConnection.RemoteUniqueIdentifier;
                                        Client client = null;
                                        lock (Clients)
                                        {
                                            client = Util.GetClientByNetID(senderNetHandle);
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
                                    break;
                                case PacketTypes.FileTransferComplete:
                                    {
                                        Packets.FileTransferComplete packet = new Packets.FileTransferComplete();
                                        packet.Unpack(data);
                                        FileTransfer toRemove;
                                        
                                        // Cancel the download if it's in progress
                                        if (InProgressFileTransfers.TryGetValue(packet.ID,out toRemove))
                                        {
                                            toRemove.Cancel=true;
                                            if (toRemove.Name=="Resources.zip")
                                            {
                                                Clients[message.SenderConnection.RemoteUniqueIdentifier].IsReady=true;
                                            }
                                        }
                                    }
                                    break;
                                default:
                                    if (type.IsSyncEvent())
                                    {
                                        // Sync Events
                                        try
                                        {
                                            var outgoingMessage = MainNetServer.CreateMessage();
                                            outgoingMessage.Write(btype);
                                            outgoingMessage.Write(len);
                                            outgoingMessage.Write(data);
                                            var toSend = MainNetServer.Connections.Exclude(message.SenderConnection);
                                            if (toSend.Count!=0)
                                            {
                                                MainNetServer.SendMessage(outgoingMessage, toSend, NetDeliveryMethod.UnreliableSequenced, (byte)ConnectionChannel.SyncEvents);
                                            }
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
                        }
                        catch (Exception e)
                        {
                            DisconnectAndLog(message.SenderConnection, type, e);
                        }
                        break;
                    }
                case NetIncomingMessageType.ConnectionLatencyUpdated:
                    {
                        Client client = Util.GetClientByNetID(message.SenderConnection.RemoteUniqueIdentifier);
                        if (client != null)
                        {
                            client.Player.Latency = message.ReadFloat();
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

        private void SendLatency()
        {
            foreach (Client c in Clients.Values)
            {
                MainNetServer.Connections.FindAll(x => x.RemoteUniqueIdentifier != c.NetID).ForEach(x =>
                {
                    NetOutgoingMessage outgoingMessage = MainNetServer.CreateMessage();
                    new Packets.PlayerInfoUpdate()
                    {
                        PedID=c.Player.PedID,
                        Username=c.Player.Username,
                        Latency=c.Player.Latency,
                    }.Pack(outgoingMessage);
                    MainNetServer.SendMessage(outgoingMessage, x, NetDeliveryMethod.ReliableSequenced, (byte)ConnectionChannel.Default);
                });
            }

        }

        private void DisconnectAndLog(NetConnection senderConnection,PacketTypes type, Exception e)
        {
            Program.Logger.Error($"Error receiving a packet of type {type}");
            Program.Logger.Error(e.Message);
            Program.Logger.Error(e.StackTrace);
            senderConnection.Disconnect(e.Message);
        }

        #region -- SYNC --
        // Before we approve the connection, we must shake hands
        private void GetHandshake(NetConnection connection, Packets.Handshake packet)
        {
            Program.Logger.Debug("New handshake from: [Name: " + packet.Username + " | Address: " + connection.RemoteEndPoint.Address.ToString() + "]");

            if (!packet.ModVersion.StartsWith(_compatibleVersion))
            {
                connection.Deny($"RAGECOOP version {_compatibleVersion.Replace('_', '.')}.x required!");
                return;
            }
            if (string.IsNullOrWhiteSpace(packet.Username))
            {
                connection.Deny("Username is empty or contains spaces!");
                return;
            }
            if (packet.Username.Any(p => !char.IsLetterOrDigit(p) && !(p == '_') && !(p=='-')))
            {
                connection.Deny("Username contains special chars!");
                return;
            }
            if (Clients.Values.Any(x => x.Player.Username.ToLower() == packet.Username.ToLower()))
            {
                connection.Deny("Username is already taken!");
                return;
            }

            var args = new HandshakeEventArgs()
            {
                EndPoint=connection.RemoteEndPoint,
                ID=packet.PedID,
                Username=packet.Username
            };
            API.Events.InvokePlayerHandshake(args);
            if (args.Cancel)
            {
                connection.Deny(args.DenyReason);
                return;
            }



            Client tmpClient;

            // Add the player to Players
            lock (Clients)
            {
                Clients.Add(connection.RemoteUniqueIdentifier,
                    tmpClient = new Client()
                    {
                        NetID = connection.RemoteUniqueIdentifier,
                        Connection=connection,
                        Player = new()
                        {
                            Username = packet.Username,
                            PedID=packet.PedID,
                        }
                    }
                );;
            }
            
            Program.Logger.Debug($"Handshake sucess, Player:{packet.Username} PedID:{packet.PedID}");
            NetOutgoingMessage outgoingMessage = MainNetServer.CreateMessage();

            // Create a new handshake packet
            new Packets.Handshake()
            {
                PedID = packet.PedID,
                Username = string.Empty,
                ModVersion = string.Empty,
            }.Pack(outgoingMessage);
            // Accept the connection and send back a new handshake packet with the connection ID
            connection.Approve(outgoingMessage);
        }

        // The connection has been approved, now we need to send all other players to the new player and the new player to all players
        private static void SendPlayerConnectPacket(NetConnection local)
        {
            Client localClient = Util.GetClientByNetID(local.RemoteUniqueIdentifier);
            if (localClient == null)
            {
                local.Disconnect("No data found!");
                return;
            }

            List<NetConnection> clients=MainNetServer.Connections.Exclude(local);
            // Send all players to local
            

            if (clients.Count > 0)
            {
               clients.ForEach(targetPlayer =>
                {
                    long targetNetHandle = targetPlayer.RemoteUniqueIdentifier;
                    NetOutgoingMessage outgoingMessage = MainNetServer.CreateMessage();

                    Client targetClient = Util.GetClientByNetID(targetNetHandle);
                    if (targetClient != null)
                    {
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
                    PedID=localClient.Player.PedID,
                    Username = localClient.Player.Username
                }.Pack(outgoingMessage);
                if(clients.Count > 0)
                {
                    MainNetServer.SendMessage(outgoingMessage, clients, NetDeliveryMethod.ReliableOrdered, 0);
                }
            }
            API.Events.InvokePlayerConnected(localClient);
            
            Program.Logger.Info($"Player {localClient.Player.Username} connected!");

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

            Client localClient = Util.GetClientByNetID( nethandle);
            if (localClient == null)
            {
                return;
            }

            Clients.Remove(localClient.NetID);

            API.Events.InvokePlayerDisconnected(localClient);
            Program.Logger.Info($"Player {localClient.Player.Username} disconnected! ID:{playerPedID}");

        }

        #region SyncEntities
        private static void PedStateSync(Packets.PedStateSync packet,long ClientID)
        {
            

            Client client = Util.GetClientByNetID(ClientID);
            if (client == null)
            {
                return;
            }


            
            foreach (var c in Clients.Values)
            {
                NetOutgoingMessage outgoingMessage = MainNetServer.CreateMessage();
                packet.Pack(outgoingMessage);
                if (c.NetID==client.NetID) { continue; }
                MainNetServer.SendMessage(outgoingMessage, c.Connection, NetDeliveryMethod.UnreliableSequenced, (byte)ConnectionChannel.PedSync);
            }
        }
        private static void VehicleStateSync(Packets.VehicleStateSync packet, long ClientID)
        {
            Client client = Util.GetClientByNetID(ClientID);
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
            Client client = Util.GetClientByNetID(ClientID);
            if (client == null)
            {
                return;
            }
            bool isPlayer = packet.ID==client.Player.PedID;
            if (isPlayer) { client.Player.Position=packet.Position; }
            
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

                NetOutgoingMessage outgoingMessage = MainNetServer.CreateMessage();
                packet.Pack(outgoingMessage);
                MainNetServer.SendMessage(outgoingMessage,c.Connection, NetDeliveryMethod.UnreliableSequenced, (byte)ConnectionChannel.PedSync);
            }
        }
        private static void VehicleSync(Packets.VehicleSync packet, long ClientID)
        {
            Client client = Util.GetClientByNetID(ClientID);
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
            Client client = Util.GetClientByNetID(ClientID);
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
        public static void SendFile(string path,string name,Client client,Action<float> updateCallback=null)
        {
            int id = RequestFileID();
            var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            fs.Seek(0, SeekOrigin.Begin);
            var total = fs.Length;
            Program.Logger.Debug($"Initiating file transfer:{name}, {total}");
            FileTransfer transfer = new()
            {
                ID=id,
                Name = name,
            };
            InProgressFileTransfers.Add(id,transfer);
            Send(
            new Packets.FileTransferRequest()
            {
                FileLength= total,
                Name=name,
                ID=id,
            },
            client, ConnectionChannel.File, NetDeliveryMethod.ReliableOrdered
            );
            int read = 0;
            int thisRead = 0;
            do
            {
                // 4 KB chunk
                byte[] chunk = new byte[4096];
                read += thisRead=fs.Read(chunk, 0, 4096);
                if (thisRead!=chunk.Length)
                {
                    if (thisRead==0) { break; }
                    Program.Logger.Trace($"Purging chunk:{thisRead}");
                    Array.Resize(ref chunk, thisRead);
                }
                Send(
                new Packets.FileTransferChunk()
                {
                    ID=id,
                    FileChunk=chunk,
                },
                client, ConnectionChannel.File, NetDeliveryMethod.ReliableOrdered);

                MainNetServer.FlushSendQueue();
                transfer.Progress=read/fs.Length;
                if (updateCallback!=null) { updateCallback(transfer.Progress);}

            } while (thisRead>0);
            Send(
                new Packets.FileTransferComplete()
                {
                    ID= id,
                }
                , client, ConnectionChannel.File, NetDeliveryMethod.ReliableOrdered
            ); 
            fs.Close();
            fs.Dispose();
            Program.Logger.Debug($"All file chunks sent:{name}");
            InProgressFileTransfers.Remove(id);
        }
        public static int RequestFileID()
        {
            int ID = 0;
            while ((ID==0)
                || InProgressFileTransfers.ContainsKey(ID))
            {
                byte[] rngBytes = new byte[4];

                RandomNumberGenerator.Create().GetBytes(rngBytes);

                // Convert the bytes into an integer
                ID = BitConverter.ToInt32(rngBytes, 0);
            }
            return ID;
        }

        /// <summary>
        /// Pack the packet then send to server.
        /// </summary>
        /// <param name="p"></param>
        /// <param name="channel"></param>
        /// <param name="method"></param>
        public static void Send(Packet p,Client client, ConnectionChannel channel = ConnectionChannel.Default, NetDeliveryMethod method = NetDeliveryMethod.UnreliableSequenced)
        {
            NetOutgoingMessage outgoingMessage = MainNetServer.CreateMessage();
            p.Pack(outgoingMessage);
            MainNetServer.SendMessage(outgoingMessage, client.Connection,method,(int)channel);
        }
    }
}
