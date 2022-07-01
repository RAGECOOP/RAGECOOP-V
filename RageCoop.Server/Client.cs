using System;
using System.Collections.Generic;
using RageCoop.Core;
using Lidgren.Network;
using GTA.Math;
using RageCoop.Core.Scripting;
using System.Security.Cryptography;

namespace RageCoop.Server
{
    /// <summary>
    /// Represents a ped from a client
    /// </summary>
    public class ServerPed
    {
        /// <summary>
        /// The <see cref="Client"/> that is responsible synchronizing for this ped.
        /// </summary>
        public Client Owner { get; internal set; }
        /// <summary>
        /// The ped's ID (not handle!).
        /// </summary>
        public int ID { get;internal set; }
        /// <summary>
        /// The ID of the ped's last vehicle.
        /// </summary>
        public int VehicleID { get; internal set; }
        /// <summary>
        /// Position of this ped
        /// </summary>
        public Vector3 Position { get; internal set; }
        /// <summary>
        /// Health
        /// </summary>
        public int Health { get; internal set; }
    }
    /// <summary>
    /// 
    /// </summary>
    public class PlayerConfig
    {
        #region CLIENT
        /// <summary>
        /// Whether to enable automatic respawn for this player. if set to false, player will just lay on the ground when it's dead
        /// </summary>
        public bool EnableAutoRespawn { get; set; }=true;
        #endregion
        /// <summary>
        /// Whether to show the player's blip on map.
        /// </summary>
        public bool ShowBlip { get; set; } = true;
        /// <summary>
        /// Whether the player's nametag is visible to other players.
        /// </summary>
        public bool ShowNameTag { get; set; } = true;
        /// <summary>
        /// The blip's color.
        /// </summary>
        public GTA.BlipColor BlipColor { get; set; } = GTA.BlipColor.White;
        internal PlayerConfigFlags GetFlags()
        {
            var flag=PlayerConfigFlags.None;
            if (ShowBlip)
            {
                flag|= PlayerConfigFlags.ShowBlip;
            }
            if (ShowNameTag)
            {
                flag |= PlayerConfigFlags.ShowNameTag;
            }
            return flag;
        }
    }
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
        internal long NetID = 0;
        internal NetConnection Connection { get;set; }
        /// <summary>
        /// The <see cref="ServerPed"/> instance representing the client's main character.
        /// </summary>
        public ServerPed Player { get; internal set; }
        /// <summary>
        /// The client's latncy in seconds.
        /// </summary>
        public float Latency { get; internal set; }
        private PlayerConfig _config { get; set; }=new PlayerConfig();
        /// <summary>
        /// The client's configuration
        /// </summary>
        public PlayerConfig Config { get { return _config; }set { _config=value;Server.SendPlayerInfos(); } }

        internal readonly Dictionary<int, Action<object>> Callbacks = new();
        internal byte[] PublicKey { get; set; }
        /// <summary>
        /// Indicates whether the client has succefully loaded all resources.
        /// </summary>
        public bool IsReady { get; internal set; }=false;
        /// <summary>
        /// 
        /// </summary>
        public string Username { get;internal set; } = "N/A";
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

            SendCustomEvent(CustomEvents.NativeCall, argsList);
        }
        /// <summary>
        /// Send a native call to client and ignore it's response.
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="args"></param>
        public void SendNativeCall(GTA.Native.Hash hash, params object[] args)
        {
            var argsList = new List<object>(args);
            argsList.InsertRange(0, new object[] { (byte)TypeCode.Empty,(ulong)hash});
            // Server.Logger?.Debug(argsList.DumpWithType());
            SendCustomEvent(CustomEvents.NativeCall, argsList);
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
        /// <param name="args"></param>
        public void SendCustomEvent(int hash,List<object> args)
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
        #endregion
    }
}
