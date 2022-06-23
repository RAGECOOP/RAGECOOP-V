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
    public class APIEvents
    {
        #region INTERNAL
        internal Dictionary<int, List<Action<CustomEventReceivedArgs>>> CustomEventHandlers = new();
        #endregion
        public event EventHandler<ChatEventArgs> OnChatMessage;
        public event EventHandler<HandshakeEventArgs> OnPlayerHandshake;
        /// <summary>
        /// Will be invoked when a player is connected, but this player might not be ready yet(client resources not loaded), using <see cref="OnPlayerReady"/> is recommended.
        /// </summary>
        public event EventHandler<Client> OnPlayerConnected;
        /// <summary>
        /// Will be invoked after the client connected and all resources(if any) have been loaded.
        /// </summary>
        public event EventHandler<Client> OnPlayerReady;
        public event EventHandler<Client> OnPlayerDisconnected;
        /// <summary>
        /// Will be invoked before registered handlers
        /// </summary>
        public event EventHandler<OnCommandEventArgs> OnCommandReceived;
        /// <summary>
        /// Invoked everytime a player's main ped has been updated
        /// </summary>
        public event EventHandler<Client> OnPlayerUpdate;
        internal void ClearHandlers()
        {
            OnChatMessage=null;
            OnPlayerHandshake=null;
            OnPlayerConnected=null;
            OnPlayerReady=null;
            OnPlayerDisconnected=null;
            // OnCustomEventReceived=null;
            OnCommandReceived=null;
            OnPlayerUpdate=null;
        }
        #region INVOKE
        internal void InvokeOnChatMessage(Packets.ChatMessage p, Client sender)
        {
            OnChatMessage?.Invoke(this, new ChatEventArgs()
            {
                Sender=sender,
                Message=p.Message
            });
        }
        internal void InvokePlayerConnected(Client client)
        { OnPlayerConnected?.Invoke(this,client); }
        internal void InvokePlayerReady(Client client)
        { OnPlayerReady?.Invoke(this, client); }
        internal void InvokePlayerDisconnected(Client client)
        { OnPlayerDisconnected?.Invoke(this,client); }
        internal void InvokePlayerHandshake(HandshakeEventArgs args)
        { OnPlayerHandshake?.Invoke(this, args); }

        internal void InvokeCustomEventReceived(Packets.CustomEvent p, Client sender)
        {
            var args = new CustomEventReceivedArgs() { Hash=p.Hash, Args=p.Args, Sender=sender };
            List<Action<CustomEventReceivedArgs>> handlers;
            if (CustomEventHandlers.TryGetValue(p.Hash, out handlers))
            {
                handlers.ForEach((x) => { x.Invoke(args); });
            }
        }
        internal bool InvokeOnCommandReceived(string cname, string[] cargs, Client sender)
        {
            var args = new OnCommandEventArgs()
            {
                Name=cname,
                Args=cargs,
                Sender=sender
            };
            OnCommandReceived?.Invoke(this, args);
            return args.Cancel;
        }
        internal void InvokePlayerUpdate(Client client)
        {
            OnPlayerUpdate?.Invoke(this, client);
        }
        #endregion
    }
    public class API
    {
        private readonly Server Server;
        internal API(Server server)
        {
            Server=server;
        }
        public APIEvents Events { get; set; }=new APIEvents();
        #region FUNCTIONS
        /*
        /// <summary>
        /// Send a native call (Function.Call) to all players.
        /// Keys = int, float, bool, string and lvector3
        /// </summary>
        /// <param name="hash">The hash (Example: 0x25223CA6B4D20B7F = GET_CLOCK_HOURS)</param>
        /// <param name="args">The arguments (Example: string = int, object = 5)</param>
        public void SendNativeCallToAll(GTA.Native.Hash hash, params object[] args)
        {
            try
            {
                if (Server.MainNetServer.ConnectionsCount == 0)
                {
                    return;
                }

                if (args != null && args.Length == 0)
                {
                    Server.Logger?.Error($"[ServerScript->SendNativeCallToAll(ulong hash, params object[] args)]: args is not null!");
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
                Server.Logger?.Error($">> {e.Message} <<>> {e.Source ?? string.Empty} <<>> {e.StackTrace ?? string.Empty} <<");
            }
        }
        */
        /// <summary>
        /// Get a list of all Clients
        /// </summary>
        /// <returns>All clients as a dictionary indexed by NetID</returns>
        public Dictionary<long, Client> GetAllClients()
        {
            return new(Server.Clients);
        }

        /// <summary>
        /// Get the client by its username
        /// </summary>
        /// <param name="username">The username to search for (non case-sensitive)</param>
        /// <returns>The Client from this user or null</returns>
        public Client GetClientByUsername(string username)
        {
            return Server.Clients.Values.FirstOrDefault(x => x.Username.ToLower() == username.ToLower());
        }

        /// <summary>
        /// Send a chat message to all players
        /// </summary>
        /// <param name="message">The chat message</param>
        /// <param name="username">The username which send this message (default = "Server")</param>
        public void SendChatMessage(string message, List<Client> targets = null, string username = "Server")
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
                Server.Logger?.Error($">> {e.Message} <<>> {e.Source ?? string.Empty} <<>> {e.StackTrace ?? string.Empty} <<");
            }
        }
        public void SendChatMessage(string message, Client target, string username = "Server")
        {
            try
            {
                Server.SendChatMessage(username, message, target.Connection);
            }
            catch (Exception e)
            {
                Server.Logger?.Error($">> {e.Message} <<>> {e.Source ?? string.Empty} <<>> {e.StackTrace ?? string.Empty} <<");
            }
        }

        /// <summary>
        /// Send CleanUpWorld to all players to delete all objects created by the server
        /// </summary>
        public void SendCleanUpWorldToAll(List<Client> clients = null)
        {
            if (Server.MainNetServer.ConnectionsCount == 0)
            {
                return;
            }
            NetOutgoingMessage outgoingMessage = Server.MainNetServer.CreateMessage();
            outgoingMessage.Write((byte)PacketTypes.CleanUpWorld);
            if (clients == null)
            {
                Server.MainNetServer.SendToAll(outgoingMessage, NetDeliveryMethod.ReliableOrdered, (byte)ConnectionChannel.Default);
            }
            else
            {
                clients.ForEach(client => { Server.MainNetServer.SendMessage(outgoingMessage,client.Connection, NetDeliveryMethod.ReliableOrdered, (byte)ConnectionChannel.Default); });
            }    
        }

        /// <summary>
        /// Register a new command chat command (Example: "/test")
        /// </summary>
        /// <param name="name">The name of the command (Example: "test" for "/test")</param>
        /// <param name="usage">How to use this message (argsLength required!)</param>
        /// <param name="argsLength">The length of args (Example: "/message USERNAME MESSAGE" = 2) (usage required!)</param>
        /// <param name="callback">Create a new function!</param>
        public void RegisterCommand(string name, string usage, short argsLength, Action<CommandContext> callback)
        {
            Server.RegisterCommand(name, usage, argsLength, callback);
        }
        /// <summary>
        /// Register a new command chat command (Example: "/test")
        /// </summary>
        /// <param name="name">The name of the command (Example: "test" for "/test")</param>
        /// <param name="callback">Create a new function!</param>
        public void RegisterCommand(string name, Action<CommandContext> callback)
        {
            Server.RegisterCommand(name, callback);
        }

        /// <summary>
        /// Register a class of commands
        /// </summary>
        /// <typeparam name="T">The name of your class with functions</typeparam>
        public void RegisterCommands<T>()
        {
            Server.RegisterCommands<T>();
        }

        /// <summary>
        /// Send an event and data to the specified clients. Use <see cref="Client.SendCustomEvent(int, List{object})"/> if you want to send event to individual client.
        /// </summary>
        /// <param name="eventHash">An unique identifier of the event</param>
        /// <param name="args">The objects conataing your data, supported types: byte, short, ushort, int, uint, long, ulong, float, bool, string.</param>
        /// <param name="targets">The target clients to send. Leave it null to send to all clients</param>
        public void SendCustomEvent(int eventHash,List<object> args,List<Client> targets=null)
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
        public void RegisterCustomEventHandler(int hash,Action<CustomEventReceivedArgs> handler)
        {
            List<Action<CustomEventReceivedArgs>> handlers;
            lock (Events.CustomEventHandlers)
            {
                if (!Events.CustomEventHandlers.TryGetValue(hash,out handlers))
                {
                    Events.CustomEventHandlers.Add(hash, handlers = new List<Action<CustomEventReceivedArgs>>());
                }
                handlers.Add(handler);
            }
        }
        public void RegisterCustomEventHandler(string name, Action<CustomEventReceivedArgs> handler)
        {
            RegisterCustomEventHandler(CustomEvents.Hash(name), handler);
        }
        public Logger GetLogger()
        {
            return Server.Logger;
        }
        #endregion
    }
}
