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
using RageCoop.Core.Scripting;

namespace RageCoop.Server
{
    internal struct IpInfo
    {
        [JsonProperty("ip")]
        public string Address { get; set; }

        [JsonProperty("country")]
        public string Country { get; set; }
    }

    /// <summary>
    /// The instantiable RageCoop server class
    /// </summary>
    public class Server
    {
        /// <summary>
        /// The API for controlling server and hooking events.
        /// </summary>
        public API API { get; private set; }
        internal readonly BaseScript BaseScript;
        internal readonly ServerSettings Settings;
        internal NetServer MainNetServer;
        internal ServerEntities Entities;

        internal readonly Dictionary<Command, Action<CommandContext>> Commands = new();
        internal readonly Dictionary<long,Client> Clients = new();
        internal readonly Dictionary<string, Client> ClientsByName = new();
        internal Client _hostClient;

        private Dictionary<int,FileTransfer> InProgressFileTransfers=new();
        private Resources Resources;
        internal Logger Logger;
        private Security Security;
        private System.Timers.Timer _sendInfoTimer = new System.Timers.Timer(5000);
        private bool _stopping = false;
        private Thread _listenerThread;
        private Thread _announceThread;
        private Worker _worker;
        private Dictionary<int,Action<PacketType,byte[]>> PendingResponses=new();
        internal Dictionary<PacketType, Func<byte[],Client,Packet>> RequestHandlers=new();
        private readonly string _compatibleVersion = "V0_5";
        /// <summary>
        /// Instantiate a server.
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="logger"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public Server(ServerSettings settings,Logger logger=null)
        {
            Settings = settings;
            if (settings==null) { throw new ArgumentNullException("Server settings cannot be null!"); }
            Logger=logger;
            if (Logger!=null) { Logger.LogLevel=Settings.LogLevel;}
            API=new API(this);
            Resources=new Resources(this);
            Security=new Security(Logger);
            Entities=new ServerEntities(this);
            BaseScript=new BaseScript(this);
        }
        /// <summary>
        /// Spawn threads and start the server
        /// </summary>
        public void Start()
        {
            Logger?.Info("================");
            Logger?.Info($"Server bound to: 0.0.0.0:{Settings.Port}");
            Logger?.Info($"Server version: {Assembly.GetCallingAssembly().GetName().Version}");
            Logger?.Info($"Compatible RAGECOOP versions: {_compatibleVersion.Replace('_', '.')}.x");
            Logger?.Info("================");

            // 623c92c287cc392406e7aaaac1c0f3b0 = RAGECOOP
            NetPeerConfiguration config = new("623c92c287cc392406e7aaaac1c0f3b0")
            {
                Port = Settings.Port,
                MaximumConnections = Settings.MaxPlayers,
                EnableUPnP = false,
                AutoFlushSendQueue = true
            };

            config.EnableMessageType(NetIncomingMessageType.ConnectionApproval);
            config.EnableMessageType(NetIncomingMessageType.ConnectionLatencyUpdated);
            config.EnableMessageType(NetIncomingMessageType.UnconnectedData);

            MainNetServer = new NetServer(config);
            MainNetServer.Start();
            _worker=new Worker("ServerWorker",Logger);
            _sendInfoTimer.Elapsed+=(s, e) => { SendPlayerInfos(); };
            _sendInfoTimer.AutoReset=true;
            _sendInfoTimer.Enabled=true;
            Logger?.Info(string.Format("Server listening on {0}:{1}", config.LocalAddress.ToString(), config.Port));
            if (Settings.AnnounceSelf)
            {

                #region -- MASTERSERVER --
                _announceThread=new Thread(async () =>
                {
                    try
                    {
                        // TLS only
                        ServicePointManager.Expect100Continue = true;
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls13 | SecurityProtocolType.Tls12 ;
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
                            Logger?.Info($"Your public IP is {info.Address}, announcing to master server...");
                        }
                        catch (Exception ex)
                        {
                            Logger?.Error(ex.InnerException?.Message ?? ex.Message);
                            return;
                        }
                        while (!_stopping)
                        {
                            string msg =
                                "{ " +
                                "\"address\": \"" + info.Address + "\", " +
                                "\"port\": \"" + Settings.Port + "\", " +
                                "\"country\": \"" + info.Country + "\", " +
                                "\"name\": \"" + Settings.Name + "\", " +
                                "\"version\": \"" + _compatibleVersion.Replace("_", ".") + "\", " +
                                "\"players\": \"" + MainNetServer.ConnectionsCount + "\", " +
                                "\"maxPlayers\": \"" + Settings.MaxPlayers + "\", " +
                                "\"description\": \"" + Settings.Description + "\", " +
                                "\"website\": \"" + Settings.Website + "\", " +
                                "\"gameMode\": \"" + Settings.GameMode + "\", " +
                                "\"language\": \"" + Settings.Language + "\"" +
                                " }";
                            HttpResponseMessage response = null;
                            try
                            {
                                var realUrl = Util.GetFinalRedirect(Settings.MasterServer);
                                response = await httpClient.PostAsync(realUrl, new StringContent(msg, Encoding.UTF8, "application/json"));
                            }
                            catch (Exception ex)
                            {
                                Logger?.Error($"MasterServer: {ex.Message}");

                                // Sleep for 5s
                                Thread.Sleep(5000);
                                continue;
                            }

                            if (response == null)
                            {
                                Logger?.Error("MasterServer: Something went wrong!");
                            }
                            else if (response.StatusCode != HttpStatusCode.OK)
                            {
                                if (response.StatusCode == HttpStatusCode.BadRequest)
                                {
                                    string requestContent = await response.Content.ReadAsStringAsync();
                                    Logger?.Error($"MasterServer: [{(int)response.StatusCode}], {requestContent}");
                                }
                                else
                                {
                                    Logger?.Error($"MasterServer: [{(int)response.StatusCode}]");
                                    Logger?.Error($"MasterServer: [{await response.Content.ReadAsStringAsync()}]");
                                }
                            }

                            // Sleep for 10s
                            for(int i = 0; i<10; i++)
                            {
                                if (_stopping)
                                {
                                    break;
                                }
                                else
                                {
                                    Thread.Sleep(1000);
                                }
                            }
                        }
                    }
                    catch (HttpRequestException ex)
                    {
                        Logger?.Error($"MasterServer: {ex.InnerException.Message}");
                    }
                    catch (Exception ex)
                    {
                        Logger?.Error($"MasterServer: {ex.Message}");
                    }
                });
                _announceThread.Start();
                #endregion
            }
            BaseScript.API=API;
            BaseScript.OnStart();
            Resources.LoadAll();
            _listenerThread=new Thread(() => Listen());
            _listenerThread.Start();
            Logger?.Info("Listening for clients");
        }
        /// <summary>
        /// Terminate threads and stop the server
        /// </summary>
        public void Stop()
        {
            _stopping = true;
            _sendInfoTimer.Stop();
            _sendInfoTimer.Enabled=false;
            _sendInfoTimer.Dispose();
            Logger?.Flush();
            _listenerThread?.Join();
            _announceThread?.Join();
            _worker.Dispose();
        }
        private void Listen()
        {
            NetIncomingMessage msg=null;
            while (!_stopping)
            {
                try
                {
                    msg=MainNetServer.WaitMessage(200);
                    ProcessMessage(msg);
                }
                catch(Exception ex)
                {
                    Logger?.Error("Error processing message");
                    Logger?.Error(ex);
                    if (msg!=null)
                    {
                        DisconnectAndLog(msg.SenderConnection, PacketType.Unknown, ex);
                    }
                }
            }
            Logger?.Info("Server is shutting down!");
            MainNetServer.Shutdown("Server is shutting down!");
            BaseScript.OnStop(); 
            Resources.UnloadAll();
        }

        private void ProcessMessage(NetIncomingMessage message)
        {
            Client sender;
            if (message == null) { return; }
            switch (message.MessageType)
            {
                case NetIncomingMessageType.ConnectionApproval:
                    {
                        Logger?.Info($"New incoming connection from: [{message.SenderConnection.RemoteEndPoint}]");
                        if (message.ReadByte() != (byte)PacketType.Handshake)
                        {
                            Logger?.Info($"IP [{message.SenderConnection.RemoteEndPoint.Address}] was blocked, reason: Wrong packet!");
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
                                Logger?.Info($"IP [{message.SenderConnection.RemoteEndPoint.Address}] was blocked, reason: {e.Message}");
                                Logger?.Error(e);
                                message.SenderConnection.Deny(e.Message);
                            }
                        }
                        break;
                    }
                case NetIncomingMessageType.StatusChanged:
                    {
                        // Get sender client
                        if (!Clients.TryGetValue(message.SenderConnection.RemoteUniqueIdentifier, out sender))
                        {
                            break;
                        }
                        NetConnectionStatus status = (NetConnectionStatus)message.ReadByte();

                        if (status == NetConnectionStatus.Disconnected)
                        {

                            SendPlayerDisconnectPacket(sender);
                        }
                        else if (status == NetConnectionStatus.Connected)
                        {
                            SendPlayerConnectPacket(sender);
                            _worker.QueueJob(() => API.Events.InvokePlayerConnected(sender));
                            Resources.SendTo(sender);
                        }
                        break;
                    }
                case NetIncomingMessageType.Data:
                    {
                        
                        // Get sender client
                        if (Clients.TryGetValue(message.SenderConnection.RemoteUniqueIdentifier, out sender))
                        {
                            // Get packet type
                            var type = (PacketType)message.ReadByte();
                            switch (type)
                            {
                                case PacketType.Response:
                                    {
                                        int id = message.ReadInt32();
                                        if (PendingResponses.TryGetValue(id, out var callback))
                                        {
                                            callback((PacketType)message.ReadByte(), message.ReadBytes(message.ReadInt32()));
                                            PendingResponses.Remove(id);
                                        }
                                        break;
                                    }
                                case PacketType.Request:
                                    {
                                        int id = message.ReadInt32();
                                        if (RequestHandlers.TryGetValue((PacketType)message.ReadByte(), out var handler))
                                        {
                                            var response=MainNetServer.CreateMessage();
                                            response.Write((byte)PacketType.Response);
                                            response.Write(id);
                                            handler(message.ReadBytes(message.ReadInt32()),sender).Pack(response);
                                            MainNetServer.SendMessage(response,message.SenderConnection,NetDeliveryMethod.ReliableOrdered);
                                        }
                                        break;
                                    }
                                default:
                                    {
                                        byte[] data = message.ReadBytes(message.ReadInt32());
                                        if (type.IsSyncEvent())
                                        {
                                            // Sync Events
                                            try
                                            {
                                                var toSend = MainNetServer.Connections.Exclude(message.SenderConnection);
                                                if (toSend.Count!=0)
                                                {
                                                    var outgoingMessage = MainNetServer.CreateMessage();
                                                    outgoingMessage.Write((byte)type);
                                                    outgoingMessage.Write(data.Length);
                                                    outgoingMessage.Write(data);
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
                                            HandlePacket(type, data, sender);
                                        }
                                        break;
                                    }
                            }
                            
                        }
                        break;
                    }
                case NetIncomingMessageType.ConnectionLatencyUpdated:
                    {
                        // Get sender client
                        if (!Clients.TryGetValue(message.SenderConnection.RemoteUniqueIdentifier, out sender))
                        {
                            break;
                        }
                        if (sender != null)
                        {
                            sender.Latency = message.ReadFloat();
                        }
                    }
                    break;
                case NetIncomingMessageType.ErrorMessage:
                    Logger?.Error(message.ReadString());
                    break;
                case NetIncomingMessageType.WarningMessage:
                    Logger?.Warning(message.ReadString());
                    break;
                case NetIncomingMessageType.DebugMessage:
                case NetIncomingMessageType.VerboseDebugMessage:
                    Logger?.Debug(message.ReadString());
                    break;
                case NetIncomingMessageType.UnconnectedData:
                    {
                        if (message.ReadByte()==(byte)PacketType.PublicKeyRequest)
                        {
                            var msg = MainNetServer.CreateMessage();
                            var p=new Packets.PublicKeyResponse();
                            Security.GetPublicKey(out p.Modulus,out p.Exponent);
                            p.Pack(msg);
                            Logger?.Debug($"Sending public key to {message.SenderEndPoint}, length:{msg.LengthBytes}");
                            MainNetServer.SendUnconnectedMessage(msg, message.SenderEndPoint);
                        }
                    }
                    break;
                default:
                    Logger?.Error(string.Format("Unhandled type: {0} {1} bytes {2} | {3}", message.MessageType, message.LengthBytes, message.DeliveryMethod, message.SequenceChannel));
                    break;
            }

            MainNetServer.Recycle(message);
        }
        internal void QueueJob(Action job)
        {
            _worker.QueueJob(job);
        }
        private void HandlePacket(PacketType type,byte[] data,Client sender)
        {

            try
            {
                
                switch (type)
                {
                    #region INTERVAL-SYNC
                    case PacketType.PedSync:
                        {

                            Packets.PedSync packet = new();
                            packet.Unpack(data);

                            PedSync(packet, sender);

                        }
                        break;
                    case PacketType.VehicleSync:
                        {
                            Packets.VehicleSync packet = new();
                            packet.Unpack(data);

                            VehicleSync(packet, sender);

                        }
                        break;
                    case PacketType.ProjectileSync:
                        {

                            Packets.ProjectileSync packet = new();
                            packet.Unpack(data);
                            ProjectileSync(packet, sender);

                        }
                        break;


                    #endregion

                    case PacketType.ChatMessage:
                        {

                            Packets.ChatMessage packet = new();
                            packet.Unpack(data);

                            _worker.QueueJob(() => API.Events.InvokeOnChatMessage(packet, sender));
                            ChatMessageReceived(packet.Username,packet.Message, sender);
                        }
                        break;
                    case PacketType.CustomEvent:
                        {
                            Packets.CustomEvent packet = new Packets.CustomEvent();
                            packet.Unpack(data);
                            _worker.QueueJob(() => API.Events.InvokeCustomEventReceived(packet, sender));
                        }
                        break;

                    default:
                        Logger?.Error("Unhandled Data / Packet type");
                        break;
                }
            }
            catch (Exception e)
            {
                DisconnectAndLog(sender.Connection, type, e);
            }
        }
        object _sendPlayersLock=new object();
        internal void SendPlayerInfos()
        {
            lock (_sendPlayersLock)
            {
                foreach (Client c in Clients.Values)
                {
                    MainNetServer.Connections.FindAll(x => x.RemoteUniqueIdentifier != c.NetID).ForEach(x =>
                    {
                        NetOutgoingMessage outgoingMessage = MainNetServer.CreateMessage();
                        new Packets.PlayerInfoUpdate()
                        {
                            PedID=c.Player.ID,
                            Username=c.Username,
                            Latency=c.Latency,
                        }.Pack(outgoingMessage);
                        MainNetServer.SendMessage(outgoingMessage, x, NetDeliveryMethod.ReliableSequenced, (byte)ConnectionChannel.Default);
                    });
                }

            }
        }

        private void DisconnectAndLog(NetConnection senderConnection,PacketType type, Exception e)
        {
            Logger?.Error($"Error receiving a packet of type {type}");
            Logger?.Error(e.Message);
            Logger?.Error(e.StackTrace);
            senderConnection.Disconnect(e.Message);
        }

        #region -- SYNC --
        // Before we approve the connection, we must shake hands
        private void GetHandshake(NetConnection connection, Packets.Handshake packet)
        {
            Logger?.Debug("New handshake from: [Name: " + packet.Username + " | Address: " + connection.RemoteEndPoint.Address.ToString() + "]");

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
            if (Clients.Values.Any(x => x.Username.ToLower() == packet.Username.ToLower()))
            {
                connection.Deny("Username is already taken!");
                return;
            }
            string passhash;
            try
            {
                Security.AddConnection(connection.RemoteEndPoint, packet.AesKeyCrypted,packet.AesIVCrypted);
                passhash=Security.Decrypt(packet.PassHashEncrypted, connection.RemoteEndPoint).ToHexString();
            }
            catch (Exception ex)
            {
                Logger?.Error($"Cannot process handshake packet from {connection.RemoteEndPoint}");
                Logger?.Error(ex);
                connection.Deny("Malformed handshak packet!");
                return;
            }
            var args = new HandshakeEventArgs()
            {
                EndPoint=connection.RemoteEndPoint,
                ID=packet.PedID,
                Username=packet.Username,
                PasswordHash=passhash,
            };
            API.Events.InvokePlayerHandshake(args);
            if (args.Cancel)
            {
                connection.Deny(args.DenyReason);
                return;
            }


            connection.Approve();

            Client tmpClient;

            // Add the player to Players
            lock (Clients)
            {
                var player = new ServerPed(this)
                {
                    ID= packet.PedID,
                };
                Entities.Add(player);
                Clients.Add(connection.RemoteUniqueIdentifier,
                    tmpClient = new Client(this)
                    {
                        NetID = connection.RemoteUniqueIdentifier,
                        Connection=connection,
                        Username=packet.Username,
                        Player = player
                    }
                );
                ClientsByName.Add(packet.Username, tmpClient);
                if (Clients.Count==1) { 
                    _hostClient=tmpClient;
                }
            }
            
            Logger?.Debug($"Handshake sucess, Player:{packet.Username} PedID:{packet.PedID}");
            
        }

        // The connection has been approved, now we need to send all other players to the new player and the new player to all players
        private void SendPlayerConnectPacket(Client newClient)
        {
            if (newClient==_hostClient)
            {
                _hostClient.SendCustomEvent(CustomEvents.IsHost, true);
            }

            // Send other players to this client
            Clients.Values.ForEach(target =>
            {
                if (target==newClient) { return; }
                NetOutgoingMessage outgoingMessage = MainNetServer.CreateMessage();
                new Packets.PlayerConnect()
                {
                    // NetHandle = targetNetHandle,
                    Username = target.Username,
                    PedID=target.Player.ID,

                }.Pack(outgoingMessage);
                MainNetServer.SendMessage(outgoingMessage, newClient.Connection, NetDeliveryMethod.ReliableOrdered, 0);
            });

            // Send all props to this player
            BaseScript.SendServerPropsTo( new(Entities.ServerProps.Values), new() { newClient});

            // Send all blips to this player
            BaseScript.SendServerBlipsTo(new(Entities.Blips.Values), new() { newClient});

            // Send new client to all players
            var cons = MainNetServer.Connections.Exclude(newClient.Connection);
            if (cons.Count!=0)
            {
                NetOutgoingMessage outgoingMessage = MainNetServer.CreateMessage();
                new Packets.PlayerConnect()
                {
                    PedID=newClient.Player.ID,
                    Username = newClient.Username
                }.Pack(outgoingMessage);

                MainNetServer.SendMessage(outgoingMessage,cons , NetDeliveryMethod.ReliableOrdered, 0);

            }
            
            Logger?.Info($"Player {newClient.Username} connected!");

            if (!string.IsNullOrEmpty(Settings.WelcomeMessage))
            {
                ChatMessageReceived("Server",Settings.WelcomeMessage , null);
            }
        }

        // Send all players a message that someone has left the server
        private void SendPlayerDisconnectPacket(Client localClient)
        {
            var cons = MainNetServer.Connections.Exclude(localClient.Connection);
            if (cons.Count!=0)
            {
                NetOutgoingMessage outgoingMessage = MainNetServer.CreateMessage();
                new Packets.PlayerDisconnect()
                {
                    PedID=localClient.Player.ID,

                }.Pack(outgoingMessage);
                MainNetServer.SendMessage(outgoingMessage,cons , NetDeliveryMethod.ReliableOrdered, 0);
            }
            Entities.CleanUp(localClient);
            _worker.QueueJob(() => API.Events.InvokePlayerDisconnected(localClient));
            Logger?.Info($"Player {localClient.Username} disconnected! ID:{localClient.Player.ID}");
            Clients.Remove(localClient.NetID);
            ClientsByName.Remove(localClient.Username);
            if (localClient==_hostClient)
            {

                _hostClient = Clients.Values.FirstOrDefault();
                _hostClient?.SendCustomEvent(CustomEvents.IsHost, true);
            }
            Security.RemoveConnection(localClient.Connection.RemoteEndPoint);
        }

        #region SyncEntities

        private void PedSync(Packets.PedSync packet, Client client)
        {
            _worker.QueueJob(() => Entities.Update(packet, client));

            bool isPlayer = packet.ID==client.Player.ID;
            if (isPlayer) 
            { 
                _worker.QueueJob(() => API.Events.InvokePlayerUpdate(client)); 
            }
            
            foreach (var c in Clients.Values)
            {

                // Don't send data back
                if (c.NetID==client.NetID) { continue; }

                // Check streaming distance
                if (isPlayer)
                {
                    if ((Settings.PlayerStreamingDistance!=-1)&&(packet.Position.DistanceTo(c.Player.Position)>Settings.PlayerStreamingDistance))
                    {
                        continue;
                    }
                }
                else if ((Settings.NpcStreamingDistance!=-1)&&(packet.Position.DistanceTo(c.Player.Position)>Settings.NpcStreamingDistance))
                {
                    continue;
                }

                NetOutgoingMessage outgoingMessage = MainNetServer.CreateMessage();
                packet.Pack(outgoingMessage);
                MainNetServer.SendMessage(outgoingMessage,c.Connection, NetDeliveryMethod.UnreliableSequenced, (byte)ConnectionChannel.PedSync);
            }
        }
        private void VehicleSync(Packets.VehicleSync packet, Client client)
        {
            _worker.QueueJob(() => Entities.Update(packet, client));
            bool isPlayer = packet.ID==client.Player?.LastVehicle?.ID;
            foreach (var c in Clients.Values)
            {
                if (c.NetID==client.NetID) { continue; }
                if (isPlayer)
                {
                    // Player's vehicle
                    if ((Settings.PlayerStreamingDistance!=-1)&&(packet.Position.DistanceTo(c.Player.Position)>Settings.PlayerStreamingDistance))
                    {
                        continue;
                    }

                }
                else if((Settings.NpcStreamingDistance!=-1)&&(packet.Position.DistanceTo(c.Player.Position)>Settings.NpcStreamingDistance))
                {
                    continue;
                }
                NetOutgoingMessage outgoingMessage = MainNetServer.CreateMessage();
                packet.Pack(outgoingMessage);
                MainNetServer.SendMessage(outgoingMessage, c.Connection, NetDeliveryMethod.UnreliableSequenced, (byte)ConnectionChannel.PedSync);
            }
        }
        private void ProjectileSync(Packets.ProjectileSync packet, Client client)
        {

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
        internal void ChatMessageReceived(string name, string message,Client sender=null)
        {
            if (message.StartsWith('/'))
            {
                string[] cmdArgs = message.Split(" ");
                string cmdName = cmdArgs[0].Remove(0, 1);
                _worker.QueueJob(()=>API.Events.InvokeOnCommandReceived(cmdName, cmdArgs, sender));
                return;
            }
            message = message.Replace("~", "");

            var msg=MainNetServer.CreateMessage();
            new Packets.ChatMessage()
            {
                Username=name,
                Message=message
            }.Pack(msg);

            if (sender==null)
            {
                // Sent by server
                MainNetServer.SendToAll(msg,NetDeliveryMethod.ReliableOrdered,(int)ConnectionChannel.Chat);
            }
            else
            {
                MainNetServer.SendToAll(msg,sender.Connection, NetDeliveryMethod.ReliableOrdered, (int)ConnectionChannel.Chat);
            }
            Logger?.Info(name + ": " + message);
        }
        internal void SendChatMessage(string name, string message, Client target)
        {
            var msg = MainNetServer.CreateMessage();
            new Packets.ChatMessage()
            {
                Username= name,
                Message=message,
            }.Pack(msg);
            MainNetServer.SendMessage(msg, target.Connection, NetDeliveryMethod.ReliableOrdered, (int)ConnectionChannel.Chat);
        }
        #endregion


        internal void RegisterCommand(string name, string usage, short argsLength, Action<CommandContext> callback)
        {
            Command command = new(name) { Usage = usage, ArgsLength = argsLength };

            if (Commands.ContainsKey(command))
            {
                throw new Exception("Command \"" + command.Name + "\" was already been registered!");
            }

            Commands.Add(command, callback);
        }
        internal void RegisterCommand(string name, Action<CommandContext> callback)
        {
            Command command = new(name);

            if (Commands.ContainsKey(command))
            {
                throw new Exception("Command \"" + command.Name + "\" was already been registered!");
            }

            Commands.Add(command, callback);
        }

        internal void RegisterCommands<T>()
        {
            IEnumerable<MethodInfo> commands = typeof(T).GetMethods().Where(method => method.GetCustomAttributes(typeof(Command), false).Any());

            foreach (MethodInfo method in commands)
            {
                Command attribute = method.GetCustomAttribute<Command>(true);

                RegisterCommand(attribute.Name, attribute.Usage, attribute.ArgsLength, (Action<CommandContext>)Delegate.CreateDelegate(typeof(Action<CommandContext>), method));
            }
        }
        internal T GetResponse<T>(Client client,Packet request, ConnectionChannel channel = ConnectionChannel.RequestResponse,int timeout=5000) where T:Packet, new()
        {
            if (Thread.CurrentThread==_listenerThread)
            {
                throw new InvalidOperationException("Cannot wait for response from the listener thread!");
            }
            var received=new AutoResetEvent(false);
            byte[] response=null;
            var id = NewRequestID();
            PendingResponses.Add(id, (type,p) =>
            {
                response=p;
                received.Set();
            });
            var msg = MainNetServer.CreateMessage();
            msg.Write((byte)PacketType.Request);
            msg.Write(id);
            request.Pack(msg);
            MainNetServer.SendMessage(msg,client.Connection,NetDeliveryMethod.ReliableOrdered,(int)channel);
            if (received.WaitOne(timeout))
            {
                var p = new T();
                p.Unpack(response);
                return p;
            }
            else
            {
                return null;
            }
        }
        internal void SendFile(string path,string name,Client client,Action<float> updateCallback=null)
        {
            SendFile(File.OpenRead(path), name,client,NewFileID(),updateCallback);
        }
        internal void SendFile(Stream stream, string name, Client client,int id=default, Action<float> updateCallback = null)
        {

            id = id ==default? NewFileID(): id ;
            stream.Seek(0, SeekOrigin.Begin);
            var total = stream.Length;
            if (GetResponse<Packets.FileTransferResponse>(client, new Packets.FileTransferRequest()
            {
                FileLength= total,
                Name=name,
                ID=id,
            }, ConnectionChannel.File)?.Response!=FileResponse.NeedToDownload)
            {
                Logger?.Info($"Skipping file transfer \"{name}\" to {client.Username}");
                stream.Close();
                stream.Dispose();
                return;
            }
            Logger?.Debug($"Initiating file transfer:{name}, {total}");
            FileTransfer transfer = new()
            {
                ID=id,
                Name = name,
            };
            InProgressFileTransfers.Add(id, transfer);
            int read = 0;
            int thisRead;
            do
            {
                // 4 KB chunk
                byte[] chunk = new byte[4096];
                read += thisRead=stream.Read(chunk, 0, 4096);
                if (thisRead!=chunk.Length)
                {
                    if (thisRead==0) { break; }
                    Logger?.Trace($"Purging chunk:{thisRead}");
                    Array.Resize(ref chunk, thisRead);
                }
                Send(
                new Packets.FileTransferChunk()
                {
                    ID=id,
                    FileChunk=chunk,
                },
                client, ConnectionChannel.File, NetDeliveryMethod.ReliableOrdered);
                transfer.Progress=read/stream.Length;
                if (updateCallback!=null) { updateCallback(transfer.Progress); }

            } while (thisRead>0);
            if (GetResponse<Packets.FileTransferResponse>(client, new Packets.FileTransferComplete()
            {
                ID= id,
            }, ConnectionChannel.File)?.Response!=FileResponse.Completed)
            {
                Logger.Warning($"File trasfer to {client.Username} failed: "+name);
            }
            stream.Close();
            stream.Dispose();
            Logger?.Debug($"All file chunks sent:{name}");
            InProgressFileTransfers.Remove(id);
        }
        internal int NewFileID()
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
        private int NewRequestID()
        {
            int ID = 0;
            while ((ID==0)
                || PendingResponses.ContainsKey(ID))
            {
                byte[] rngBytes = new byte[4];

                RandomNumberGenerator.Create().GetBytes(rngBytes);

                // Convert the bytes into an integer
                ID = BitConverter.ToInt32(rngBytes, 0);
            }
            return ID;
        }
        internal void Send(Packet p,Client client, ConnectionChannel channel = ConnectionChannel.Default, NetDeliveryMethod method = NetDeliveryMethod.UnreliableSequenced)
        {
            NetOutgoingMessage outgoingMessage = MainNetServer.CreateMessage();
            p.Pack(outgoingMessage);
            MainNetServer.SendMessage(outgoingMessage, client.Connection,method,(int)channel);
        }
    }
}
