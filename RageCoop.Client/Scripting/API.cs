#undef DEBUG
using GTA;
using Newtonsoft.Json;
using RageCoop.Core;
using RageCoop.Core.Scripting;
using SHVDN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace RageCoop.Client.Scripting
{
    /// <summary>
    /// 
    /// </summary>
    public class CustomEventReceivedArgs : EventArgs
    {
        /// <summary>
        /// The event hash
        /// </summary>
        public int Hash { get; set; }
        /// <summary>
        /// Supported types: byte, short, ushort, int, uint, long, ulong, float, bool, string, Vector3, Quaternion
        /// </summary>
        public object[] Args { get; set; }
    }


    /// <summary>
    /// Client configuration, this will conflict with server-side config.
    /// </summary>
    public class ClientConfig : MarshalByRefObject
    {
        /// <summary>
        /// Get or set local player's username, set won't be effective if already connected to a server.
        /// </summary>
        public string Username
        {
            get => Main.Settings.Username;
            set
            {
                if (Networking.IsOnServer || string.IsNullOrEmpty(value))
                {
                    return;
                }
                Main.Settings.Username = value;
            }
        }
        /// <summary>
        /// Enable automatic respawn for this player.
        /// </summary>
        public bool EnableAutoRespawn { get; set; } = true;

        /// <summary>
        /// Get or set player's blip color
        /// </summary>
        public BlipColor BlipColor { get; set; } = BlipColor.White;

        /// <summary>
        /// Get or set player's blip sprite
        /// </summary>
        public BlipSprite BlipSprite { get; set; } = BlipSprite.Standard;

        /// <summary>
        /// Get or set scale of player's blip
        /// </summary>
        public float BlipScale { get; set; } = 1;

    }


    /// <summary>
    /// Base events for RageCoop
    /// </summary>
    public class ClientEvents : MarshalByRefObject
    {
        internal Dictionary<int, List<Action<CustomEventReceivedArgs>>> CustomEventHandlers = new Dictionary<int, List<Action<CustomEventReceivedArgs>>>();

        #region DELEGATES
        /// <summary>
        /// 
        /// </summary>
        public delegate void EmptyEvent();
        /// <summary>
        /// 
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="args"></param>
        public delegate void CustomEvent(int hash, List<object> args);
        #endregion
        /// <summary>
        /// The local player is dead
        /// </summary>
        public event EmptyEvent OnPlayerDied;

        /// <summary>
        /// A local vehicle is spawned
        /// </summary>
        public event EventHandler<SyncedVehicle> OnVehicleSpawned;

        /// <summary>
        /// A local vehicle is deleted
        /// </summary>
        public event EventHandler<SyncedVehicle> OnVehicleDeleted;

        /// <summary>
        /// A local ped is spawned
        /// </summary>
        public event EventHandler<SyncedPed> OnPedSpawned;

        /// <summary>
        /// A local ped is deleted
        /// </summary>
        public event EventHandler<SyncedPed> OnPedDeleted;

        #region INVOKE
        internal void InvokeVehicleSpawned(SyncedVehicle v) { OnVehicleSpawned?.Invoke(null, v); }
        internal void InvokeVehicleDeleted(SyncedVehicle v) { OnVehicleDeleted?.Invoke(null, v); }
        internal void InvokePedSpawned(SyncedPed p) { OnPedSpawned?.Invoke(null, p); }
        internal void InvokePedDeleted(SyncedPed p) { OnPedDeleted?.Invoke(null, p); }
        internal void InvokePlayerDied() { OnPlayerDied?.Invoke(); }

        internal void InvokeCustomEventReceived(Packets.CustomEvent p)
        {
            var args = new CustomEventReceivedArgs() { Hash = p.Hash, Args = p.Args };

            // Main.Logger.Debug($"CustomEvent:\n"+args.Args.DumpWithType());

            if (CustomEventHandlers.TryGetValue(p.Hash, out List<Action<CustomEventReceivedArgs>> handlers))
            {
                handlers.ForEach((x) => { x.Invoke(args); });
            }

            if (Util.IsPrimaryDomain)
            {
                ResourceDomain.DoCallBack("CustomEvent",p);
            }
        }
        #endregion
    }

    /// <summary>
    /// Provides vital functionality to interact with RAGECOOP
    /// </summary>
    public class API : MarshalByRefObject
    {
        static API()
        {
            if (!Util.IsPrimaryDomain)
            {
                ResourceDomain.RegisterCallBackForCurrentDomain("CustomEvent",
                    (data) => {
                        Events.InvokeCustomEventReceived(data as Packets.CustomEvent);
                    });
            }
        }
        static API Instance;
        private API() { }

        /// <summary>
        /// Get an instance to bridge data between domains
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static API GetInstance()
        {
            if (Instance != null) { return Instance; }
            if (Util.IsPrimaryDomain)
            {
                Instance = new API();
            }
            else
            {
                Instance = AppDomain.CurrentDomain.GetData("RageCoop.Client.API") as API;
            }
            return Instance;
        }

        public static ClientEvents Events = new ClientEvents();
        public ClientConfig Config = new ClientConfig();

        #region PROPERTIES

        /// <summary>
        /// Get the local player's ID
        /// </summary>
        /// <returns>PlayerID</returns>
        public int LocalPlayerID => Main.LocalPlayerID;

        /// <summary>
        /// Check if player is connected to a server
        /// </summary>
        public bool IsOnServer => Networking.IsOnServer;

        /// <summary>
        /// Get an <see cref="System.Net.IPEndPoint"/> that the player is currently connected to, or null if not connected to the server
        /// </summary>
        public System.Net.IPEndPoint ServerEndPoint => Networking.IsOnServer ? Networking.ServerConnection?.RemoteEndPoint : null;

        /// <summary>
        /// Check if a RAGECOOP menu is visible
        /// </summary>
        public bool IsMenuVisible => Menus.CoopMenu.MenuPool.AreAnyVisible;

        /// <summary>
        /// Check if the RAGECOOP chat is visible
        /// </summary>
        public bool IsChatFocused => Main.MainChat.Focused;

        /// <summary>
        /// Check if the RAGECOOP list of players is visible
        /// </summary>
        public bool IsPlayerListVisible => Util.GetTickCount64() - PlayerList.Pressed < 5000;

        /// <summary>
        /// Get the version of RAGECOOP
        /// </summary>
        public Version CurrentVersion => Main.Version;

        /// <summary>
        /// Get a <see cref="Core.Logger"/> that RAGECOOP is currently using.
        /// </summary>
        /// <returns></returns>
        public Logger Logger => Main.Logger;
        /// <summary>
        /// Get all players indexed by their ID
        /// </summary>
        public Dictionary<int, Player> Players => new Dictionary<int, Player>(PlayerList.Players);

        #endregion

        #region FUNCTIONS
        public ClientResource GetResource(string name)
        {
            if (Main.Resources.LoadedResources.TryGetValue(name.ToLower(), out var res))
            {
                return res;
            }
            return null;
        }
        /// <summary>
        /// Connect to a server
        /// </summary>
        /// <param name="address">Address of the server, e.g. 127.0.0.1:4499</param>
        /// <exception cref="InvalidOperationException">When a connection is active or being established</exception>
        public void Connect(string address)
        {
            if (Networking.IsOnServer || Networking.IsConnecting)
            {
                throw new InvalidOperationException("Cannot connect to server when another connection is active");
            }
            Networking.ToggleConnection(address);
        }
        /// <summary>
        /// Disconnect from current server or cancel the connection attempt.
        /// </summary>
        public void Disconnect()
        {
            if (Networking.IsOnServer || Networking.IsConnecting)
            {
                Networking.ToggleConnection(null);
            }
        }

        /// <summary>
        /// List all servers from master server address
        /// </summary>
        /// <returns></returns>
        public List<ServerInfo> ListServers()
        {
            return JsonConvert.DeserializeObject<List<ServerInfo>>(HttpHelper.DownloadString(Main.Settings.MasterServer));
        }

        /// <summary>
        /// Send a local chat message to this player
        /// </summary>
        /// <param name="from">Name of the sender</param>
        /// <param name="message">The player's message</param>
        public void LocalChatMessage(string from, string message)
        {
            Main.MainChat.AddMessage(from, message);
        }

        /// <summary>
        /// Send a chat message or command to server/other players
        /// </summary>
        /// <param name="message"></param>
        public void SendChatMessage(string message)
        {
            Networking.SendChatMessage(message);
        }

        /// <summary>
        /// Queue an action to be executed on next tick.
        /// </summary>
        /// <param name="a"></param>
        public void QueueAction(Action a)
        {
            WorldThread.QueueAction(a);
        }
        public void QueueActionAndWait(Action a, int timeout = 15000)
        {
            var done = new AutoResetEvent(false);
            Exception e = null;
            QueueAction(() =>
            {
                try
                {
                    a();
                    done.Set();
                }
                catch (Exception ex) { e = ex; }
            });
            if (e != null) { throw e; }
            else if (!done.WaitOne(timeout)) { throw new TimeoutException(); }
        }

        /// <summary>
        /// Queue an action to be executed on next tick, allowing you to call scripting API from another thread.
        /// </summary>
        /// <param name="a"> An <see cref="Func{T, TResult}"/> to be executed with a return value indicating whether it can be removed after execution.</param>
        public static void QueueAction(Func<bool> a)
        {
            WorldThread.QueueAction(a);
        }

        /// <summary>
        /// Send an event and data to the server.
        /// </summary>
        /// <param name="eventHash">An unique identifier of the event</param>
        /// <param name="args">The objects conataing your data, see <see cref="CustomEventReceivedArgs"/> for a list of supported types</param>
        public void SendCustomEvent(CustomEventHash eventHash, params object[] args)
        {

            Networking.Peer.SendTo(new Packets.CustomEvent()
            {
                Args = args,
                Hash = eventHash
            }, Networking.ServerConnection, ConnectionChannel.Event, Lidgren.Network.NetDeliveryMethod.ReliableOrdered);
        }
        /// <summary>
        /// Send an event and data to the server
        /// </summary>
        /// <param name="flags"></param>
        /// <param name="eventHash">An unique identifier of the event</param>
        /// <param name="args">The objects conataing your data, see <see cref="CustomEventReceivedArgs"/> for a list of supported types</param>
        public void SendCustomEvent(CustomEventFlags flags, CustomEventHash eventHash, params object[] args)
        {
            Networking.Peer.SendTo(new Packets.CustomEvent(flags)
            {
                Args = args,
                Hash = eventHash
            }, Networking.ServerConnection, ConnectionChannel.Event, Lidgren.Network.NetDeliveryMethod.ReliableOrdered);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public void RequestSharedFile(string name, Action<string> callback)
        {
            EventHandler<string> handler = (s, e) =>
            {
                if (e.EndsWith(name))
                {
                    callback(e);
                }
            };
            DownloadManager.DownloadCompleted += handler;
            Networking.GetResponse<Packets.FileTransferResponse>(new Packets.FileTransferRequest()
            {
                Name = name,
            },
            (p) =>
            {
                if (p.Response != FileResponse.Loaded)
                {
                    DownloadManager.DownloadCompleted -= handler;
                    throw new ArgumentException("Requested file was not found on the server: " + name);
                }
            });
        }
        public static void RegisterCustomEventHandler(CustomEventHash hash, Action<CustomEventReceivedArgs> handler)
        {
            lock (Events.CustomEventHandlers)
            {
                if (!Events.CustomEventHandlers.TryGetValue(hash, out List<Action<CustomEventReceivedArgs>> handlers))
                {
                    Events.CustomEventHandlers.Add(hash, handlers = new List<Action<CustomEventReceivedArgs>>());
                }
                handlers.Add(handler);
            }
        }
        #endregion
    }
}
