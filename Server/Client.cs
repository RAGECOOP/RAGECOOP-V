using System;
using System.Collections.Generic;
using RageCoop.Core;
using Lidgren.Network;
using GTA.Math;

namespace RageCoop.Server
{
    public class Client
    {
        public long NetID = 0;
        internal NetConnection Connection { get; set; }
        public PlayerData Player;
        private readonly Dictionary<string, object> _customData = new();
        private long _callbacksCount = 0;
        public readonly Dictionary<long, Action<object>> Callbacks = new();
        public bool FilesReceived { get;internal set; } = false;
        public bool FilesSent = false;

        #region CUSTOMDATA FUNCTIONS
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
        #endregion

        #region FUNCTIONS
        public void Kick(string reason="You has been kicked!")
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
                Program.Logger.Error($">> {e.Message} <<>> {e.Source ?? string.Empty} <<>> {e.StackTrace ?? string.Empty} <<");
            }
        }

        public void SendNativeCall(ulong hash, params object[] args)
        {
            try
            {
                NetConnection userConnection = Server.MainNetServer.Connections.Find(x => x.RemoteUniqueIdentifier == NetID);
                if (userConnection == null)
                {
                    Program.Logger.Error($"[Client->SendNativeCall(ulong hash, params object[] args)]: Connection \"{NetID}\" not found!");
                    return;
                }

                if (args != null && args.Length == 0)
                {
                    Program.Logger.Error($"[Client->SendNativeCall(ulong hash, Dictionary<string, object> args)]: Missing arguments!");
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
                Program.Logger.Error($">> {e.Message} <<>> {e.Source ?? string.Empty} <<>> {e.StackTrace ?? string.Empty} <<");
            }
        }

        public void SendNativeResponse(Action<object> callback, ulong hash, Type returnType, params object[] args)
        {
            try
            {
                NetConnection userConnection = Server.MainNetServer.Connections.Find(x => x.RemoteUniqueIdentifier == NetID);
                if (userConnection == null)
                {
                    Program.Logger.Error($"[Client->SendNativeResponse(Action<object> callback, ulong hash, Type type, params object[] args)]: Connection \"{NetID}\" not found!");
                    return;
                }

                if (args != null && args.Length == 0)
                {
                    Program.Logger.Error($"[Client->SendNativeCall(ulong hash, Dictionary<string, object> args)]: Missing arguments!");
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
                    Program.Logger.Error($"[Client->SendNativeCall(ulong hash, Dictionary<string, object> args)]: Missing return type!");
                    return;
                }

                NetOutgoingMessage outgoingMessage = Server.MainNetServer.CreateMessage();
                new Packets.NativeResponse()
                {
                    Hash = hash,
                    Args = new List<object>(args) ?? new List<object>(),
                    ResultType = returnTypeValue,
                    ID = id
                }.Pack(outgoingMessage);
                Server.MainNetServer.SendMessage(outgoingMessage, userConnection, NetDeliveryMethod.ReliableOrdered, (byte)ConnectionChannel.Native);
            }
            catch (Exception e)
            {
                Program.Logger.Error($">> {e.Message} <<>> {e.Source ?? string.Empty} <<>> {e.StackTrace ?? string.Empty} <<");
            }
        }

        public void SendCleanUpWorld()
        {
            NetConnection userConnection = Server.MainNetServer.Connections.Find(x => x.RemoteUniqueIdentifier == NetID);
            if (userConnection == null)
            {
                Program.Logger.Error($"[Client->SendCleanUpWorld()]: Connection \"{NetID}\" not found!");
                return;
            }

            NetOutgoingMessage outgoingMessage = Server.MainNetServer.CreateMessage();
            outgoingMessage.Write((byte)PacketTypes.CleanUpWorld);
            Server.MainNetServer.SendMessage(outgoingMessage, userConnection, NetDeliveryMethod.ReliableOrdered, (byte)ConnectionChannel.Default);
        }

        public void SendTriggerEvent(string eventName, params object[] args)
        {
            if (!FilesReceived)
            {
                Program.Logger.Warning($"Player \"{Player.Username}\" doesn't have all the files yet!");
                return;
            }

            try
            {
                NetConnection userConnection = Server.MainNetServer.Connections.Find(x => x.RemoteUniqueIdentifier == NetID);
                if (userConnection == null)
                {
                    return;
                }

                NetOutgoingMessage outgoingMessage = Server.MainNetServer.CreateMessage();
                new Packets.ServerClientEvent()
                {
                    EventName = eventName,
                    Args = new List<object>(args)
                }.Pack(outgoingMessage);
                Server.MainNetServer.SendMessage(outgoingMessage, userConnection, NetDeliveryMethod.ReliableOrdered, (byte)ConnectionChannel.Event);
                Server.MainNetServer.FlushSendQueue();
            }
            catch (Exception e)
            {
                Program.Logger.Error($">> {e.Message} <<>> {e.Source ?? string.Empty} <<>> {e.StackTrace ?? string.Empty} <<");
            }
        }
        #endregion
    }
}
