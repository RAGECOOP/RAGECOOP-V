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
        public bool ReadyToStop = false;

        private static Thread _mainThread;
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
            while (!Program.ReadyToStop)
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

            _script.API.InvokeStop();
            ReadyToStop = true;
        }

        public bool InvokeModPacketReceived(long from, long target, string modName, byte customID, byte[] bytes)
        {
            Task<bool> task = new(() => _script.API.InvokeModPacketReceived(from, target, modName, customID, bytes));
            task.Start();
            task.Wait(5000);

            return task.Result;
        }

        public void InvokePlayerHandshake(Client client)
        {
            lock (_actionQueue.SyncRoot)
            {
                _actionQueue.Enqueue(new Action(() => _script.API.InvokePlayerHandshake(client)));
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

        public void InvokePlayerPositionUpdate(string username)
        {
            lock (_actionQueue.SyncRoot)
            {
                _actionQueue.Enqueue(new Action(() => _script.API.InvokePlayerPositionUpdate(username)));
            }
        }

        public void InvokePlayerUpdate(Client client)
        {
            lock (_actionQueue.SyncRoot)
            {
                _actionQueue.Enqueue(new Action(() => _script.API.InvokePlayerUpdate(client)));
            }
        }

        public void InvokePlayerHealthUpdate(string username)
        {
            lock (_actionQueue.SyncRoot)
            {
                _actionQueue.Enqueue(new Action(() => _script.API.InvokePlayerHealthUpdate(username)));
            }
        }

        public void InvokePlayerPedHandleUpdate(string username)
        {
            lock (_actionQueue.SyncRoot)
            {
                _actionQueue.Enqueue(new Action(() => _script.API.InvokePlayerPedHandleUpdate(username)));
            }
        }

        public void InvokePlayerVehicleHandleUpdate(string username)
        {
            lock (_actionQueue.SyncRoot)
            {
                _actionQueue.Enqueue(new Action(() => _script.API.InvokePlayerVehicleHandleUpdate(username)));
            }
        }

        public void InvokeTick(long tick)
        {
            lock (_actionQueue.SyncRoot)
            {
                _actionQueue.Enqueue(new Action(() => _script.API.InvokeTick(tick)));
            }
        }
    }

    public class API
    {
        #region DELEGATES
        public delegate void EmptyEvent();
        public delegate void OnTickEvent(long tick);
        public delegate void ChatEvent(string username, string message, CancelEventArgs cancel);
        public delegate void PlayerEvent(Client client);
        public delegate void ModEvent(long from, long target, string modName, byte customID, byte[] bytes, CancelEventArgs args);
        #endregion

        #region EVENTS
        /// <summary>
        /// Called every tick
        /// </summary>
        public event OnTickEvent OnTick;
        /// <summary>
        /// Called when the server has started
        /// </summary>
        public event EmptyEvent OnStart;
        /// <summary>
        /// Called when the server has stopped
        /// </summary>
        public event EmptyEvent OnStop;
        /// <summary>
        /// Called when the server receives a new chat message for players
        /// </summary>
        public event ChatEvent OnChatMessage;
        /// <summary>
        /// Called when the server receives a new incoming connection
        /// </summary>
        public event PlayerEvent OnPlayerHandshake;
        /// <summary>
        /// Called when a new player has successfully connected
        /// </summary>
        public event PlayerEvent OnPlayerConnected;
        /// <summary>
        /// Called when a new player has successfully disconnected
        /// </summary>
        public event PlayerEvent OnPlayerDisconnected;
        /// <summary>
        /// Called when a new player sends data like health
        /// </summary>
        public event PlayerEvent OnPlayerUpdate;
        /// <summary>
        /// Called when a player has a new health value
        /// </summary>
        public event PlayerEvent OnPlayerHealthUpdate;
        /// <summary>
        /// Called when a player has a new position
        /// </summary>
        public event PlayerEvent OnPlayerPositionUpdate;
        /// <summary>
        /// Called when a player has a new position
        /// </summary>
        public event PlayerEvent OnPlayerPedHandleUpdate;
        /// <summary>
        /// Called when a player has a new position
        /// </summary>
        public event PlayerEvent OnPlayerVehicleHandleUpdate;
        /// <summary>
        /// Called when a player sends a packet from another modification
        /// </summary>
        public event ModEvent OnModPacketReceived;

        internal void InvokeTick(long tick)
        {
            OnTick?.Invoke(tick);
        }

        internal void InvokeStart()
        {
            OnStart?.Invoke();
        }

        internal void InvokeStop()
        {
            OnStop?.Invoke();
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

        internal void InvokePlayerHealthUpdate(string username)
        {
            OnPlayerHealthUpdate?.Invoke(Server.Clients.FirstOrDefault(x => x.Player.Username == username));
        }

        internal bool InvokeChatMessage(string username, string message)
        {
            CancelEventArgs args = new(false);
            OnChatMessage?.Invoke(username, message, args);
            return args.Cancel;
        }

        internal void InvokePlayerPositionUpdate(string username)
        {
            OnPlayerPositionUpdate?.Invoke(Server.Clients.FirstOrDefault(x => x.Player.Username == username));
        }

        internal void InvokePlayerPedHandleUpdate(string username)
        {
            OnPlayerPedHandleUpdate?.Invoke(Server.Clients.FirstOrDefault(x => x.Player.Username == username));
        }

        internal void InvokePlayerVehicleHandleUpdate(string username)
        {
            OnPlayerVehicleHandleUpdate?.Invoke(Server.Clients.FirstOrDefault(x => x.Player.Username == username));
        }

        internal bool InvokeModPacketReceived(long from, long target, string modName, byte customID, byte[] bytes)
        {
            CancelEventArgs args = new(false);
            OnModPacketReceived?.Invoke(from, target, modName, customID, bytes, args);
            return args.Cancel;
        }
        #endregion

        #region FUNCTIONS
        /// <summary>
        /// Send a mod packet to all players
        /// </summary>
        /// <param name="modName">The name of the modification that will receive the data</param>
        /// <param name="customID">The ID to check what this data is</param>
        /// <param name="bytes">The serialized data</param>
        /// <param name="netHandleList">The list of connections (players) that will receive the data</param>
        public static void SendModPacketToAll(string modName, byte customID, byte[] bytes, List<long> netHandleList = null)
        {
            try
            {
                List<NetConnection> connections = netHandleList == null
                    ? Server.MainNetServer.Connections
                    : Server.MainNetServer.Connections.FindAll(c => netHandleList.Contains(c.RemoteUniqueIdentifier));
                // A resource can be calling this function on disconnect of the last player in the server and we will
                // get an empty connection list, make sure connections has at least one handle in it
                if (connections.Count > 0)
                {
                    NetOutgoingMessage outgoingMessage = Server.MainNetServer.CreateMessage();
                    new Packets.Mod()
                    {
                        NetHandle = 0,
                        Target = 0,
                        Name = modName,
                        CustomPacketID = customID,
                        Bytes = bytes
                    }.PacketToNetOutGoingMessage(outgoingMessage);
                    Logging.Debug($"SendModPacketToAll recipients list {connections.Count}");
                    Server.MainNetServer.SendMessage(outgoingMessage, connections, NetDeliveryMethod.ReliableOrdered, (byte)ConnectionChannel.Mod);
                    Server.MainNetServer.FlushSendQueue();
                }
            }
            catch (Exception e)
            {
                Logging.Error($">> {e.Message} <<>> {e.Source ?? string.Empty} <<>> {e.StackTrace ?? string.Empty} <<");
            }
        }

        /// <summary>
        /// Send a native call (Function.Call) to all players.
        /// Keys = int, float, bool, string and lvector3
        /// </summary>
        /// <param name="hash">The hash (Example: 0x25223CA6B4D20B7F = GET_CLOCK_HOURS)</param>
        /// <param name="args">The arguments (Example: string = int, object = 5)</param>
        public static void SendNativeCallToAll(ulong hash, params object[] args)
        {
            try
            {
                if (Server.MainNetServer.ConnectionsCount == 0)
                {
                    return;
                }

                if (args != null && args.Length == 0)
                {
                    Logging.Error($"[ServerScript->SendNativeCallToAll(ulong hash, params object[] args)]: args is not null!");
                    return;
                }

                Packets.NativeCall packet = new()
                {
                    Hash = hash,
                    Args = new List<object>(args) ?? new List<object>()
                };

                NetOutgoingMessage outgoingMessage = Server.MainNetServer.CreateMessage();
                packet.PacketToNetOutGoingMessage(outgoingMessage);
                Server.MainNetServer.SendMessage(outgoingMessage, Server.MainNetServer.Connections, NetDeliveryMethod.ReliableOrdered, (byte)ConnectionChannel.Native);
            }
            catch (Exception e)
            {
                Logging.Error($">> {e.Message} <<>> {e.Source ?? string.Empty} <<>> {e.StackTrace ?? string.Empty} <<");
            }
        }

        /// <summary>
        /// Get all connections as a list of NetHandle(long)
        /// </summary>
        /// <returns>All connections(NetHandle) as a List</returns>
        public static List<long> GetAllConnections()
        {
            List<long> result = new();

            Server.MainNetServer.Connections.ForEach(x => result.Add(x.RemoteUniqueIdentifier));

            return result;
        }

        /// <summary>
        /// Get the count of all connections
        /// </summary>
        /// <returns>The count of all connections as an integer</returns>
        public static int GetAllClientsCount()
        {
            return Server.Clients.Count;
        }

        /// <summary>
        /// Get a list of all Clients
        /// </summary>
        /// <returns>All Clients as a List</returns>
        public static List<Client> GetAllClients()
        {
            return Server.Clients;
        }

        /// <summary>
        /// Get the client by its username
        /// </summary>
        /// <param name="username">The username to search for</param>
        /// <returns>The Client from this user or null</returns>
        public static Client GetClientByUsername(string username)
        {
            return Server.Clients.FirstOrDefault(x => x.Player.Username.ToLower() == username.ToLower());
        }

        /// <summary>
        /// Send a chat message to all players
        /// </summary>
        /// <param name="message">The chat message</param>
        /// <param name="username">The username which send this message (default = "Server")</param>
        /// <param name="netHandleList">The list of connections (players) who received this chat message</param>
        public static void SendChatMessageToAll(string message, string username = "Server", List<long> netHandleList = null)
        {
            try
            {
                if (Server.MainNetServer.ConnectionsCount == 0)
                {
                    return;
                }

                List<NetConnection> connections = netHandleList == null
                    ? Server.MainNetServer.Connections
                    : Server.MainNetServer.Connections.FindAll(c => netHandleList.Contains(c.RemoteUniqueIdentifier));

                Server.SendChatMessage(username, message, connections);
            }
            catch (Exception e)
            {
                Logging.Error($">> {e.Message} <<>> {e.Source ?? string.Empty} <<>> {e.StackTrace ?? string.Empty} <<");
            }
        }

        /// <summary>
        /// Send CleanUpWorld to all players to delete all objects created by the server
        /// </summary>
        public static void SendCleanUpWorldToAll(List<long> netHandleList = null)
        {
            if (Server.MainNetServer.ConnectionsCount == 0)
            {
                return;
            }

            List<NetConnection> connections = netHandleList == null
                    ? Server.MainNetServer.Connections
                    : Server.MainNetServer.Connections.FindAll(c => netHandleList.Contains(c.RemoteUniqueIdentifier));

            NetOutgoingMessage outgoingMessage = Server.MainNetServer.CreateMessage();
            outgoingMessage.Write((byte)PacketTypes.CleanUpWorld);
            Server.MainNetServer.SendMessage(outgoingMessage, connections, NetDeliveryMethod.ReliableOrdered, (byte)ConnectionChannel.Default);
        }

        /// <summary>
        /// Register a new command chat command (Example: "/test")
        /// </summary>
        /// <param name="name">The name of the command (Example: "test" for "/test")</param>
        /// <param name="usage">How to use this message (argsLength required!)</param>
        /// <param name="argsLength">The length of args (Example: "/message USERNAME MESSAGE" = 2) (usage required!)</param>
        /// <param name="callback">Create a new function!</param>
        public static void RegisterCommand(string name, string usage, short argsLength, Action<CommandContext> callback)
        {
            Server.RegisterCommand(name, usage, argsLength, callback);
        }
        /// <summary>
        /// Register a new command chat command (Example: "/test")
        /// </summary>
        /// <param name="name">The name of the command (Example: "test" for "/test")</param>
        /// <param name="callback">Create a new function!</param>
        public static void RegisterCommand(string name, Action<CommandContext> callback)
        {
            Server.RegisterCommand(name, callback);
        }

        /// <summary>
        /// Register a class of commands
        /// </summary>
        /// <typeparam name="T">The name of your class with functions</typeparam>
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

        /// <summary>
        /// Set the Usage (Example: "Please use "/help"". ArgsLength required!)
        /// </summary>
        public string Usage { get; set; }

        /// <summary>
        /// Set the length of arguments (Example: 2 for "/message USERNAME MESSAGE". Usage required!)
        /// </summary>
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
        /// Gets the arguments (Example: "/message USERNAME MESSAGE", Args[0] for USERNAME)
        /// </summary>
        public string[] Args { get; internal set; }
    }
}
