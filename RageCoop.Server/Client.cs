using System;
using System.Collections.Generic;
using RageCoop.Core;
using Lidgren.Network;
using System.Linq;
using GTA;
using RageCoop.Core.Scripting;
using System.Security.Cryptography;
using RageCoop.Server.Scripting;

namespace RageCoop.Server
{
    /// <summary>
    /// Represent a player connected to this server.
    /// </summary>
    public class Client
    {
        private readonly Server Server;
        internal Client(Server server)
        {
            Server=server;
        }
        /// <summary>
        /// Th client's IP address and port.
        /// </summary>
        public System.Net.IPEndPoint EndPoint { get { return Connection?.RemoteEndPoint; } }
        internal long NetID = 0;
        internal NetConnection Connection { get;set; }
        /// <summary>
        /// The <see cref="ServerPed"/> instance representing the client's main character.
        /// </summary>
        public ServerPed Player { get; internal set; }
        /// <summary>
        /// The client's latency in seconds.
        /// </summary>
        public float Latency { get; internal set; }
        internal readonly Dictionary<int, Action<object>> Callbacks = new();
        internal byte[] PublicKey { get; set; }
        /// <summary>
        /// Indicates whether the client has succefully loaded all resources.
        /// </summary>
        public bool IsReady { get; internal set; }=false;
        /// <summary>
        /// The client's username.
        /// </summary>
        public string Username { get;internal set; } = "N/A";


        private bool _autoRespawn=true;

        /// <summary>
        /// Gets or sets whether to enable automatic respawn for this client's main ped.
        /// </summary>
        public bool EnableAutoRespawn { 
            get { return _autoRespawn; } 
            set {
                Server.BaseScript.SetAutoRespawn(this,value);
                _autoRespawn=value;
            }
        }

        private bool _displayNameTag=true;

        /// <summary>
        /// Gets or sets whether to enable automatic respawn for this client's main ped.
        /// </summary>
        public bool DisplayNameTag
        {
            get { return _displayNameTag; }
            set
            {
                Server.BaseScript.SetNameTag(this,value);
                _displayNameTag=value;
            }
        }
        #region CUSTOMDATA FUNCTIONS
        /*
        public void SetData<T>(string name, T data)
        {
            if (HasData(name))
            {
                _customData[name] = data;
            }
            else
            {
                _customData.Add(name, data);
            }
        }

        public bool HasData(string name)
        {
            return _customData.ContainsKey(name);
        }

        public T GetData<T>(string name)
        {
            return HasData(name) ? (T)_customData[name] : default;
        }

        public void RemoveData(string name)
        {
            if (HasData(name))
            {
                _customData.Remove(name);
            }
        }
        */

        #endregion
        #region FUNCTIONS
        /// <summary>
        /// Kick this client
        /// </summary>
        /// <param name="reason"></param>
        public void Kick(string reason="You have been kicked!")
        {
            Connection?.Disconnect(reason);
        }
        /// <summary>
        /// Kick this client
        /// </summary>
        /// <param name="reasons">Reasons to kick</param>
        public void Kick(params string[] reasons)
        {
            Kick(string.Join(" ", reasons));
        }

        /// <summary>
        /// Send a chat messsage to this client, not visible to others.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="from"></param>
        public void SendChatMessage(string message, string from = "Server")
        {
            try
            {
                NetConnection userConnection = Server.MainNetServer.Connections.Find(x => x.RemoteUniqueIdentifier == NetID);
                if (userConnection == null)
                {
                    return;
                }

                Server.SendChatMessage(from, message, userConnection);
            }
            catch (Exception e)
            {
                Server.Logger?.Error($">> {e.Message} <<>> {e.Source ?? string.Empty} <<>> {e.StackTrace ?? string.Empty} <<");
            }
        }

        /// <summary>
        /// Send a native call to client and do a callback when the response received.
        /// </summary>
        /// <typeparam name="T">Type of the response</typeparam>
        /// <param name="callBack"></param>
        /// <param name="hash"></param>
        /// <param name="args"></param>
        public void SendNativeCall<T>(Action<object> callBack, GTA.Native.Hash hash, params object[] args)
        {
            var argsList= new List<object>(args);
            argsList.InsertRange(0, new object[] { (byte)Type.GetTypeCode(typeof(T)), RequestNativeCallID<T>(callBack), (ulong)hash });

            SendCustomEventQueued(CustomEvents.NativeCall, argsList.ToArray());
        }
        /// <summary>
        /// Send a native call to client and ignore it's response.
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="args"></param>
        public void SendNativeCall(GTA.Native.Hash hash, params object[] args)
        {
            var argsList = new List<object>(args);
            argsList.InsertRange(0, new object[] { (byte)TypeCode.Empty,(ulong)hash });
            // Server.Logger?.Debug(argsList.DumpWithType());
            SendCustomEventQueued(CustomEvents.NativeCall, argsList.ToArray());
        }
        private int RequestNativeCallID<T>(Action<object> callback)
        {
            int ID = 0;
            lock (Callbacks)
            {
                while ((ID==0)
                    || Callbacks.ContainsKey(ID))
                {
                    byte[] rngBytes = new byte[4];

                    RandomNumberGenerator.Create().GetBytes(rngBytes);

                    // Convert the bytes into an integer
                    ID = BitConverter.ToInt32(rngBytes, 0);
                }
                Callbacks.Add(ID, callback);
            }
            return ID;
        }
        /// <summary>
        /// Trigger a CustomEvent for this client
        /// </summary>
        /// <param name="hash">An unique identifier of the event, you can use <see cref="CustomEvents.Hash(string)"/> to get it from a string</param>
        /// <param name="args">Arguments</param>
        public void SendCustomEvent(int hash,params object[] args)
        {
            if (!IsReady)
            {
                Server.Logger?.Warning($"Player \"{Username}\" is not ready!");
            }

            try
            {

                NetOutgoingMessage outgoingMessage = Server.MainNetServer.CreateMessage();
                new Packets.CustomEvent()
                {
                    Hash=hash,
                    Args=args
                }.Pack(outgoingMessage);
                Server.MainNetServer.SendMessage(outgoingMessage, Connection, NetDeliveryMethod.ReliableOrdered, (byte)ConnectionChannel.Event);

            }
            catch (Exception ex)
            {
                Server.Logger?.Error(ex);
            }
        }

        /// <summary>
        /// Send a CustomEvent that'll be queued at client side and invoked from script thread
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="args"></param>
        public void SendCustomEventQueued(int hash, params object[] args)
        {
            if (!IsReady)
            {
                Server.Logger?.Warning($"Player \"{Username}\" is not ready!");
            }

            try
            {

                NetOutgoingMessage outgoingMessage = Server.MainNetServer.CreateMessage();
                new Packets.CustomEvent(null,true)
                {
                    Hash=hash,
                    Args=args
                }.Pack(outgoingMessage);
                Server.MainNetServer.SendMessage(outgoingMessage, Connection, NetDeliveryMethod.ReliableOrdered, (byte)ConnectionChannel.Event);

            }
            catch (Exception ex)
            {
                Server.Logger?.Error(ex);
            }
        }

        #endregion
    }
}
