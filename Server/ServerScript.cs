using System;
using System.Collections.Generic;

using Lidgren.Network;

namespace CoopServer
{
    public class ServerScript
    {
        public virtual void Start() { }

        public virtual void OnPlayerConnect(Entities.EntitiesPlayer player)
        {
            Logging.Info("New player [" + player.SocialClubName + " | " + player.Username + "] connected!");
        }

        public virtual void OnPlayerDisconnect(Entities.EntitiesPlayer player, string reason)
        {
            Logging.Info(player.Username + " left the server, reason: " + reason);
        }

        public virtual bool OnChatMessage(string username, string message)
        {
            return false;
        }

        public static List<long> GetAllConnections()
        {
            List<long> result = new();

            lock (Server.MainNetServer.Connections)
            {
                Server.MainNetServer.Connections.ForEach(x => result.Add(x.RemoteUniqueIdentifier));
            }

            return result;
        }

        public static int GetAllPlayersCount()
        {
            lock (Server.Players)
            {
                return Server.Players.Count;
            }
        }

        public static Dictionary<long, Entities.EntitiesPlayer> GetAllPlayers()
        {
            lock (Server.Players)
            {
                return Server.Players;
            }
        }

        public static void KickPlayerByUsername(string username, string[] reason)
        {
            lock (Server.MainNetServer.Connections)
            {
                NetConnection userConnection = Util.GetConnectionByUsername(username);
                if (userConnection == null)
                {
                    Logging.Warning("[ServerScript->KickPlayerByUsername(\"" + username + "\", \"" + string.Join(" ", reason) + "\")]: User not found!");
                    return;
                }

                userConnection.Disconnect(string.Join(" ", reason));
            }
        }

        public static void SendChatMessageToAll(string message, string username = "Server")
        {
            List<NetConnection> connections = Server.MainNetServer.Connections;

            if (connections.Count != 0)
            {
                ChatMessagePacket packet = new()
                {
                    Username = username,
                    Message = message
                };

                NetOutgoingMessage outgoingMessage = Server.MainNetServer.CreateMessage();
                packet.PacketToNetOutGoingMessage(outgoingMessage);
                Server.MainNetServer.SendMessage(outgoingMessage, Server.MainNetServer.Connections, NetDeliveryMethod.ReliableOrdered, 0);
            }

            Logging.Info(username + ": " + message);
        }

        public static void SendChatMessageToPlayer(string username, string message, string from = "Server")
        {
            lock (Server.MainNetServer.Connections)
            {
                NetConnection userConnection = Util.GetConnectionByUsername(username);
                if (userConnection == null)
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

            Logging.Info(from + ": " + message);
        }

        public static void RegisterCommand(string name, Action<CommandContext> callback)
        {
            Server.RegisterCommand(name, callback);
        }

        public static void RegisterCommands<T>()
        {
            Server.RegisterCommands<T>();
        }
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
