#undef DEBUG
using Lidgren.Network;
using Newtonsoft.Json;
using RageCoop.Client.Menus;
using RageCoop.Core;
using RageCoop.Core.Scripting;
using SHVDN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

[assembly: InternalsVisibleTo("CodeGen")] // For generating api bridge

namespace RageCoop.Client.Scripting
{

    /// <summary>
    ///     Provides vital functionality to interact with RAGECOOP
    /// </summary>
    internal static unsafe partial class API
    {
        #region INTERNAL

        internal static Dictionary<int, List<CustomEventHandler>> CustomEventHandlers =
            new();

        #endregion


        /// <summary>
        ///     Client configuration, this will conflict with server-side config.
        /// </summary>
        public static class Config
        {
            /// <summary>
            ///     Get or set local player's username, set won't be effective if already connected to a server.
            /// </summary>
            public static string Username
            {
                get => Settings.Username;
                set
                {
                    if (Networking.IsOnServer || string.IsNullOrEmpty(value)) return;
                    Settings.Username = value;
                }
            }

            /// <summary>
            ///     Enable automatic respawn for this player.
            /// </summary>
            public static bool EnableAutoRespawn { get; set; } = true;

            /// <summary>
            ///     Get or set player's blip color
            /// </summary>
            public static BlipColor BlipColor { get; set; } = BlipColor.White;

            /// <summary>
            ///     Get or set player's blip sprite
            /// </summary>
            public static BlipSprite BlipSprite { get; set; } = BlipSprite.Standard;

            /// <summary>
            ///     Get or set scale of player's blip
            /// </summary>
            public static float BlipScale { get; set; } = 1;

            public static bool ShowPlayerNameTag
            {
                get => Settings.ShowPlayerNameTag;
                set
                {
                    if (value == ShowPlayerNameTag) return;
                    Settings.ShowPlayerNameTag = value;
                    Util.SaveSettings();
                }
            }
        }

        /// <summary>
        ///     Base events for RageCoop
        /// </summary>
        public static class Events
        {
            /// <summary>
            ///     The local player is dead
            /// </summary>
            public static event EmptyEvent OnPlayerDied;

            /// <summary>
            ///     A local vehicle is spawned
            /// </summary>
            public static event EventHandler<SyncedVehicle> OnVehicleSpawned;

            /// <summary>
            ///     A local vehicle is deleted
            /// </summary>
            public static event EventHandler<SyncedVehicle> OnVehicleDeleted;

            /// <summary>
            ///     A local ped is spawned
            /// </summary>
            public static event EventHandler<SyncedPed> OnPedSpawned;

            /// <summary>
            ///     A local ped is deleted
            /// </summary>
            public static event EventHandler<SyncedPed> OnPedDeleted;

            #region DELEGATES

            /// <summary>
            /// </summary>
            public delegate void EmptyEvent();

            /// <summary>
            /// </summary>
            /// <param name="hash"></param>
            /// <param name="args"></param>
            public delegate void CustomEvent(int hash, List<object> args);

            #endregion

            #region INVOKE

            internal static void InvokeVehicleSpawned(SyncedVehicle v)
            {
                OnVehicleSpawned?.Invoke(null, v);
            }

            internal static void InvokeVehicleDeleted(SyncedVehicle v)
            {
                OnVehicleDeleted?.Invoke(null, v);
            }

            internal static void InvokePedSpawned(SyncedPed p)
            {
                OnPedSpawned?.Invoke(null, p);
            }

            internal static void InvokePedDeleted(SyncedPed p)
            {
                OnPedDeleted?.Invoke(null, p);
            }

            internal static void InvokePlayerDied()
            {
                OnPlayerDied?.Invoke();
            }

            internal static void InvokeCustomEventReceived(Packets.CustomEvent p)
            {

                // Log.Debug($"CustomEvent:\n"+args.Args.DumpWithType());

                if (CustomEventHandlers.TryGetValue(p.Hash, out var handlers))
                {
                    fixed (byte* pData = p.Payload)
                    {
                        foreach (var handler in handlers)
                        {
                            try
                            {
                                handler.Invoke(p.Hash, pData, p.Payload.Length);
                            }
                            catch (Exception ex)
                            {
                                Log.Error("InvokeCustomEvent", ex);
                            }
                        }
                    }
                }
            }

            #endregion
        }

        #region PROPERTIES

        /// <summary>
        ///     Get the local player's ID
        /// </summary>
        /// <returns>PlayerID</returns>
        public static int LocalPlayerID => Main.LocalPlayerID;

        /// <summary>
        ///     Check if player is connected to a server
        /// </summary>
        public static bool IsOnServer => Networking.IsOnServer;

        /// <summary>
        ///     Get an <see cref="System.Net.IPEndPoint" /> that the player is currently connected to, or null if not connected to
        ///     the server
        /// </summary>
        public static IPEndPoint ServerEndPoint =>
            Networking.IsOnServer ? Networking.ServerConnection?.RemoteEndPoint : null;

        /// <summary>
        ///     Check if a RAGECOOP menu is visible
        /// </summary>
        public static bool IsMenuVisible => CoopMenu.MenuPool.AreAnyVisible;

        /// <summary>
        ///     Check if the RAGECOOP chat is visible
        /// </summary>
        public static bool IsChatFocused => MainChat.Focused;

        /// <summary>
        ///     Check if the RAGECOOP list of players is visible
        /// </summary>
        public static bool IsPlayerListVisible => Util.GetTickCount64() - PlayerList.Pressed < 5000;

        /// <summary>
        ///     Get the version of RAGECOOP
        /// </summary>
        public static Version CurrentVersion => Main.ModVersion;

        /// <summary>
        ///     Get all players indexed by their ID
        /// </summary>
        public static Dictionary<int, Player> Players => new(PlayerList.Players);

        #endregion

        #region FUNCTIONS

        /// <summary>
        ///     Queue an action to be executed on next tick.
        /// </summary>
        /// <param name="a"></param>
        public static void QueueAction(Action a)
        {
            WorldThread.QueueAction(a);
        }

        public static void QueueActionAndWait(Action a, int timeout = 15000)
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
                catch (Exception ex)
                {
                    e = ex;
                }
            });
            if (e != null)
                throw e;
            if (!done.WaitOne(timeout)) throw new TimeoutException();
        }

        /// <summary>
        ///     Queue an action to be executed on next tick, allowing you to call scripting API from another thread.
        /// </summary>
        /// <param name="a">
        ///     An <see cref="Func{T, TResult}" /> to be executed with a return value indicating whether it can be
        ///     removed after execution.
        /// </param>
        public static void QueueAction(Func<bool> a)
        {
            WorldThread.QueueAction(a);
        }

        /// <summary>
        ///     Send an event and data to the server.
        /// </summary>
        /// <param name="eventHash">An unique identifier of the event</param>
        /// <param name="args">
        ///     The objects conataing your data, see <see cref="CustomEventReceivedArgs" /> for a list of supported
        ///     types
        /// </param>
        public static void SendCustomEvent(CustomEventHash eventHash, params object[] args)
        => SendCustomEvent(CustomEventFlags.None, eventHash, args);

        /// <summary>
        ///     Send an event and data to the server
        /// </summary>
        /// <param name="flags"></param>
        /// <param name="eventHash">An unique identifier of the event</param>
        /// <param name="args">
        ///     The objects conataing your data, see <see cref="CustomEventReceivedArgs" /> for a list of supported
        ///     types
        /// </param>
        public static void SendCustomEvent(CustomEventFlags flags, CustomEventHash eventHash, params object[] args)
        {
            var writer = GetWriter();
            CustomEvents.WriteObjects(writer, args);
            Networking.Peer.SendTo(new Packets.CustomEvent(flags)
            {

                Payload = writer.ToByteArray(writer.Position),
                Hash = eventHash
            }, Networking.ServerConnection, ConnectionChannel.Event, NetDeliveryMethod.ReliableOrdered);
        }

        /// <summary>
        ///     Register an handler to the specifed event hash, one event can have multiple handlers. This will be invoked from
        ///     backgound thread, use <see cref="QueueAction(Action)" /> in the handler to dispatch code to script thread.
        /// </summary>
        /// <param name="hash">
        ///     An unique identifier of the event
        /// </param>
        /// <param name="handler">An handler to be invoked when the event is received from the server. </param>
        public static void RegisterCustomEventHandler(CustomEventHash hash, Action<CustomEventReceivedArgs> handler)
            => RegisterCustomEventHandler(hash, (CustomEventHandler)handler);


        /// <summary>
        /// </summary>
        /// <returns></returns>
        public static void RequestSharedFile(string name, Action<string> callback)
        {
            EventHandler<string> handler = (s, e) =>
            {
                if (e.EndsWith(name)) callback(e);
            };
            DownloadManager.DownloadCompleted += handler;
            Networking.GetResponse<Packets.FileTransferResponse>(new Packets.FileTransferRequest
            {
                Name = name
            },
                p =>
                {
                    if (p.Response != FileResponse.Loaded)
                    {
                        DownloadManager.DownloadCompleted -= handler;
                        throw new ArgumentException("Requested file was not found on the server: " + name);
                    }
                });
        }



        /// <summary>
        ///     Connect to a server
        /// </summary>
        /// <param name="address">Address of the server, e.g. 127.0.0.1:4499</param>
        /// <exception cref="InvalidOperationException">When a connection is active or being established</exception>
        [Remoting]
        public static void Connect(string address)
        {
            if (Networking.IsOnServer || Networking.IsConnecting)
                throw new InvalidOperationException("Cannot connect to server when another connection is active");
            Networking.ToggleConnection(address);
        }

        /// <summary>
        ///     Disconnect from current server or cancel the connection attempt.
        /// </summary>
        [Remoting]
        public static void Disconnect()
        {
            if (Networking.IsOnServer || Networking.IsConnecting) Networking.ToggleConnection(null);
        }

        /// <summary>
        ///     List all servers from master server address
        /// </summary>
        /// <returns></returns>
        [Remoting]
        public static List<ServerInfo> ListServers()
        {
            return JsonDeserialize<List<ServerInfo>>(
                HttpHelper.DownloadString(Settings.MasterServer));
        }

        /// <summary>
        ///     Send a local chat message to this player
        /// </summary>
        /// <param name="from">Name of the sender</param>
        /// <param name="message">The player's message</param>
        [Remoting]
        public static void LocalChatMessage(string from, string message)
        {
            MainChat.AddMessage(from, message);
        }

        /// <summary>
        ///     Send a chat message or command to server/other players
        /// </summary>
        /// <param name="message"></param>
        [Remoting]
        public static void SendChatMessage(string message)
        {
            if (!IsOnServer)
                throw new InvalidOperationException("Not on server");
            Networking.SendChatMessage(message);
        }

        /// <summary>
        /// Get the <see cref="ClientResource"/> with this name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        [Remoting]
        public static ClientResource GetResource(string name)
        {
            if (MainRes.LoadedResources.TryGetValue(name, out var resource))
                return resource;

            return null;
        }

        /// <summary>
        /// Get <see cref="ClientResource"/> that contains the specified file
        /// </summary>
        /// <returns></returns>
        [Remoting]
        public static ClientResource GetResouceFromFilePath(string filePath)
        {
            foreach (var res in MainRes.LoadedResources)
            {
                if (res.Value.Files.Any(file => file.Value.FullPath.ToLower() == filePath.ToLower()))
                    return res.Value;
            }
            return null;
        }


        [Remoting(GenBridge = false)]
        public static object GetProperty(string name)
            => typeof(API).GetProperty(name, BindingFlags.Static | BindingFlags.Public)?.GetValue(null);

        [Remoting(GenBridge = false)]
        public static void SetProperty(string name, string jsonVal)
        {
            var prop = typeof(API).GetProperty(name, BindingFlags.Static | BindingFlags.Public); ;
            if (prop == null)
                throw new KeyNotFoundException($"Property {name} was not found");
            prop.SetValue(null, JsonDeserialize(jsonVal, prop.PropertyType));
        }

        [Remoting]
        public static object GetConfig(string name)
            => typeof(Config).GetProperty(name, BindingFlags.Static | BindingFlags.Public)?.GetValue(null);

        [Remoting]
        public static void SetConfig(string name, string jsonVal)
        {
            var prop = typeof(Config).GetProperty(name, BindingFlags.Static | BindingFlags.Public);
            if (prop == null)
                throw new KeyNotFoundException($"Property {name} was not found");
            prop.SetValue(null, JsonDeserialize(jsonVal, prop.PropertyType));
        }



        /// <summary>
        ///     Register an handler to the specifed event hash, one event can have multiple handlers. This will be invoked from
        ///     backgound thread, use <see cref="QueueAction(Action)" /> in the handler to dispatch code to script thread.
        /// </summary>
        /// <param name="hash">
        ///     An unique identifier of the event
        /// </param>
        /// <param name="handler">An handler to be invoked when the event is received from the server. </param>
        [Remoting]
        public static void RegisterCustomEventHandler(CustomEventHash hash, CustomEventHandler handler)
        {
            if (handler.Module == default)
                throw new ArgumentException("Module not specified");

            if (handler.FunctionPtr == default)
                throw new ArgumentException("Function pointer not specified");

            lock (CustomEventHandlers)
            {
                if (!CustomEventHandlers.TryGetValue(hash, out var handlers))
                    CustomEventHandlers.Add(hash, handlers = new());
                handlers.Add(handler);
            }
        }

        #endregion


    }
}