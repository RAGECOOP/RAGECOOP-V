#undef DEBUG
using System.Collections.Generic;
using System;
using System.Linq;
using RageCoop.Core;

namespace RageCoop.Client.Scripting
{
    /// <summary>
    /// Provides vital functionality to interact with RAGECOOP
    /// </summary>
    public static class API
    {
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
            public delegate void EmptyEvent();
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

            /// <summary>
            /// This will be invoked when a CustomEvent is received from the server.
            /// </summary>
            public static event CustomEvent OnCustomEventReceived;

            #region INVOKE
            internal static void InvokeVehicleSpawned(SyncedVehicle v) { OnVehicleSpawned?.Invoke(null, v); }
            internal static void InvokeVehicleDeleted(SyncedVehicle v) { OnVehicleDeleted?.Invoke(null, v); }
            internal static void InvokePedSpawned(SyncedPed p) { OnPedSpawned?.Invoke(null, p); }
            internal static void InvokePedDeleted(SyncedPed p) { OnPedDeleted?.Invoke(null, p); }
            internal static void InvokePlayerDied() { OnPlayerDied?.Invoke(); }
            internal static void InvokeTick() { OnTick?.Invoke(); }

            internal static void InvokeCustomEventReceived(int hash, List<object> args)
            {
                OnCustomEventReceived?.Invoke(hash, args);
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
                OnCustomEventReceived=null;
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
        /// Get a <see cref="Core.Logging.Logger"/> that RAGECOOP is currently using.
        /// </summary>
        /// <returns></returns>
        public static Core.Logging.Logger GetLogger()
        {
            return Main.Logger;
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
        /// Send an event and data to the specified clients.
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
        #endregion
    }
}
