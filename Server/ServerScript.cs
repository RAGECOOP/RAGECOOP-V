using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using Lidgren.Network;

namespace CoopServer
{
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
