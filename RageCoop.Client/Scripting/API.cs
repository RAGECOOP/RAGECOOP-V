#undef DEBUG
using System.Collections.Generic;
using System;
using System.Linq;
using RageCoop.Core;

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
        /// Arguments
        /// </summary>
        public List<object> Args { get; set; }
    }
    /// <summary>
    /// Provides vital functionality to interact with RAGECOOP
    /// </summary>
    public static class API
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
                get { return Main.Settings.Username; }
                set
                {
                    if (IsOnServer || string.IsNullOrEmpty(value))
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
            public static event EmptyEvent OnPlayerDied;

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


            #region INVOKE
            internal static void InvokeVehicleSpawned(SyncedVehicle v) { OnVehicleSpawned?.Invoke(null, v); }
            internal static void InvokeVehicleDeleted(SyncedVehicle v) { OnVehicleDeleted?.Invoke(null, v); }
            internal static void InvokePedSpawned(SyncedPed p) { OnPedSpawned?.Invoke(null, p); }
            internal static void InvokePedDeleted(SyncedPed p) { OnPedDeleted?.Invoke(null, p); }
            internal static void InvokePlayerDied() { OnPlayerDied?.Invoke(); }
            internal static void InvokeTick() { OnTick?.Invoke(); }

            internal static void InvokeCustomEventReceived(Packets.CustomEvent p)
            {
                var args = new CustomEventReceivedArgs() { Hash=p.Hash, Args=p.Args};

                // Main.Logger.Debug($"CustomEvent:\n"+args.Args.DumpWithType());
                
                List<Action<CustomEventReceivedArgs>> handlers;
                if (CustomEventHandlers.TryGetValue(p.Hash, out handlers))
                {
                    handlers.ForEach((x) => { x.Invoke(args); });
                }
            }
            #endregion
            internal static void ClearHandlers()
            {
                OnPlayerDied=null;
                OnTick=null;
                OnPedDeleted=null;
                OnPedSpawned=null;
                OnVehicleDeleted=null;
                OnVehicleSpawned=null;
            }
        }

        #region FUNCTIONS
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
        /// Get a <see cref="Core.Logger"/> that RAGECOOP is currently using.
        /// </summary>
        /// <returns></returns>
        public static Logger Logger
        {
            get
            {
                return Main.Logger;
            }
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
        /// Disconnect from the server
        /// </summary>
        public static void Disconnect()
        {
            Networking.ToggleConnection(null);
        }

        /// <summary>
        /// Check if the player is already on a server
        /// </summary>
        public static bool IsOnServer
        {
            get { return Networking.IsOnServer; }
        }

        /// <summary>
        /// Get the local player's ID
        /// </summary>
        /// <returns>PlayerID</returns>
        public static int LocalPlayerID
        {
            get { return Main.LocalPlayerID; }
        }

        /// <summary>
        /// Check if a RAGECOOP menu is visible
        /// </summary>
        public static bool IsMenuVisible
        {
            get { return Menus.CoopMenu.MenuPool.AreAnyVisible; }
        }

        /// <summary>
        /// Check if the RAGECOOP chat is visible
        /// </summary>
        public static bool IsChatFocused
        {
            get { return Main.MainChat.Focused; }
        }

        /// <summary>
        /// Check if the RAGECOOP list of players is visible
        /// </summary>
        public static bool IsPlayerListVisible
        {
            get { return Util.GetTickCount64() - PlayerList.Pressed < 5000; }
        }

        /// <summary>
        /// Get the version of RAGECOOP
        /// </summary>
        public static string CurrentVersion
        {
            get { return Main.CurrentVersion; }
        }
        /// <summary>
        /// Send an event and data to the server.
        /// </summary>
        /// <param name="eventHash">An unique identifier of the event</param>
        /// <param name="args">The objects conataing your data, supported types: 
        /// byte, short, ushort, int, uint, long, ulong, float, bool, string.</param>
        public static void SendCustomEvent(int eventHash, List<object> args)
        {
            var p = new Packets.CustomEvent()
            {
                Args=args,
                Hash=eventHash
            };
            Networking.Send(p, ConnectionChannel.Event, Lidgren.Network.NetDeliveryMethod.ReliableOrdered);
        }
        /// <summary>
        /// Send an event and data to the server.
        /// </summary>
        /// <param name="eventHash"></param>
        /// <param name="args"></param>
        public static void SendCustomEvent(int eventHash,params object[] args)
        {
            var p = new Packets.CustomEvent()
            {
                Args=new List<object>(args),
                Hash=eventHash
            };
            Networking.Send(p, ConnectionChannel.Event, Lidgren.Network.NetDeliveryMethod.ReliableOrdered);
        }

        /// <summary>
        /// Register an handler to the specifed event hash, one event can have multiple handlers. This will be invoked from backgound thread, use <see cref="QueueAction(Action)"/> in the handler to dispatch code to script thread.
        /// </summary>
        /// <param name="hash">An unique identifier of the event, you can hash your event name with <see cref="Core.Scripting.CustomEvents.Hash(string)"/></param>
        /// <param name="handler">An handler to be invoked when the event is received from the server. </param>
        public static void RegisterCustomEventHandler(int hash, Action<CustomEventReceivedArgs> handler)
        {
            List<Action<CustomEventReceivedArgs>> handlers;
            lock (CustomEventHandlers)
            {
                if (!CustomEventHandlers.TryGetValue(hash, out handlers))
                {
                    CustomEventHandlers.Add(hash, handlers = new List<Action<CustomEventReceivedArgs>>());
                }
                handlers.Add(handler);
            }
        }
        #endregion
    }
}
