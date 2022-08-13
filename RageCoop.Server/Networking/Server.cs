using System;
using System.Diagnostics;
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

    /// <summary>
    /// The instantiable RageCoop server class
    /// </summary>
    public partial class Server
    {
        /// <summary>
        /// The API for controlling server and hooking events.
        /// </summary>
        public API API { get; private set; }
        internal readonly BaseScript BaseScript;
        internal readonly Settings Settings;
        internal NetServer MainNetServer;
        internal ServerEntities Entities;

        internal readonly Dictionary<Command, Action<CommandContext>> Commands = new();
        internal readonly Dictionary<long,Client> ClientsByNetHandle = new();
        internal readonly Dictionary<string, Client> ClientsByName = new();
        internal readonly Dictionary<int, Client> ClientsByID = new();
        internal Client _hostClient;

        private Dictionary<int,FileTransfer> InProgressFileTransfers=new();
        internal Resources Resources;
        internal Logger Logger;
        private Security Security;
        private bool _stopping = false;
        private Thread _listenerThread;
        private Thread _announceThread;
        private Thread _latencyThread;
        private Worker _worker;
        private HashSet<char> _allowedCharacterSet;
        private Dictionary<int,Action<PacketType,byte[]>> PendingResponses=new();
        internal Dictionary<PacketType, Func<byte[],Client,Packet>> RequestHandlers=new();
        private readonly string _compatibleVersion = "V0_5";
        /// <summary>
        /// Instantiate a server.
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="logger"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public Server(Settings settings,Logger logger=null)
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
            _allowedCharacterSet=new HashSet<char>(Settings.AllowedUsernameChars.ToCharArray());


            _worker=new Worker("ServerWorker", Logger);

            _listenerThread=new Thread(() => Listen());
            _latencyThread=new Thread(() =>
            {
                while (!_stopping)
                {
                    foreach(var c in ClientsByNetHandle.Values.ToArray())
                    {
                        try
                        {
                            NetOutgoingMessage outgoingMessage = MainNetServer.CreateMessage();
                            new Packets.PlayerInfoUpdate()
                            {
                                PedID=c.Player.ID,
                                Username=c.Username,
                                Latency=c.Latency,
                                Position=c.Player.Position
                            }.Pack(outgoingMessage);
                            MainNetServer.SendToAll(outgoingMessage, NetDeliveryMethod.ReliableSequenced, (byte)ConnectionChannel.Default);
                        }
                        catch(Exception ex)
                        {
                            Logger?.Error(ex);
                        }
                    }
                    Thread.Sleep(1000);
                }
            });
            _announceThread=new Thread(async () =>
            {
                try
                {
                    // TLS only
                    ServicePointManager.Expect100Continue = true;
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls13 | SecurityProtocolType.Tls12;
                    ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

                    HttpClient httpClient = new();
                    IpInfo info;
                    try
                    {
                        info = CoreUtils.GetIPInfo();
                        Logger?.Info($"Your public IP is {info.Address}, announcing to master server...");
                    }
                    catch (Exception ex)
                    {
                        Logger?.Error(ex.InnerException?.Message ?? ex.Message);
                        return;
                    }
                    while (!_stopping)
                    {
                        HttpResponseMessage response = null;
                        try
                        {
                            var serverInfo = new ServerInfo
                            {
                                Address = info.Address,
                                Port=Settings.Port.ToString(),
                                Country=info.Country,
                                Name=Settings.Name,
                                Version=_compatibleVersion.Replace("_", "."),
                                Players=MainNetServer.ConnectionsCount.ToString(),
                                MaxPlayers=Settings.MaxPlayers.ToString(),
                                Description=Settings.Description,
                                Website=Settings.Website,
                                GameMode=Settings.GameMode,
                                Language=Settings.Language,
                                P2P=Settings.UseP2P,
                                ZeroTier=Settings.UseZeroTier,
                                ZeroTierNetWorkID=Settings.UseZeroTier ? Settings.ZeroTierNetworkID : "",
                                ZeroTierAddress=Settings.UseZeroTier ? ZeroTierHelper.Networks[Settings.ZeroTierNetworkID].Addresses.Where(x => !x.Contains(":")).First() : "0.0.0.0",
                            };
                            string msg = JsonConvert.SerializeObject(serverInfo);

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
                        for (int i = 0; i<10; i++)
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

            if (Settings.UseZeroTier)
            {
                Logger?.Info($"Joining ZeroTier network: "+Settings.ZeroTierNetworkID);
                if (ZeroTierHelper.Join(Settings.ZeroTierNetworkID)==null)
                {
                    throw new Exception("Failed to obtain ZeroTier network IP");
                }
            }
            else if (Settings.UseP2P)
            {
                Logger?.Warning("ZeroTier is not enabled, P2P connection may not work as expected.");
            }

            // 623c92c287cc392406e7aaaac1c0f3b0 = RAGECOOP
            NetPeerConfiguration config = new("623c92c287cc392406e7aaaac1c0f3b0")
            {
                Port = Settings.Port,
                MaximumConnections = Settings.MaxPlayers,
                EnableUPnP = false,
                AutoFlushSendQueue = true,
                PingInterval=5
            };

            config.EnableMessageType(NetIncomingMessageType.ConnectionApproval);
            config.EnableMessageType(NetIncomingMessageType.UnconnectedData);

            MainNetServer = new NetServer(config);
            MainNetServer.Start();
            Logger?.Info(string.Format("Server listening on {0}:{1}", config.LocalAddress.ToString(), config.Port));
            
            BaseScript.API=API;
            BaseScript.OnStart();
            Resources.LoadAll();
            _listenerThread.Start();
            _latencyThread.Start();
            if (Settings.AnnounceSelf)
            {
                _announceThread.Start();
            }

            Logger?.Info("Listening for clients");
        }
        /// <summary>
        /// Terminate threads and stop the server
        /// </summary>
        public void Stop()
        {
            _stopping = true;
            Logger?.Flush();
            _listenerThread.Join();
            _latencyThread.Join();
            if (_announceThread.IsAlive)
            {
                _announceThread.Join();
            }
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
                                GetHandshake(message.SenderConnection, data.GetPacket<Packets.Handshake>());
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
                        if (!ClientsByNetHandle.TryGetValue(message.SenderConnection.RemoteUniqueIdentifier, out sender))
                        {
                            break;
                        }
                        NetConnectionStatus status = (NetConnectionStatus)message.ReadByte();

                        if (status == NetConnectionStatus.Disconnected)
                        {

                            PlayerDisconnected(sender);
                        }
                        else if (status == NetConnectionStatus.Connected)
                        {
                            PlayerConnected(sender);
                            _worker.QueueJob(() => API.Events.InvokePlayerConnected(sender));
                            Resources.SendTo(sender);
                        }
                        break;
                    }
                case NetIncomingMessageType.Data:
                    {
                        
                        // Get sender client
                        if (ClientsByNetHandle.TryGetValue(message.SenderConnection.RemoteUniqueIdentifier, out sender))
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

                                            if (Settings.UseP2P) { break; }
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
                    case PacketType.PedSync:
                        PedSync(data.GetPacket<Packets.PedSync>(), sender);
                        break;

                    case PacketType.VehicleSync:
                        VehicleSync(data.GetPacket<Packets.VehicleSync>(), sender);
                        break;

                    case PacketType.ProjectileSync:
                        ProjectileSync(data.GetPacket<Packets.ProjectileSync>(), sender);
                        break;

                    case PacketType.ChatMessage:
                        {
                            Packets.ChatMessage packet = new((b) =>
                            {
                                return Security.Decrypt(b,sender.EndPoint);
                            });
                            packet.Deserialize(data);
                            ChatMessageReceived(packet.Username,packet.Message, sender);
                        }
                        break;

                    case PacketType.Voice:
                        {
                            Forward(data.GetPacket<Packets.Voice>(),sender,ConnectionChannel.Voice);
                        }
                        break;

                    case PacketType.CustomEvent:
                        {
                            Packets.CustomEvent packet = new Packets.CustomEvent();
                            packet.Deserialize(data);
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
 
            _worker.QueueJob(() => API.Events.InvokeOnChatMessage(message, sender));
                            
            foreach(var c in ClientsByNetHandle.Values)
            {
                var msg = MainNetServer.CreateMessage();
                var crypt = new Func<string, byte[]>((s) =>
                {
                    return Security.Encrypt(s.GetBytes(), c.EndPoint);
                });
                new Packets.ChatMessage(crypt)
                {
                    Username=name,
                    Message=message
                }.Pack(msg); 
                MainNetServer.SendMessage(msg,c.Connection, NetDeliveryMethod.ReliableOrdered, (int)ConnectionChannel.Chat);
            }

            Logger?.Info(name + ": " + message);
        }
        internal void SendChatMessage(string name, string message, Client target)
        {
            if(target == null) { return; }
            var msg = MainNetServer.CreateMessage();
            new Packets.ChatMessage(new Func<string, byte[]>((s) =>
            {
                return Security.Encrypt(s.GetBytes(), target.EndPoint);
            }))
            {
                Username= name,
                Message=message,
            }.Pack(msg);
            MainNetServer.SendMessage(msg, target.Connection, NetDeliveryMethod.ReliableOrdered, (int)ConnectionChannel.Chat);
        }

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
                p.Deserialize(response);
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
        internal void Forward(Packet p, Client except, ConnectionChannel channel = ConnectionChannel.Default, NetDeliveryMethod method = NetDeliveryMethod.UnreliableSequenced)
        {
            NetOutgoingMessage outgoingMessage = MainNetServer.CreateMessage();
            p.Pack(outgoingMessage);
            MainNetServer.SendToAll(outgoingMessage, except.Connection, method, (int)channel);
        }
        internal void SendToAll(Packet p, ConnectionChannel channel = ConnectionChannel.Default, NetDeliveryMethod method = NetDeliveryMethod.UnreliableSequenced)
        {
            NetOutgoingMessage outgoingMessage = MainNetServer.CreateMessage();
            p.Pack(outgoingMessage);
            MainNetServer.SendToAll(outgoingMessage, method, (int)channel);
        }
    }
}
