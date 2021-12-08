using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Lidgren.Network;

namespace CoopServer
{
    public abstract class ServerScript
    {
        public API API { get; } = new();
    }

    internal class Resource
    {
        private static Thread _mainThread;
        private static bool _hasToStop = false;
        private static Queue _actionQueue;
        private static ServerScript _script;

        public Resource(ServerScript script)
        {
            _actionQueue = Queue.Synchronized(new Queue());
            _mainThread = new(ThreadLoop) { IsBackground = true };
            _mainThread.Start();

            lock (_actionQueue.SyncRoot)
            {
                _actionQueue.Enqueue(() =>
                {
                    _script = script;
                    _script.API.InvokeStart();
                });
            }
        }

        private void ThreadLoop()
        {
            while (!_hasToStop)
            {
                Queue localQueue;
                lock (_actionQueue.SyncRoot)
                {
                    localQueue = new(_actionQueue);
                    _actionQueue.Clear();
                }

                while (localQueue.Count > 0)
                {
                    (localQueue.Dequeue() as Action)?.Invoke();
                }

                // 16 milliseconds to sleep to reduce CPU usage
                Thread.Sleep(1000 / 60);
            }
        }

        public bool InvokeModPacketReceived(long from, long target, string mod, byte customID, byte[] bytes)
        {
            Task<bool> task = new(() => _script.API.InvokeModPacketReceived(from, target, mod, customID, bytes));
            task.Start();
            task.Wait(5000);

            return task.Result;
        }

        public void InvokePlayerHandshake(Client client)
        {
            lock (_actionQueue.SyncRoot)
            {
                _actionQueue.Enqueue(new Action(() => _script.API.InvokePlayerConnected(client)));
            }
        }

        public void InvokePlayerConnected(Client client)
        {
            lock (_actionQueue.SyncRoot)
            {
                _actionQueue.Enqueue(new Action(() => _script.API.InvokePlayerConnected(client)));
            }
        }

        public void InvokePlayerDisconnected(Client client)
        {
            lock (_actionQueue.SyncRoot)
            {
                _actionQueue.Enqueue(new Action(() => _script.API.InvokePlayerDisconnected(client)));
            }
        }

        public bool InvokeChatMessage(string username, string message)
        {
            Task<bool> task = new(() => _script.API.InvokeChatMessage(username, message));
            task.Start();
            task.Wait(5000);

            return task.Result;
        }

        public void InvokePlayerPositionUpdate(PlayerData playerData)
        {
            lock (_actionQueue.SyncRoot)
            {
                _actionQueue.Enqueue(new Action(() => _script.API.InvokePlayerPositionUpdate(playerData)));
            }
        }

        public void InvokePlayerUpdate(Client client)
        {
            lock (_actionQueue.SyncRoot)
            {
                _actionQueue.Enqueue(new Action(() => _script.API.InvokePlayerUpdate(client)));
            }
        }

        public void InvokePlayerHealthUpdate(PlayerData playerData)
        {
            lock (_actionQueue.SyncRoot)
            {
                _actionQueue.Enqueue(new Action(() => _script.API.InvokePlayerHealthUpdate(playerData)));
            }
        }
    }

    public class API
    {
        #region DELEGATES
        public delegate void EmptyEvent();
        public delegate void ChatEvent(string username, string message, CancelEventArgs cancel);
        public delegate void PlayerEvent(Client client);
        public delegate void ModEvent(long from, long target, string mod, byte customID, byte[] bytes, CancelEventArgs args);
        #endregion

        #region EVENTS
        public event EmptyEvent OnStart;
        public event ChatEvent OnChatMessage;
        public event PlayerEvent OnPlayerHandshake;
        public event PlayerEvent OnPlayerConnected;
        public event PlayerEvent OnPlayerDisconnected;
        public event PlayerEvent OnPlayerUpdate;
        public event PlayerEvent OnPlayerHealthUpdate;
        public event PlayerEvent OnPlayerPositionUpdate;
        public event ModEvent OnModPacketReceived;

        internal void InvokeStart()
        {
            OnStart?.Invoke();
        }

        internal void InvokePlayerHandshake(Client client)
        {
            OnPlayerHandshake?.Invoke(client);
        }

        internal void InvokePlayerConnected(Client client)
        {
            OnPlayerConnected?.Invoke(client);
        }

        internal void InvokePlayerDisconnected(Client client)
        {
            OnPlayerDisconnected?.Invoke(client);
        }

        internal void InvokePlayerUpdate(Client client)
        {
            OnPlayerUpdate?.Invoke(client);
        }

        internal void InvokePlayerHealthUpdate(PlayerData playerData)
        {
            OnPlayerHealthUpdate?.Invoke(Server.Clients.First(x => x.Player.Username == playerData.Username));
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
        public static void SendModPacketToAll(string mod, byte customID, byte[] bytes)
        {
            try
            {
                NetOutgoingMessage outgoingMessage = Server.MainNetServer.CreateMessage();
                new ModPacket()
                {
                    ID = 0,
                    Target = 0,
                    Mod = mod,
                    CustomPacketID = customID,
                    Bytes = bytes
                }.PacketToNetOutGoingMessage(outgoingMessage);
                Server.MainNetServer.SendMessage(outgoingMessage, Server.MainNetServer.Connections, NetDeliveryMethod.ReliableOrdered, 0);
                Server.MainNetServer.FlushSendQueue();
            }
            catch (Exception e)
            {
                Logging.Error($">> {e.Message} <<>> {e.Source ?? string.Empty} <<>> {e.StackTrace ?? string.Empty} <<");
            }
        }

        public static void SendNativeCallToAll(ulong hash, params object[] args)
        {
            try
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
            catch (Exception e)
            {
                Logging.Error($">> {e.Message} <<>> {e.Source ?? string.Empty} <<>> {e.StackTrace ?? string.Empty} <<");
            }
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
            Client client = Server.Clients.FirstOrDefault(x => x.Player.Username == username);
            return client.Equals(default(Client)) ? null : client;
        }

        public static void SendChatMessageToAll(string message, string username = "Server")
        {
            try
            {
                if (Server.MainNetServer.ConnectionsCount == 0)
                {
                    return;
                }

                NetOutgoingMessage outgoingMessage = Server.MainNetServer.CreateMessage();
                new ChatMessagePacket()
                {
                    Username = username,
                    Message = message
                }.PacketToNetOutGoingMessage(outgoingMessage);
                Server.MainNetServer.SendMessage(outgoingMessage, Server.MainNetServer.Connections, NetDeliveryMethod.ReliableOrdered, 0);
            }
            catch (Exception e)
            {
                Logging.Error($">> {e.Message} <<>> {e.Source ?? string.Empty} <<>> {e.StackTrace ?? string.Empty} <<");
            }
        }

        public static void RegisterCommand(string name, string usage, short argsLength, Action<CommandContext> callback)
        {
            Server.RegisterCommand(name, usage, argsLength, callback);
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

    [AttributeUsage(AttributeTargets.Method)]
    public class Command : Attribute
    {
        /// <summary>
        /// Sets name of the command
        /// </summary>
        public string Name { get; set; }

        public string Usage { get; set; }

        public short ArgsLength { get; set; }

        public Command(string name)
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
