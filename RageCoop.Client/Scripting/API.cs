#undef DEBUG
using GTA;
using Newtonsoft.Json;
using RageCoop.Core;
using System;
using System.Collections.Generic;
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
    /// Provides vital functionality to interact with RAGECOOP
    /// </summary>
    public class API
    {
        #region INTERNAL
        internal static Dictionary<int, List<Action<CustomEventReceivedArgs>>> CustomEventHandlers = new Dictionary<int, List<Action<CustomEventReceivedArgs>>>();
        #endregion
        /// <summary>
        /// Client configuration, this will conflict with server-side config.
        /// </summary>
        public static class Config
        {
            /// <summary>
            /// Get or set local player's username, set won't be effective if already connected to a server.
            /// </summary>
            public static string Username
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
            public static bool EnableAutoRespawn { get; set; } = true;

            /// <summary>
            /// Get or set player's blip color
            /// </summary>
            public static BlipColor BlipColor { get; set; } = BlipColor.White;

            /// <summary>
            /// Get or set player's blip sprite
            /// </summary>
            public static BlipSprite BlipSprite { get; set; } = BlipSprite.Standard;

            /// <summary>
            /// Get or set scale of player's blip
            /// </summary>
            public static float BlipScale { get; set; } = 1;

        }
        /// <summary>
        /// Base events for RageCoop
        /// </summary>
        public static class Events
        {
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
            public static event EventHandler<string> OnPlayerDied;

            /// <summary>
            /// A local vehicle is spawned
            /// </summary>
            public static event EventHandler<SyncedVehicle> OnVehicleSpawned;

            /// <summary>
            /// A local vehicle is deleted
            /// </summary>
            public static event EventHandler<SyncedVehicle> OnVehicleDeleted;

            /// <summary>
            /// A local ped is spawned
            /// </summary>
            public static event EventHandler<SyncedPed> OnPedSpawned;

            /// <summary>
            /// A local ped is deleted
            /// </summary>
            public static event EventHandler<SyncedPed> OnPedDeleted;

            /// <summary>
            /// This is equivalent of <see cref="GTA.Script.Tick"/>.
            /// </summary>
            public static event EmptyEvent OnTick;

            /// <summary>
            /// This is equivalent of <see cref="Script.KeyDown"/>
            /// </summary>
            public static KeyEventHandler OnKeyDown;

            /// <summary>
            /// This is equivalent of <see cref="Script.KeyUp"/>
            /// </summary>
            public static KeyEventHandler OnKeyUp;

            #region INVOKE
            internal static void InvokeVehicleSpawned(SyncedVehicle v) { OnVehicleSpawned?.Invoke(null, v); }
            internal static void InvokeVehicleDeleted(SyncedVehicle v) { OnVehicleDeleted?.Invoke(null, v); }
            internal static void InvokePedSpawned(SyncedPed p) { OnPedSpawned?.Invoke(null, p); }
            internal static void InvokePedDeleted(SyncedPed p) { OnPedDeleted?.Invoke(null, p); }
            internal static void InvokePlayerDied(string m) { OnPlayerDied?.Invoke(null, m); }
            internal static void InvokeTick() { OnTick?.Invoke(); }

            internal static void InvokeKeyDown(object s, KeyEventArgs e) { OnKeyDown?.Invoke(s, e); }

            internal static void InvokeKeyUp(object s, KeyEventArgs e) { OnKeyUp?.Invoke(s, e); }

            internal static void InvokeCustomEventReceived(Packets.CustomEvent p)
            {
                var args = new CustomEventReceivedArgs() { Hash = p.Hash, Args = p.Args };

                // Main.Logger.Debug($"CustomEvent:\n"+args.Args.DumpWithType());

                if (CustomEventHandlers.TryGetValue(p.Hash, out List<Action<CustomEventReceivedArgs>> handlers))
                {
                    handlers.ForEach((x) => { x.Invoke(args); });
                }
            }
            #endregion
        }

        #region PROPERTIES

        /// <summary>
        /// Get the local player's ID
        /// </summary>
        /// <returns>PlayerID</returns>
        public static int LocalPlayerID => Main.LocalPlayerID;

        /// <summary>
        /// Check if player is connected to a server
        /// </summary>
        public static bool IsOnServer => Networking.IsOnServer;

        /// <summary>
        /// Get an <see cref="System.Net.IPEndPoint"/> that the player is currently connected to, or null if not connected to the server
        /// </summary>
        public static System.Net.IPEndPoint ServerEndPoint => Networking.IsOnServer ? Networking.ServerConnection?.RemoteEndPoint : null;

        /// <summary>
        /// Check if a RAGECOOP menu is visible
        /// </summary>
        public static bool IsMenuVisible => Menus.CoopMenu.MenuPool.AreAnyVisible;

        /// <summary>
        /// Check if the RAGECOOP chat is visible
        /// </summary>
        public static bool IsChatFocused => Main.MainChat.Focused;

        /// <summary>
        /// Check if the RAGECOOP list of players is visible
        /// </summary>
        public static bool IsPlayerListVisible => Util.GetTickCount64() - PlayerList.Pressed < 5000;

        /// <summary>
        /// Get the version of RAGECOOP
        /// </summary>
        public static Version CurrentVersion => Main.Version;

        /// <summary>
        /// Get a <see cref="Core.Logger"/> that RAGECOOP is currently using.
        /// </summary>
        /// <returns></returns>
        public static Logger Logger => Main.Logger;
        /// <summary>
        /// Get all players indexed by their ID
        /// </summary>
        public static Dictionary<int, Player> Players => new Dictionary<int, Player>(PlayerList.Players);

        #endregion

        #region FUNCTIONS
        /// <summary>
        /// Connect to a server
        /// </summary>
        /// <param name="address">Address of the server, e.g. 127.0.0.1:4499</param>
        /// <exception cref="InvalidOperationException">When a connection is active or being established</exception>
        public static void Connect(string address)
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
        public static void Disconnect()
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
        public static List<ServerInfo> ListServers()
        {
            return JsonConvert.DeserializeObject<List<ServerInfo>>(HttpHelper.DownloadString(Main.Settings.MasterServer));
        }

        /// <summary>
        /// Send a local chat message to this player
        /// </summary>
        /// <param name="from">Name of the sender</param>
        /// <param name="message">The player's message</param>
        public static void LocalChatMessage(string from, string message)
        {
            Main.MainChat.AddMessage(from, message);
        }

        /// <summary>
        /// Send a chat message or command to server/other players
        /// </summary>
        /// <param name="message"></param>
        public static void SendChatMessage(string message)
        {
            Networking.SendChatMessage(message);
        }

        /// <summary>
        /// Queue an action to be executed on next tick.
        /// </summary>
        /// <param name="a"></param>
        public static void QueueAction(Action a)
        {
            Main.QueueAction(a);
        }

        /// <summary>
        /// Queue an action to be executed on next tick, allowing you to call scripting API from another thread.
        /// </summary>
        /// <param name="a"> An <see cref="Func{T, TResult}"/> to be executed with a return value indicating whether it can be removed after execution.</param>
        public static void QueueAction(Func<bool> a)
        {
            Main.QueueAction(a);
        }

        /// <summary>
        /// Send an event and data to the server.
        /// </summary>
        /// <param name="eventHash">An unique identifier of the event</param>
        /// <param name="args">The objects conataing your data, see <see cref="CustomEventReceivedArgs"/> for a list of supported types</param>
        public static void SendCustomEvent(int eventHash, params object[] args)
        {

            Networking.Peer.SendTo(new Packets.CustomEvent()
            {
                Args = args,
                Hash = eventHash
            }, Networking.ServerConnection, ConnectionChannel.Event, Lidgren.Network.NetDeliveryMethod.ReliableOrdered);
        }

        /// <summary>
        /// Register an handler to the specifed event hash, one event can have multiple handlers. This will be invoked from backgound thread, use <see cref="QueueAction(Action)"/> in the handler to dispatch code to script thread.
        /// </summary>
        /// <param name="hash">An unique identifier of the event, you can hash your event name with <see cref="Core.Scripting.CustomEvents.Hash(string)"/></param>
        /// <param name="handler">An handler to be invoked when the event is received from the server. </param>
        public static void RegisterCustomEventHandler(int hash, Action<CustomEventReceivedArgs> handler)
        {
            lock (CustomEventHandlers)
            {
                if (!CustomEventHandlers.TryGetValue(hash, out List<Action<CustomEventReceivedArgs>> handlers))
                {
                    CustomEventHandlers.Add(hash, handlers = new List<Action<CustomEventReceivedArgs>>());
                }
                handlers.Add(handler);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static void RequestSharedFile(string name, Action<string> callback)
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
        #endregion
    }
}
