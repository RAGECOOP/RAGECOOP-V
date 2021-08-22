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
        public delegate void PlayerEvent(Entities.EntitiesPlayer player);
        #endregion

        #region EVENTS
        public event EventHandler OnStart;
        public event ChatEvent OnChatMessage;
        public event PlayerEvent OnPlayerConnected;
        public event PlayerEvent OnPlayerDisconnected;
        public event PlayerEvent OnPlayerPositionUpdate;

        internal void InvokeStart()
        {
            OnStart?.Invoke(this, EventArgs.Empty);
        }

        internal void InvokePlayerConnected(Entities.EntitiesPlayer player)
        {
            OnPlayerConnected?.Invoke(player);
        }

        internal void InvokePlayerDisconnected(Entities.EntitiesPlayer player)
        {
            OnPlayerDisconnected?.Invoke(player);
        }

        internal bool InvokeChatMessage(string username, string message)
        {
            var args = new CancelEventArgs(false);
            OnChatMessage?.Invoke(username, message, args);
            return args.Cancel;
        }

        internal void InvokePlayerPositionUpdate(Entities.EntitiesPlayer player)
        {
            OnPlayerPositionUpdate?.Invoke(player);
        }
        #endregion

        #region FUNCTIONS
        public static void SendNativeCallToAll(ulong hash, params object[] args)
        {
            List<NetConnection> connections = Server.MainNetServer.Connections;
            if (connections.Count == 0)
            {
                return;
            }

            List<NativeArgument> arguments = Util.ParseNativeArguments(args);
            if (arguments == null)
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

        public static void SendNativeCallToPlayer(string username, ulong hash, params object[] args)
        {
            NetConnection userConnection = Util.GetConnectionByUsername(username);
            if (userConnection == default)
            {
                Logging.Warning("[ServerScript->SendNativeCallToPlayer(\"" + username + "\", \"" + hash + "\", params object[] args)]: User not found!");
                return;
            }

            List<NativeArgument> arguments = Util.ParseNativeArguments(args);
            if (arguments == null)
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
            Server.MainNetServer.SendMessage(outgoingMessage, userConnection, NetDeliveryMethod.ReliableOrdered, 0);
        }

        public static List<long> GetAllConnections()
        {
            List<long> result = new();

            Server.MainNetServer.Connections.ForEach(x => result.Add(x.RemoteUniqueIdentifier));

            return result;
        }

        public static int GetAllPlayersCount()
        {
            return Server.Players.Count;
        }

        public static Dictionary<long, Entities.EntitiesPlayer> GetAllPlayers()
        {
            return Server.Players;
        }

        public static Entities.EntitiesPlayer GetPlayerByUsername(string username)
        {
            return Server.Players.FirstOrDefault(x => x.Value.Username == username).Value;
        }

        public static void KickPlayerByUsername(string username, string[] reason)
        {
            NetConnection userConnection = Util.GetConnectionByUsername(username);
            if (userConnection == default)
            {
                Logging.Warning("[ServerScript->KickPlayerByUsername(\"" + username + "\", \"" + string.Join(" ", reason) + "\")]: User not found!");
                return;
            }

            userConnection.Disconnect(string.Join(" ", reason));
        }

        public static void SendChatMessageToAll(string message, string username = "Server")
        {
            List<NetConnection> connections = Server.MainNetServer.Connections;
            if (connections.Count == 0)
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

        public static void SendChatMessageToPlayer(string username, string message, string from = "Server")
        {
            NetConnection userConnection = Util.GetConnectionByUsername(username);
            if (userConnection == default)
            {
                Logging.Warning("[ServerScript->SendChatMessageToPlayer(\"" + username + "\", \"" + message + "\", \"" + from + "\")]: User not found!");
                return;
            }

            ChatMessagePacket packet = new()
            {
                Username = from,
                Message = message
            };

            NetOutgoingMessage outgoingMessage = Server.MainNetServer.CreateMessage();
            packet.PacketToNetOutGoingMessage(outgoingMessage);
            Server.MainNetServer.SendMessage(outgoingMessage, userConnection, NetDeliveryMethod.ReliableOrdered, 0);
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
        public Entities.EntitiesPlayer Player { get; internal set; }

        /// <summary>
        /// Gets the chatdata associated with the command
        /// </summary>
        public string[] Args { get; internal set; }
    }
}
