using Lidgren.Network;
using RageCoop.Core;
using RageCoop.Server.Scripting;
using System;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Timer = System.Timers.Timer;

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
        internal readonly Dictionary<long, Client> ClientsByNetHandle = new();
        internal readonly Dictionary<string, Client> ClientsByName = new();
        internal readonly Dictionary<int, Client> ClientsByID = new();
        internal Client _hostClient;

        private readonly Dictionary<int, FileTransfer> InProgressFileTransfers = new();
        internal Resources Resources;
        internal Logger Logger;
        internal Security Security;
        private bool _stopping = false;
        private readonly Thread _listenerThread;
        private readonly Timer _announceTimer = new();
        private readonly Timer _playerUpdateTimer = new();
        private readonly Timer _antiAssholesTimer = new();
        private readonly Timer _updateTimer = new();
        private readonly Worker _worker;
        private readonly HashSet<char> _allowedCharacterSet;
        private readonly Dictionary<int, Action<PacketType, NetIncomingMessage>> PendingResponses = new();
        internal Dictionary<PacketType, Func<NetIncomingMessage, Client, Packet>> RequestHandlers = new();
        /// <summary>
        /// Get the current server version
        /// </summary>
        public static readonly Version Version = typeof(Server).Assembly.GetName().Version;
        /// <summary>
        /// Instantiate a server.
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="logger"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public Server(Settings settings, Logger logger = null)
        {
            Settings = settings;
            if (settings == null) { throw new ArgumentNullException("Server settings cannot be null!"); }
            Logger = logger;
            if (Logger != null) { Logger.LogLevel = Settings.LogLevel; }
            API = new API(this);
            Resources = new Resources(this);
            Security = new Security(Logger);
            Entities = new ServerEntities(this);
            BaseScript = new BaseScript(this);
            _allowedCharacterSet = new HashSet<char>(Settings.AllowedUsernameChars.ToCharArray());


            _worker = new Worker("ServerWorker", Logger);

            _listenerThread = new Thread(() => Listen());

            _announceTimer.Interval = 1;
            _announceTimer.Elapsed += (s, e) =>
            {
                _announceTimer.Interval = 10000;
                _announceTimer.Stop();
                Announce();
                _announceTimer.Start();
            };

            _playerUpdateTimer.Interval = 1000;
            _playerUpdateTimer.Elapsed += (s, e) => SendPlayerUpdate();


            _antiAssholesTimer.Interval = 5000;
            _antiAssholesTimer.Elapsed += (s, e) => KickAssholes();


            _updateTimer.Interval = 1;
            _updateTimer.Elapsed += (s, e) =>
            {
                _updateTimer.Interval = 1000 * 60 * 10; // 10 minutes
                _updateTimer.Stop();
                CheckUpdate();
                _updateTimer.Start();
            };
        }


        /// <summary>
        /// Spawn threads and start the server
        /// </summary>
        public void Start()
        {
            Logger?.Info("================");
            Logger?.Info($"Listening port: {Settings.Port}");
            Logger?.Info($"Server version: {Version}");
            Logger?.Info($"Compatible client version: {Version.ToString(3)}");
            Logger?.Info($"Runtime: {CoreUtils.GetInvariantRID()} => {System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier}");
            Logger?.Info("================");
            Logger?.Info($"Listening addresses:");
            foreach (NetworkInterface netInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                Logger?.Info($"[{netInterface.Description}]:");
                IPInterfaceProperties ipProps = netInterface.GetIPProperties();
                foreach (UnicastIPAddressInformation addr in ipProps.UnicastAddresses)
                {
                    Logger.Info(string.Join(", ", addr.Address));
                }
                Logger.Info("");
            }
            if (Settings.UseZeroTier)
            {
                Logger?.Info($"Joining ZeroTier network: " + Settings.ZeroTierNetworkID);
                if (ZeroTierHelper.Join(Settings.ZeroTierNetworkID) == null)
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
                PingInterval = 5
            };

            config.EnableMessageType(NetIncomingMessageType.ConnectionApproval);
            config.EnableMessageType(NetIncomingMessageType.UnconnectedData);

            MainNetServer = new NetServer(config);
            MainNetServer.Start();
            BaseScript.API = API;
            BaseScript.OnStart();
            Resources.LoadAll();
            _listenerThread.Start();
            Logger?.Info("Listening for clients");

            _playerUpdateTimer.Enabled = true;
            if (Settings.AnnounceSelf)
            {
                _announceTimer.Enabled = true;
            }
            if (Settings.AutoUpdate)
            {
                _updateTimer.Enabled = true;
            }
            _antiAssholesTimer.Enabled = true;


        }
        /// <summary>
        /// Terminate threads and stop the server
        /// </summary>
        public void Stop()
        {
            Logger?.Flush();
            Logger?.Dispose();
            _stopping = true;
            _listenerThread.Join();
            _playerUpdateTimer.Enabled = false;
            _announceTimer.Enabled = false;
            _worker.Dispose();
        }
        internal void QueueJob(Action job)
        {
            _worker.QueueJob(job);
        }

        // Send a message to targets or all players
        internal void ChatMessageReceived(string name, string message, Client sender = null)
        {
            if (message.StartsWith('/'))
            {
                string[] cmdArgs = message.Split(" ");
                string cmdName = cmdArgs[0].Remove(0, 1);
                QueueJob(() => API.Events.InvokeOnCommandReceived(cmdName, cmdArgs, sender));
                return;
            }
            message = message.Replace("~", "");

            QueueJob(() => API.Events.InvokeOnChatMessage(message, sender));

            foreach (var c in ClientsByNetHandle.Values)
            {
                var msg = MainNetServer.CreateMessage();
                var crypt = new Func<string, byte[]>((s) =>
                {
                    return Security.Encrypt(s.GetBytes(), c.EndPoint);
                });
                new Packets.ChatMessage(crypt)
                {
                    Username = name,
                    Message = message
                }.Pack(msg);
                MainNetServer.SendMessage(msg, c.Connection, NetDeliveryMethod.ReliableOrdered, (int)ConnectionChannel.Chat);
            }
        }
        internal void SendChatMessage(string name, string message, Client target)
        {
            if (target == null) { return; }
            var msg = MainNetServer.CreateMessage();
            new Packets.ChatMessage(new Func<string, byte[]>((s) =>
            {
                return Security.Encrypt(s.GetBytes(), target.EndPoint);
            }))
            {
                Username = name,
                Message = message,
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
        internal T GetResponse<T>(Client client, Packet request, ConnectionChannel channel = ConnectionChannel.RequestResponse, int timeout = 5000) where T : Packet, new()
        {
            if (Thread.CurrentThread == _listenerThread)
            {
                throw new InvalidOperationException("Cannot wait for response from the listener thread!");
            }

            var received = new AutoResetEvent(false);
            T response = new T();
            var id = NewRequestID();
            PendingResponses.Add(id, (type, m) =>
            {
                response.Deserialize(m);
                received.Set();
            });
            var msg = MainNetServer.CreateMessage();
            msg.Write((byte)PacketType.Request);
            msg.Write(id);
            request.Pack(msg);
            MainNetServer.SendMessage(msg, client.Connection, NetDeliveryMethod.ReliableOrdered, (int)channel);
            if (received.WaitOne(timeout))
            {
                return response;
            }

            return null;
        }
        internal void SendFile(string path, string name, Client client, Action<float> updateCallback = null)
        {
            var fs = File.OpenRead(path);
            SendFile(fs, name, client, NewFileID(), updateCallback);
            fs.Close();
            fs.Dispose();
        }
        internal void SendFile(Stream stream, string name, Client client, int id = default, Action<float> updateCallback = null)
        {
            stream.Seek(0, SeekOrigin.Begin);
            id = id == default ? NewFileID() : id;
            var total = stream.Length;
            Logger?.Debug($"Requesting file transfer:{name}, {total}");
            if (GetResponse<Packets.FileTransferResponse>(client, new Packets.FileTransferRequest()
            {
                FileLength = total,
                Name = name,
                ID = id,
            }, ConnectionChannel.File)?.Response != FileResponse.NeedToDownload)
            {
                Logger?.Info($"Skipping file transfer \"{name}\" to {client.Username}");
                return;
            }
            Logger?.Debug($"Initiating file transfer:{name}, {total}");
            FileTransfer transfer = new()
            {
                ID = id,
                Name = name,
            };
            InProgressFileTransfers.Add(id, transfer);
            int read = 0;
            int thisRead;
            do
            {
                // 4 KB chunk
                byte[] chunk = new byte[4096];
                read += thisRead = stream.Read(chunk, 0, 4096);
                if (thisRead != chunk.Length)
                {
                    if (thisRead == 0) { break; }
                    Logger?.Trace($"Purging chunk:{thisRead}");
                    Array.Resize(ref chunk, thisRead);
                }
                Send(
                new Packets.FileTransferChunk()
                {
                    ID = id,
                    FileChunk = chunk,
                },
                client, ConnectionChannel.File, NetDeliveryMethod.ReliableOrdered);
                transfer.Progress = read / stream.Length;
                if (updateCallback != null) { updateCallback(transfer.Progress); }

            } while (thisRead > 0);
            if (GetResponse<Packets.FileTransferResponse>(client, new Packets.FileTransferComplete()
            {
                ID = id,
            }, ConnectionChannel.File)?.Response != FileResponse.Completed)
            {
                Logger.Warning($"File trasfer to {client.Username} failed: " + name);
            }
            Logger?.Debug($"All file chunks sent:{name}");
            InProgressFileTransfers.Remove(id);
        }
        internal int NewFileID()
        {
            int ID = 0;
            while ((ID == 0)
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
            while ((ID == 0)
                || PendingResponses.ContainsKey(ID))
            {
                byte[] rngBytes = new byte[4];

                RandomNumberGenerator.Create().GetBytes(rngBytes);

                // Convert the bytes into an integer
                ID = BitConverter.ToInt32(rngBytes, 0);
            }
            return ID;
        }
        internal void Send(Packet p, Client client, ConnectionChannel channel = ConnectionChannel.Default, NetDeliveryMethod method = NetDeliveryMethod.UnreliableSequenced)
        {
            NetOutgoingMessage outgoingMessage = MainNetServer.CreateMessage();
            p.Pack(outgoingMessage);
            MainNetServer.SendMessage(outgoingMessage, client.Connection, method, (int)channel);
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
