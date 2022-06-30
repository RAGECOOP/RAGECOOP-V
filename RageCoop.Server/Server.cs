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

    public class Server
    {
        public API API { get; private set; }
        internal BaseScript BaseScript { get; set; }=new BaseScript();
        internal readonly ServerSettings Settings;
        internal NetServer MainNetServer;

        internal readonly Dictionary<Command, Action<CommandContext>> Commands = new();
        internal readonly Dictionary<long,Client> Clients = new();
        
        private Dictionary<int,FileTransfer> InProgressFileTransfers=new();
        private Resources Resources;
        internal Logger Logger;
        private Security Security;
        private System.Timers.Timer _sendInfoTimer = new System.Timers.Timer(5000);
        private bool _stopping = false;
        private Thread _listenerThread;
        private Thread _announceThread;
        private Worker _worker;
        private readonly string _compatibleVersion = "V0_5";
        public Server(ServerSettings settings,Logger logger=null)
        {
            Settings = settings;
            if (settings==null) { throw new ArgumentNullException("Server settings cannot be null!"); }
            Logger=logger;
            if (Logger!=null) { Logger.LogLevel=Settings.LogLevel;}
            API=new API(this);
            Resources=new Resources(this);
            Security=new Security(Logger);
            
        }
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
            _worker=new Worker("ServerWorker");
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
                            Logger?.Info($"Your public IP is {info.Address}, announcing to master server...");
                        }
                        catch (Exception ex)
                        {
                            Logger?.Error(ex.InnerException?.Message ?? ex.Message);
                            return;
                        }
                        var realMaster = Settings.MasterServer=="[AUTO]" ? Util.DownloadString("https://ragecoop.online/stuff/masterserver") : Settings.MasterServer;
                        while (!_stopping)
                        {
                            string msg =
                                "{ " +
                                "\"address\": \"" + info.Address + "\", " +
                                "\"port\": \"" + Settings.Port + "\", " +
                                "\"name\": \"" + Settings.Name + "\", " +
                                "\"version\": \"" + _compatibleVersion.Replace("_", ".") + "\", " +
                                "\"players\": \"" + MainNetServer.ConnectionsCount + "\", " +
                                "\"maxPlayers\": \"" + Settings.MaxPlayers + "\"" +
                                " }";
                            HttpResponseMessage response = null;
                            try
                            {
                                response = await httpClient.PostAsync(realMaster, new StringContent(msg, Encoding.UTF8, "application/json"));
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
        }
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
            Logger?.Info("Listening for clients");
            while (!_stopping)
            {
                try
                {
                    ProcessMessage(MainNetServer.WaitMessage(200));
                }
                catch(Exception ex)
                {
                    Logger?.Error("Error processing message");
                    Logger?.Error(ex);
                }
            }
            Logger?.Info("Server is shutting down!");
            MainNetServer.Shutdown("Server is shutting down!");
            BaseScript.OnStop(); 
            Resources.StopAll();
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
                        if (message.ReadByte() != (byte)PacketTypes.Handshake)
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
                            Resources.SendTo(sender);
                            _worker.QueueJob(()=> API.Events.InvokePlayerConnected(sender));
                            if (sender.IsReady)
                            {
                                _worker.QueueJob(()=>API.Events.InvokePlayerReady(sender));
                            }
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
                            // Get sender client
                            if (!Clients.TryGetValue(message.SenderConnection.RemoteUniqueIdentifier, out sender))
                            {
                                throw new UnauthorizedAccessException("No client data found:"+message.SenderEndPoint);
                            }
                            switch (type)
                            {

                                #region SyncData

                                case PacketTypes.PedStateSync:
                                    {
                                        Packets.PedStateSync packet = new();
                                        packet.Unpack(data);

                                        PedStateSync(packet, sender);
                                        break;
                                    }
                                case PacketTypes.VehicleStateSync:
                                    {
                                        Packets.VehicleStateSync packet = new();
                                        packet.Unpack(data);

                                        VehicleStateSync(packet, sender);

                                        break;
                                    }
                                case PacketTypes.PedSync:
                                    {

                                        Packets.PedSync packet = new();
                                        packet.Unpack(data);

                                        PedSync(packet, sender);

                                    }
                                    break;
                                case PacketTypes.VehicleSync:
                                    {
                                        Packets.VehicleSync packet = new();
                                        packet.Unpack(data);

                                        VehicleSync(packet, sender);

                                    }
                                    break;
                                case PacketTypes.ProjectileSync:
                                    {

                                        Packets.ProjectileSync packet = new();
                                        packet.Unpack(data);
                                        ProjectileSync(packet, sender);

                                    }
                                    break;


                                #endregion

                                case PacketTypes.ChatMessage:
                                    {

                                        Packets.ChatMessage packet = new();
                                        packet.Unpack(data);

                                        _worker.QueueJob(()=>API.Events.InvokeOnChatMessage(packet, sender));
                                        SendChatMessage(packet,sender);
                                    }
                                    break;
                                case PacketTypes.CustomEvent:
                                    {
                                        Packets.CustomEvent packet = new Packets.CustomEvent();
                                        packet.Unpack(data);
                                        _worker.QueueJob(() => API.Events.InvokeCustomEventReceived(packet, sender));
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
                                                sender.IsReady=true;
                                                _worker.QueueJob(() => API.Events.InvokePlayerReady(sender));
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
                                            var toSend = MainNetServer.Connections.Exclude(message.SenderConnection);
                                            if (toSend.Count!=0)
                                            {
                                                var outgoingMessage = MainNetServer.CreateMessage();
                                                outgoingMessage.Write(btype);
                                                outgoingMessage.Write(len);
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
                                        Logger?.Error("Unhandled Data / Packet type");
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
                        if (message.ReadByte()==(byte)PacketTypes.PublicKeyRequest)
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
        object _sendPlayersLock=new object();
        internal void SendPlayerInfos()
        {
            lock (_sendPlayersLock)
            {
                foreach (Client c in Clients.Values)
                {
                    BaseScript.SetAutoRespawn(c,c.Config.EnableAutoRespawn);
                    MainNetServer.Connections.FindAll(x => x.RemoteUniqueIdentifier != c.NetID).ForEach(x =>
                    {
                        NetOutgoingMessage outgoingMessage = MainNetServer.CreateMessage();
                        new Packets.PlayerInfoUpdate()
                        {
                            PedID=c.ID,
                            Username=c.Username,
                            Latency=c.Latency,
                            Flags=c.Config.GetFlags(),
                            BlipColor=c.Config.BlipColor,
                        }.Pack(outgoingMessage);
                        MainNetServer.SendMessage(outgoingMessage, x, NetDeliveryMethod.ReliableSequenced, (byte)ConnectionChannel.Default);
                    });
                }

            }
        }

        private void DisconnectAndLog(NetConnection senderConnection,PacketTypes type, Exception e)
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
                passhash=BitConverter.ToString(Security.Decrypt(packet.PassHashEncrypted, connection.RemoteEndPoint)).Replace("-", String.Empty);
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
                Clients.Add(connection.RemoteUniqueIdentifier,
                    tmpClient = new Client(this)
                    {
                        NetID = connection.RemoteUniqueIdentifier,
                        Connection=connection,
                        Username=packet.Username,
                        ID=packet.PedID,
                        Player = new()
                        {
                            ID= packet.PedID,
                        }
                    }
                );;
            }
            
            Logger?.Debug($"Handshake sucess, Player:{packet.Username} PedID:{packet.PedID}");
            
        }

        // The connection has been approved, now we need to send all other players to the new player and the new player to all players
        private void SendPlayerConnectPacket(Client newClient)
        {

            // Send other players to this client
            Clients.Values.ForEach(target =>
            {
                if (target==newClient) { return; }
                NetOutgoingMessage outgoingMessage = MainNetServer.CreateMessage();
                new Packets.PlayerConnect()
                {
                    // NetHandle = targetNetHandle,
                    Username = target.Username,
                    PedID=target.ID,

                }.Pack(outgoingMessage);
                MainNetServer.SendMessage(outgoingMessage, newClient.Connection, NetDeliveryMethod.ReliableOrdered, 0);
            });
            // Send new client to all players
            var cons = MainNetServer.Connections.Exclude(newClient.Connection);
            if (cons.Count!=0)
            {
                NetOutgoingMessage outgoingMessage = MainNetServer.CreateMessage();
                new Packets.PlayerConnect()
                {
                    PedID=newClient.ID,
                    Username = newClient.Username
                }.Pack(outgoingMessage);

                MainNetServer.SendMessage(outgoingMessage,cons , NetDeliveryMethod.ReliableOrdered, 0);

            }
            
            Logger?.Info($"Player {newClient.Username} connected!");

            if (!string.IsNullOrEmpty(Settings.WelcomeMessage))
            {
                SendChatMessage(new Packets.ChatMessage() { Username = "Server", Message = Settings.WelcomeMessage }, null,new List<NetConnection>() { newClient.Connection });
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
                    PedID=localClient.ID,

                }.Pack(outgoingMessage);
                MainNetServer.SendMessage(outgoingMessage,cons , NetDeliveryMethod.ReliableOrdered, 0);
            }
            _worker.QueueJob(() => API.Events.InvokePlayerDisconnected(localClient));
            Logger?.Info($"Player {localClient.Username} disconnected! ID:{localClient.ID}");
            Clients.Remove(localClient.NetID);
            Security.RemoveConnection(localClient.Connection.RemoteEndPoint);
        }

        #region SyncEntities
        private void PedStateSync(Packets.PedStateSync packet, Client client)
        {
            


            
            foreach (var c in Clients.Values)
            {
                NetOutgoingMessage outgoingMessage = MainNetServer.CreateMessage();
                packet.Pack(outgoingMessage);
                if (c.NetID==client.NetID) { continue; }
                MainNetServer.SendMessage(outgoingMessage, c.Connection, NetDeliveryMethod.UnreliableSequenced, (byte)ConnectionChannel.PedSync);
            }
        }
        private void VehicleStateSync(Packets.VehicleStateSync packet, Client client)
        {
            // Save the new data
            if (packet.Passengers.ContainsValue(client.ID))
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
        private void PedSync(Packets.PedSync packet, Client client)
        {
            bool isPlayer = packet.ID==client.ID;
            if (isPlayer) 
            { 
                client.Player.Position=packet.Position;
                client.Player.Health=packet.Health ;
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
            bool isPlayer = packet.ID==client.Player.VehicleID;
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
        private void SendChatMessage(Packets.ChatMessage packet,Client sender=null, List<NetConnection> targets = null)
        {
            if (packet.Message.StartsWith('/'))
            {
                string[] cmdArgs = packet.Message.Split(" ");
                string cmdName = cmdArgs[0].Remove(0, 1);
                _worker.QueueJob(()=>API.Events.InvokeOnCommandReceived(cmdName, cmdArgs, sender));
                

                return;
            }
            packet.Message = packet.Message.Replace("~", "");
            SendChatMessage(packet.Username, packet.Message, targets);

            Logger?.Info(packet.Username + ": " + packet.Message);
        }

        internal void SendChatMessage(string username, string message, List<NetConnection> targets = null)
        {
            if (MainNetServer.Connections.Count==0) { return; }
            NetOutgoingMessage outgoingMessage = MainNetServer.CreateMessage();

            new Packets.ChatMessage() { Username = username, Message = message }.Pack(outgoingMessage);
            
            MainNetServer.SendMessage(outgoingMessage, targets ?? MainNetServer.Connections, NetDeliveryMethod.ReliableOrdered, (byte)ConnectionChannel.Chat);
        }
        internal void SendChatMessage(string username, string message, NetConnection target)
        {
            SendChatMessage(username, message, new List<NetConnection>() { target });
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

        internal void SendFile(string path,string name,Client client,Action<float> updateCallback=null)
        {
            int id = RequestFileID();
            var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            fs.Seek(0, SeekOrigin.Begin);
            var total = fs.Length;
            Logger?.Debug($"Initiating file transfer:{name}, {total}");
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
            Logger?.Debug($"All file chunks sent:{name}");
            InProgressFileTransfers.Remove(id);
        }
        private int RequestFileID()
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
        internal void Send(Packet p,Client client, ConnectionChannel channel = ConnectionChannel.Default, NetDeliveryMethod method = NetDeliveryMethod.UnreliableSequenced)
        {
            NetOutgoingMessage outgoingMessage = MainNetServer.CreateMessage();
            p.Pack(outgoingMessage);
            MainNetServer.SendMessage(outgoingMessage, client.Connection,method,(int)channel);
        }
    }
}
