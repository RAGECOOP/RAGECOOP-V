using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lidgren.Network;
using RageCoop.Core;
using RageCoop.Core.Scripting;
using System.Net;

namespace RageCoop.Server.Scripting
{
    public static class API
    {
        #region INTERNAL
        internal static Dictionary<int, List<Action<CustomEventReceivedArgs>>> CustomEventHandlers = new();
        #endregion
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
            /// <summary>
            /// Will be invoked before registered handlers
            /// </summary>
            public static event EventHandler<OnCommandEventArgs> OnCommandReceived;
            /// <summary>
            /// Invoked everytime a player's main ped has been updated
            /// </summary>
            public static event EventHandler<Client> OnPlayerUpdate;
            /*
            /// <summary>
            /// This will be invoked when a CustomEvent is received from one client.
            /// </summary>
            public static event EventHandler<CustomEventReceivedArgs> OnCustomEventReceived;
            */
            internal static void ClearHandlers()
            {
                OnChatMessage=null;
                OnPlayerHandshake=null;
                OnPlayerConnected=null;
                OnPlayerDisconnected=null;
                // OnCustomEventReceived=null;
                OnCommandReceived=null;
                OnPlayerUpdate=null;
            }
            #region INVOKE
            internal static void InvokeOnChatMessage(Packets.ChatMessage p,Client sender) 
            { 
                OnChatMessage?.Invoke(null,new ChatEventArgs() {
                Sender=sender,
                Message=p.Message
                }); 
            }
            internal static void InvokePlayerConnected(Client client) 
            { OnPlayerConnected?.Invoke(client); }
            internal static void InvokePlayerDisconnected(Client client) 
            { OnPlayerDisconnected?.Invoke(client); }
            internal static void InvokePlayerHandshake(HandshakeEventArgs args)
            { OnPlayerHandshake?.Invoke(null, args); }

            internal static void InvokeCustomEventReceived(Packets.CustomEvent p,Client sender)
            {
                var args = new CustomEventReceivedArgs() { Hash=p.Hash, Args=p.Args, Sender=sender };
                List<Action<CustomEventReceivedArgs>> handlers;
                if (CustomEventHandlers.TryGetValue(p.Hash,out handlers))
                {
                    handlers.ForEach((x) => { x.Invoke(args); });
                }
            }
            internal static bool InvokeOnCommandReceived(string cname,string[] cargs,Client sender)
            {
                var args = new OnCommandEventArgs()
                {
                    Name=cname,
                    Args=cargs,
                    Sender=sender
                };
                OnCommandReceived?.Invoke(null,args);
                return args.Cancel;
            }
            internal static void InvokePlayerUpdate(Client client)
            {
                OnPlayerUpdate?.Invoke(null,client);
            }
            #endregion
        }

        #region FUNCTIONS
        /*
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
        */
        /// <summary>
        /// Get a list of all Clients
        /// </summary>
        /// <returns>All clients as a dictionary indexed by NetID</returns>
        public static Dictionary<long, Client> GetAllClients()
        {
            return new(Server.Clients);
        }

        /// <summary>
        /// Get the client by its username
        /// </summary>
        /// <param name="username">The username to search for (non case-sensitive)</param>
        /// <returns>The Client from this user or null</returns>
        public static Client GetClientByUsername(string username)
        {
            return Server.Clients.Values.FirstOrDefault(x => x.Username.ToLower() == username.ToLower());
        }

        /// <summary>
        /// Send a chat message to all players
        /// </summary>
        /// <param name="message">The chat message</param>
        /// <param name="username">The username which send this message (default = "Server")</param>
        public static void SendChatMessage(string message, List<Client> targets = null, string username = "Server")
        {
            try
            {
                if (Server.MainNetServer.ConnectionsCount == 0)
                {
                    return;
                }
                targets ??= new(Server.Clients.Values);
                foreach(Client client in targets)
                {
                    Server.SendChatMessage(username, message, client.Connection);
                }
            }
            catch (Exception e)
            {
                Program.Logger.Error($">> {e.Message} <<>> {e.Source ?? string.Empty} <<>> {e.StackTrace ?? string.Empty} <<");
            }
        }
        public static void SendChatMessage(string message, Client target, string username = "Server")
        {
            try
            {
                Server.SendChatMessage(username, message, target.Connection);
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
        /// Send an event and data to the specified clients.
        /// </summary>
        /// <param name="eventHash">An unique identifier of the event</param>
        /// <param name="args">The objects conataing your data, supported types: byte, short, ushort, int, uint, long, ulong, float, bool, string.</param>
        /// <param name="targets">The target clients to send.</param>
        public static void SendCustomEvent(int eventHash,List<object> args,List<Client> targets=null)
        {
            targets ??= new(Server.Clients.Values);
            var p = new Packets.CustomEvent()
            {
                Args=args,
                Hash=eventHash
            };
            foreach(var c in targets)
            {
                Server.Send(p,c,ConnectionChannel.Event,NetDeliveryMethod.ReliableOrdered);
            }
        }
        /// <summary>
        /// Register an handler to the specifed event hash, one event can have multiple handlers.
        /// </summary>
        /// <param name="hash">An unique identifier of the event, you can hash your event name with <see cref="Core.Scripting.CustomEvents.Hash(string)"/></param>
        /// <param name="handler">An handler to be invoked when the event is received from the server. This will be invoked from main thread.
        public static void RegisterCustomEventHandler(int hash,Action<CustomEventReceivedArgs> handler)
        {
            List<Action<CustomEventReceivedArgs>> handlers;
            lock (CustomEventHandlers)
            {
                if (!CustomEventHandlers.TryGetValue(hash,out handlers))
                {
                    CustomEventHandlers.Add(hash, handlers = new List<Action<CustomEventReceivedArgs>>());
                }
                handlers.Add(handler);
            }
        }
        public static void RegisterCustomEventHandler(string name, Action<CustomEventReceivedArgs> handler)
        {
            RegisterCustomEventHandler(CustomEvents.Hash(name), handler);
        }
        public static Logger GetLogger()
        {
            return Program.Logger;
        }
        #endregion
    }
}
