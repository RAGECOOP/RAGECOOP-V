using System;
using System.Collections.Generic;
using RageCoop.Core;
using Lidgren.Network;
using GTA.Math;
using RageCoop.Core.Scripting;
using System.Security.Cryptography;

namespace RageCoop.Server
{
    public class ServerPed
    {
        public int ID { get;internal set; }
        /// <summary>
        /// The ID of the ped's last vehicle.
        /// </summary>
        public int VehicleID { get; internal set; }
        public Vector3 Position { get; internal set; }

        public int Health { get; internal set; }
    }
    public class PlayerConfig
    {
        #region CLIENT
        public bool EnableAutoRespawn { get; set; }=true;
        #endregion
        public bool ShowBlip { get; set; } = true;
        public bool ShowNameTag { get; set; } = true;
        public GTA.BlipColor BlipColor { get; set; } = GTA.BlipColor.White;
        public PlayerConfigFlags GetFlags()
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
    public class Client
    {
        private readonly Server Server;
        internal Client(Server server)
        {
            Server=server;
        }
        internal long NetID = 0;
        public NetConnection Connection { get;internal set; }
        public ServerPed Player { get; internal set; }
        public float Latency { get; internal set; }
        public int ID { get; internal set; }
        private PlayerConfig _config { get; set; }=new PlayerConfig();
        public PlayerConfig Config { get { return _config; }set { _config=value;Server.SendPlayerInfos(); } }

        internal readonly Dictionary<int, Action<object>> Callbacks = new();
        internal byte[] PublicKey { get; set; }
        public bool IsReady { get; internal set; }=false;
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
        public void Kick(string reason="You have been kicked!")
        {
            Connection?.Disconnect(reason);
        }
        public void Kick(params string[] reasons)
        {
            Kick(string.Join(" ", reasons));
        }

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
        /// Send a CleanUpWorld message to this client.
        /// </summary>
        /// <param name="clients"></param>
        public void SendCleanUpWorld(List<Client> clients = null)
        {
            SendCustomEvent(CustomEvents.CleanUpWorld, null);
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

        public void SendCustomEvent(int id,List<object> args)
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
                    Hash=id,
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
