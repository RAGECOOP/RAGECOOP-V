using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Lidgren.Network;

namespace CoopServer
{
    internal class Resource
    {
        private static Thread _mainThread;
        private static bool _hasToStop = false;
        private static Queue<Action> _actionQueue;
        private static TaskFactory _factory;
        private static ServerScript _script;

        public Resource(ServerScript script)
        {
            _factory = new();
            _actionQueue = new();
            _mainThread = new(ThreadLoop) { IsBackground = true };
            _mainThread.Start();

            lock (_actionQueue)
            {
                _actionQueue.Enqueue(() => _script = script);
            }
        }

        private void ThreadLoop()
        {
            do
            {
                // 16 milliseconds to sleep to reduce CPU usage
                Thread.Sleep(1000 / 60);

                if (_actionQueue.Count == 0)
                {
                    continue;
                }

                lock (_actionQueue)
                {
                    _factory.StartNew(() => _actionQueue.Dequeue()?.Invoke());
                }
            } while (_hasToStop);
        }

        public bool InvokeModPacketReceived(long from, long target, string mod, byte customID, byte[] bytes)
        {
            Task<bool> shutdownTask = new(() => _script.API.InvokeModPacketReceived(from, target, mod, customID, bytes));
            shutdownTask.Start();
            shutdownTask.Wait(5000);

            return shutdownTask.Result;
        }

        public void InvokePlayerConnected(Client client)
        {
            lock (_actionQueue)
            {
                _actionQueue.Enqueue(() => _script.API.InvokePlayerConnected(client));
            }
        }

        public void InvokePlayerDisconnected(Client client)
        {
            lock (_actionQueue)
            {
                _actionQueue.Enqueue(() => _script.API.InvokePlayerDisconnected(client));
            }
        }

        public bool InvokeChatMessage(string username, string message)
        {
            Task<bool> shutdownTask = new(() => _script.API.InvokeChatMessage(username, message));
            shutdownTask.Start();
            shutdownTask.Wait(5000);

            return shutdownTask.Result;
        }

        public void InvokePlayerPositionUpdate(PlayerData playerData)
        {
            lock (_actionQueue)
            {
                _actionQueue.Enqueue(() => _script.API.InvokePlayerPositionUpdate(playerData));
            }
        }
    }

    public abstract class ServerScript
    {
        public API API { get; } = new();
    }

    public class API
    {
        #region DELEGATES
        public delegate void ChatEvent(string username, string message, CancelEventArgs cancel);
        public delegate void PlayerEvent(Client client);
        public delegate void ModEvent(long from, long target, string mod, byte customID, byte[] bytes, CancelEventArgs args);
        #endregion

        #region EVENTS
        public event EventHandler OnStart;
        public event ChatEvent OnChatMessage;
        public event PlayerEvent OnPlayerConnected;
        public event PlayerEvent OnPlayerDisconnected;
        public event PlayerEvent OnPlayerPositionUpdate;
        public event ModEvent OnModPacketReceived;

        internal void InvokeStart()
        {
            OnStart?.Invoke(this, EventArgs.Empty);
        }

        internal void InvokePlayerConnected(Client client)
        {
            OnPlayerConnected?.Invoke(client);
        }

        internal void InvokePlayerDisconnected(Client client)
        {
            OnPlayerDisconnected?.Invoke(client);
        }

        internal bool InvokeChatMessage(string username, string message)
        {
            CancelEventArgs args = new(false);
            OnChatMessage?.Invoke(username, message, args);
            return args.Cancel;
        }

        internal void InvokePlayerPositionUpdate(PlayerData playerData)
        {
            OnPlayerPositionUpdate?.Invoke(Server.Clients.First(x => x.Player.Username == playerData.Username));
        }

        internal bool InvokeModPacketReceived(long from, long target, string mod, byte customID, byte[] bytes)
        {
            CancelEventArgs args = new(false);
            OnModPacketReceived?.Invoke(from, target, mod, customID, bytes, args);
            return args.Cancel;
        }
        #endregion

        #region FUNCTIONS
        public static void SendNativeCallToAll(ulong hash, params object[] args)
        {
            if (Server.MainNetServer.ConnectionsCount == 0)
            {
                return;
            }

            List<NativeArgument> arguments;
            if ((arguments = Util.ParseNativeArguments(args)) == null)
            {
                return;
            }

            NativeCallPacket packet = new()
            {
                Hash = hash,
                Args = arguments
            };

            NetOutgoingMessage outgoingMessage = Server.MainNetServer.CreateMessage();
            packet.PacketToNetOutGoingMessage(outgoingMessage);
            Server.MainNetServer.SendMessage(outgoingMessage, Server.MainNetServer.Connections, NetDeliveryMethod.ReliableOrdered, 0);
        }

        public static List<long> GetAllConnections()
        {
            List<long> result = new();

            Server.MainNetServer.Connections.ForEach(x => result.Add(x.RemoteUniqueIdentifier));

            return result;
        }

        public static int GetAllClientsCount()
        {
            return Server.Clients.Count;
        }

        public static List<Client> GetAllClients()
        {
            return Server.Clients;
        }

        public static Client GetClientByUsername(string username)
        {
            return Server.Clients.FirstOrDefault(x => x.Player.Username == username);
        }

        public static void SendChatMessageToAll(string message, string username = "Server")
        {
            if (Server.MainNetServer.ConnectionsCount == 0)
            {
                return;
            }

            ChatMessagePacket packet = new()
            {
                Username = username,
                Message = message
            };

            NetOutgoingMessage outgoingMessage = Server.MainNetServer.CreateMessage();
            packet.PacketToNetOutGoingMessage(outgoingMessage);
            Server.MainNetServer.SendMessage(outgoingMessage, Server.MainNetServer.Connections, NetDeliveryMethod.ReliableOrdered, 0);
        }

        public static void RegisterCommand(string name, Action<CommandContext> callback)
        {
            Server.RegisterCommand(name, callback);
        }

        public static void RegisterCommands<T>()
        {
            Server.RegisterCommands<T>();
        }
        #endregion
    }

    public class Command
    {
        public string Name { get; set; }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class CommandAttribute : Attribute
    {
        /// <summary>
        /// Sets name of the command
        /// </summary>
        public string Name { get; set; }

        public CommandAttribute(string name)
        {
            Name = name;
        }
    }

    public class CommandContext
    {
        /// <summary>
        /// Gets the client which executed the command
        /// </summary>
        public Client Client { get; internal set; }

        /// <summary>
        /// Gets the chatdata associated with the command
        /// </summary>
        public string[] Args { get; internal set; }
    }
}
