using System;
using System.Collections.Generic;

using Lidgren.Network;

namespace CoopServer
{
    public class Client
    {
        public long NetHandle = 0;
        private float CurrentLatency = 0f;
        public float Latency
        {
            get => CurrentLatency;
            internal set
            {
                CurrentLatency = value;

                if ((value * 1000f) > Server.MainSettings.MaxLatency)
                {
                    Server.MainNetServer.Connections.Find(x => x.RemoteUniqueIdentifier == NetHandle)?.Disconnect($"Too high latency [{value * 1000f}/{(float)Server.MainSettings.MaxLatency}]");
                }
            }
        }
        public PlayerData Player;
        private readonly Dictionary<string, object> CustomData = new();
        private long CallbacksCount = 0;
        internal readonly Dictionary<long, Action<object>> Callbacks = new();
        internal bool FilesReceived = false;
        public bool FilesSent = false;

        #region CUSTOMDATA FUNCTIONS
        public void SetData<T>(string name, T data)
        {
            if (HasData(name))
            {
                CustomData[name] = data;
            }
            else
            {
                CustomData.Add(name, data);
            }
        }

        public bool HasData(string name)
        {
            return CustomData.ContainsKey(name);
        }

        public T GetData<T>(string name)
        {
            return HasData(name) ? (T)CustomData[name] : default;
        }

        public void RemoveData(string name)
        {
            if (HasData(name))
            {
                CustomData.Remove(name);
            }
        }
        #endregion

        #region FUNCTIONS
        public void Kick(string[] reason)
        {
            Server.MainNetServer.Connections.Find(x => x.RemoteUniqueIdentifier == NetHandle)?.Disconnect(string.Join(" ", reason));
        }
        public void Kick(string reason)
        {
            Server.MainNetServer.Connections.Find(x => x.RemoteUniqueIdentifier == NetHandle)?.Disconnect(reason);
        }

        public void SendChatMessage(string message, string from = "Server")
        {
            try
            {
                NetConnection userConnection = Server.MainNetServer.Connections.Find(x => x.RemoteUniqueIdentifier == NetHandle);
                if (userConnection == null)
                {
                    return;
                }

                Server.SendChatMessage(from, message, userConnection);
            }
            catch (Exception e)
            {
                Logging.Error($">> {e.Message} <<>> {e.Source ?? string.Empty} <<>> {e.StackTrace ?? string.Empty} <<");
            }
        }

        public void SendNativeCall(ulong hash, params object[] args)
        {
            try
            {
                NetConnection userConnection = Server.MainNetServer.Connections.Find(x => x.RemoteUniqueIdentifier == NetHandle);
                if (userConnection == null)
                {
                    Logging.Error($"[Client->SendNativeCall(ulong hash, params object[] args)]: Connection \"{NetHandle}\" not found!");
                    return;
                }

                if (args != null && args.Length == 0)
                {
                    Logging.Error($"[Client->SendNativeCall(ulong hash, Dictionary<string, object> args)]: Missing arguments!");
                    return;
                }

                Packets.NativeCall packet = new()
                {
                    Hash = hash,
                    Args = new List<object>(args) ?? new List<object>(),
                };

                NetOutgoingMessage outgoingMessage = Server.MainNetServer.CreateMessage();
                packet.PacketToNetOutGoingMessage(outgoingMessage);
                Server.MainNetServer.SendMessage(outgoingMessage, userConnection, NetDeliveryMethod.ReliableOrdered, (byte)ConnectionChannel.Native);
            }
            catch (Exception e)
            {
                Logging.Error($">> {e.Message} <<>> {e.Source ?? string.Empty} <<>> {e.StackTrace ?? string.Empty} <<");
            }
        }

        public void SendNativeResponse(Action<object> callback, ulong hash, Type returnType, params object[] args)
        {
            try
            {
                NetConnection userConnection = Server.MainNetServer.Connections.Find(x => x.RemoteUniqueIdentifier == NetHandle);
                if (userConnection == null)
                {
                    Logging.Error($"[Client->SendNativeResponse(Action<object> callback, ulong hash, Type type, params object[] args)]: Connection \"{NetHandle}\" not found!");
                    return;
                }

                if (args != null && args.Length == 0)
                {
                    Logging.Error($"[Client->SendNativeCall(ulong hash, Dictionary<string, object> args)]: Missing arguments!");
                    return;
                }

                long id = ++CallbacksCount;
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
                else if (returnType == typeof(LVector3))
                {
                    returnTypeValue = 0x04;
                }
                else
                {
                    Logging.Error($"[Client->SendNativeCall(ulong hash, Dictionary<string, object> args)]: Missing return type!");
                    return;
                }

                NetOutgoingMessage outgoingMessage = Server.MainNetServer.CreateMessage();
                new Packets.NativeResponse()
                {
                    Hash = hash,
                    Args = new List<object>(args) ?? new List<object>(),
                    ResultType = returnTypeValue,
                    ID = id
                }.PacketToNetOutGoingMessage(outgoingMessage);
                Server.MainNetServer.SendMessage(outgoingMessage, userConnection, NetDeliveryMethod.ReliableOrdered, (byte)ConnectionChannel.Native);
            }
            catch (Exception e)
            {
                Logging.Error($">> {e.Message} <<>> {e.Source ?? string.Empty} <<>> {e.StackTrace ?? string.Empty} <<");
            }
        }

        public void SendCleanUpWorld()
        {
            NetConnection userConnection = Server.MainNetServer.Connections.Find(x => x.RemoteUniqueIdentifier == NetHandle);
            if (userConnection == null)
            {
                Logging.Error($"[Client->SendCleanUpWorld()]: Connection \"{NetHandle}\" not found!");
                return;
            }

            NetOutgoingMessage outgoingMessage = Server.MainNetServer.CreateMessage();
            outgoingMessage.Write((byte)PacketTypes.CleanUpWorld);
            Server.MainNetServer.SendMessage(outgoingMessage, userConnection, NetDeliveryMethod.ReliableOrdered, (byte)ConnectionChannel.Default);
        }

        public void SendModPacket(string modName, byte customID, byte[] bytes)
        {
            try
            {
                NetConnection userConnection = Server.MainNetServer.Connections.Find(x => x.RemoteUniqueIdentifier == NetHandle);
                if (userConnection == null)
                {
                    return;
                }

                NetOutgoingMessage outgoingMessage = Server.MainNetServer.CreateMessage();
                new Packets.Mod()
                {
                    NetHandle = 0,
                    Target = 0,
                    Name = modName,
                    CustomPacketID = customID,
                    Bytes = bytes
                }.PacketToNetOutGoingMessage(outgoingMessage);
                Server.MainNetServer.SendMessage(outgoingMessage, userConnection, NetDeliveryMethod.ReliableOrdered, (byte)ConnectionChannel.Mod);
                Server.MainNetServer.FlushSendQueue();
            }
            catch (Exception e)
            {
                Logging.Error($">> {e.Message} <<>> {e.Source ?? string.Empty} <<>> {e.StackTrace ?? string.Empty} <<");
            }
        }
        #endregion
    }
}
