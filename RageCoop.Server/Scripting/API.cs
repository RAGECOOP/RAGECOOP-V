using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lidgren.Network;
using RageCoop.Core;
using System.Net;

namespace RageCoop.Server.Scripting
{
    public static class API
    {
        public static class Events
        {
            #region DELEGATES
            public delegate void EmptyEvent();
            public delegate void PlayerConnect(Client client);
            public delegate void PlayerDisconnect(Client client);

            #endregion
            public static event EventHandler<ChatEventArgs> OnChatMessage;
            public static event EventHandler<HandshakeEventArgs> OnPlayerHandshake;
            public static event PlayerConnect OnPlayerConnected;
            public static event PlayerDisconnect OnPlayerDisconnected;
            public static void ClearHandlers()
            {
                foreach (Delegate d in OnChatMessage.GetInvocationList())
                {
                    OnChatMessage -= (EventHandler<ChatEventArgs>)d;
                }
                foreach (Delegate d in OnPlayerHandshake.GetInvocationList())
                {
                    OnPlayerHandshake -= (EventHandler<HandshakeEventArgs>)d;
                }
                foreach (Delegate d in OnPlayerConnected.GetInvocationList())
                {
                    OnPlayerConnected -= (PlayerConnect)d;
                }
                foreach (Delegate d in OnPlayerDisconnected.GetInvocationList())
                {
                    OnPlayerDisconnected -= (PlayerDisconnect)d;
                }
            }
            #region INVOKE
            internal static void InvokeOnChatMessage(Packets.ChatMessage p,NetConnection con) 
            { 
                OnChatMessage?.Invoke(null,new ChatEventArgs() {
                Sender=Util.GetClientByNetID(con.RemoteUniqueIdentifier),
                Message=p.Message
                }); 
            }
            internal static void InvokePlayerConnected(Client client) 
            { OnPlayerConnected?.Invoke(client); }
            internal static void InvokePlayerDisconnected(Client client) 
            { OnPlayerDisconnected?.Invoke(client); }
            internal static void InvokePlayerHandshake(HandshakeEventArgs args)
            {
                OnPlayerHandshake?.Invoke(null, args);
            }
            #endregion
        }

        #region FUNCTIONS
        /// <summary>
        /// Send a native call (Function.Call) to all players.
        /// Keys = int, float, bool, string and lvector3
        /// </summary>
        /// <param name="hash">The hash (Example: 0x25223CA6B4D20B7F = GET_CLOCK_HOURS)</param>
        /// <param name="args">The arguments (Example: string = int, object = 5)</param>
        public static void SendNativeCallToAll(GTA.Native.Hash hash, params object[] args)
        {
            try
            {
                if (Server.MainNetServer.ConnectionsCount == 0)
                {
                    return;
                }

                if (args != null && args.Length == 0)
                {
                    Program.Logger.Error($"[ServerScript->SendNativeCallToAll(ulong hash, params object[] args)]: args is not null!");
                    return;
                }

                Packets.NativeCall packet = new()
                {
                    Hash = (ulong)hash,
                    Args = new List<object>(args) ?? new List<object>()
                };

                NetOutgoingMessage outgoingMessage = Server.MainNetServer.CreateMessage();
                packet.Pack(outgoingMessage);
                Server.MainNetServer.SendMessage(outgoingMessage, Server.MainNetServer.Connections, NetDeliveryMethod.ReliableOrdered, (byte)ConnectionChannel.Native);
            }
            catch (Exception e)
            {
                Program.Logger.Error($">> {e.Message} <<>> {e.Source ?? string.Empty} <<>> {e.StackTrace ?? string.Empty} <<");
            }
        }

        /// <summary>
        /// Get a list of all Clients
        /// </summary>
        /// <returns>All clients as a dictionary indexed by NetID</returns>
        public static Dictionary<long, Client> GetAllClients()
        {
            return Server.Clients;
        }

        /// <summary>
        /// Get the client by its username
        /// </summary>
        /// <param name="username">The username to search for (non case-sensitive)</param>
        /// <returns>The Client from this user or null</returns>
        public static Client GetClientByUsername(string username)
        {
            return Server.Clients.Values.FirstOrDefault(x => x.Player.Username.ToLower() == username.ToLower());
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
                Program.Logger.Error($">> {e.Message} <<>> {e.Source ?? string.Empty} <<>> {e.StackTrace ?? string.Empty} <<");
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

        /// <summary>
        /// Register a class of events
        /// </summary>
        /// <typeparam name="T">The name of your class with functions</typeparam>
        public static void RegisterEvents<T>()
        {
            Server.RegisterEvents<T>();
        }
        #endregion
    }
}
