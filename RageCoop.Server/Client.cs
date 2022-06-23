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
        /*
        /// <summary>
        /// Send a native call to client and ignore its return value.
        /// </summary>
        /// <param name="hash">The function's hash</param>
        /// <param name="args">Arguments</param>
        public void SendNativeCall(ulong hash, params object[] args)
        {
            try
            {
                NetConnection userConnection = Server.MainNetServer.Connections.Find(x => x.RemoteUniqueIdentifier == NetID);
                if (userConnection == null)
                {
                    Server.Logger?.Error($"[Client->SendNativeCall(ulong hash, params object[] args)]: Connection \"{NetID}\" not found!");
                    return;
                }

                if (args != null && args.Length == 0)
                {
                    Server.Logger?.Error($"[Client->SendNativeCall(ulong hash, Dictionary<string, object> args)]: Missing arguments!");
                    return;
                }

                Packets.NativeCall packet = new()
                {
                    Hash = hash,
                    Args = new List<object>(args) ?? new List<object>(),
                };

                NetOutgoingMessage outgoingMessage = Server.MainNetServer.CreateMessage();
                packet.Pack(outgoingMessage);
                Server.MainNetServer.SendMessage(outgoingMessage, userConnection, NetDeliveryMethod.ReliableOrdered, (byte)ConnectionChannel.Native);
            }
            catch (Exception e)
            {
                Server.Logger?.Error($">> {e.Message} <<>> {e.Source ?? string.Empty} <<>> {e.StackTrace ?? string.Empty} <<");
            }
        }
        /// <summary>
        /// Send a native call to client and do a callback when the response is received.
        /// </summary>
        /// <param name="callback">The callback to be invoked when the response is received.</param>
        /// <param name="hash">The function's hash</param>
        /// <param name="returnType">The return type of the response</param>
        /// <param name="args">Arguments</param>
        public void SendNativeCallWithResponse(Action<object> callback, GTA.Native.Hash hash, Type returnType, params object[] args)
        {
            try
            {
                NetConnection userConnection = Server.MainNetServer.Connections.Find(x => x.RemoteUniqueIdentifier == NetID);
                if (userConnection == null)
                {
                    Server.Logger?.Error($"[Client->SendNativeResponse(Action<object> callback, ulong hash, Type type, params object[] args)]: Connection \"{NetID}\" not found!");
                    return;
                }

                if (args != null && args.Length == 0)
                {
                    Server.Logger?.Error($"[Client->SendNativeCall(ulong hash, Dictionary<string, object> args)]: Missing arguments!");
                    return;
                }

                long id = ++_callbacksCount;
                Callbacks.Add(id, callback);

                byte returnTypeValue = 0x00;
                if (returnType == typeof(int))
                {
                    // NOTHING BECAUSE VALUE IS 0x00
                }
                else if (returnType == typeof(bool))
                {
                    returnTypeValue = 0x01;
                }
                else if (returnType == typeof(float))
                {
                    returnTypeValue = 0x02;
                }
                else if (returnType == typeof(string))
                {
                    returnTypeValue = 0x03;
                }
                else if (returnType == typeof(Vector3))
                {
                    returnTypeValue = 0x04;
                }
                else
                {
                    Server.Logger?.Error($"[Client->SendNativeCall(ulong hash, Dictionary<string, object> args)]: Missing return type!");
                    return;
                }

                NetOutgoingMessage outgoingMessage = Server.MainNetServer.CreateMessage();
                new Packets.NativeResponse()
                {
                    Hash = (ulong)hash,
                    Args = new List<object>(args) ?? new List<object>(),
                    ResultType = returnTypeValue,
                    ID = id
                }.Pack(outgoingMessage);
                Server.MainNetServer.SendMessage(outgoingMessage, userConnection, NetDeliveryMethod.ReliableOrdered, (byte)ConnectionChannel.Native);
            }
            catch (Exception e)
            {
                Server.Logger?.Error($">> {e.Message} <<>> {e.Source ?? string.Empty} <<>> {e.StackTrace ?? string.Empty} <<");
            }
        }
        */

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
        public void SendCleanUpWorld()
        {
            NetConnection userConnection = Server.MainNetServer.Connections.Find(x => x.RemoteUniqueIdentifier == NetID);
            if (userConnection == null)
            {
                Server.Logger?.Error($"[Client->SendCleanUpWorld()]: Connection \"{NetID}\" not found!");
                return;
            }
            NetOutgoingMessage outgoingMessage = Server.MainNetServer.CreateMessage();
            outgoingMessage.Write((byte)PacketTypes.CleanUpWorld);
            Server.MainNetServer.SendMessage(outgoingMessage, userConnection, NetDeliveryMethod.ReliableOrdered, (byte)ConnectionChannel.Default);
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
