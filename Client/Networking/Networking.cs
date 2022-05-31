using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using Lidgren.Network;
using RageCoop.Core;
using System.Threading.Tasks;
using System.Threading;
using GTA.Math;
using GTA.Native;

namespace RageCoop.Client
{
    internal static partial class Networking
    {
        public static NetClient Client;
        public static float Latency = 0;
        public static bool ShowNetworkInfo = false;
        public static int BytesReceived = 0;
        public static int BytesSend = 0;
        private static Thread ReceiveThread;

        public static void DisConnectFromServer(string address)
        {
            if (IsOnServer)
            {
                Client.Disconnect("Bye!");
            }
            else
            {
                // 623c92c287cc392406e7aaaac1c0f3b0 = RAGECOOP
                NetPeerConfiguration config = new NetPeerConfiguration("623c92c287cc392406e7aaaac1c0f3b0")
                {
                    AutoFlushSendQueue = false
                };

                config.EnableMessageType(NetIncomingMessageType.ConnectionLatencyUpdated);

                Client = new NetClient(config);

                Client.Start();

                string[] ip = new string[2];

                int idx = address.LastIndexOf(':');
                if (idx != -1)
                {
                    ip[0] = address.Substring(0, idx);
                    ip[1] = address.Substring(idx + 1);
                }

                if (ip.Length != 2)
                {
                    throw new Exception("Malformed URL");
                }
                
                // Send HandshakePacket
                EntityPool.AddPlayer();
                NetOutgoingMessage outgoingMessage = Client.CreateMessage();
                new Packets.Handshake()
                {
                    PedID =  Main.LocalPlayerID,
                    Username = Main.Settings.Username,
                    ModVersion = Main.CurrentVersion,
                }.Pack(outgoingMessage);

                Client.Connect(ip[0], short.Parse(ip[1]), outgoingMessage);
            }
        }
        public static bool IsOnServer
        {
            get { return Client?.ConnectionStatus == NetConnectionStatus.Connected; }
        }
        public static void Start()
        {
            ReceiveThread=new Thread(() =>
            {
                while (true)
                {
                    try
                    {
                        if (Client!=null)
                        {
                            Client.FlushSendQueue();
                            ReceiveMessages();
                        }
                    }
                    catch (Exception ex)
                    {
                        Main.Logger.Error(ex);
                    }
                    Thread.Sleep(5);
                }
            });
            ReceiveThread.Start();
        }
        
        #region -- GET --
        #region -- PLAYER --
        private static void PlayerConnect(Packets.PlayerConnect packet)
        {
            var p = new PlayerData
            {
                PedID = packet.PedID,
                Username= packet.Username,
            };
            GTA.UI.Notification.Show($"{p.Username} connected.");
            PlayerList.SetPlayer(packet.PedID, packet.Username);

            Main.Logger.Debug($"player connected:{p.Username}");
            Main.DumpCharacters();
            COOPAPI.Connected(packet.PedID);
        }
        private static void PlayerDisconnect(Packets.PlayerDisconnect packet)
        {
            var name=PlayerList.GetPlayer(packet.PedID).Username;
            GTA.UI.Notification.Show($"{name} left.");
            COOPAPI.Disconnected(packet.PedID);
            PlayerList.RemovePlayer(packet.PedID);
            EntityPool.RemoveAllFromPlayer(packet.PedID);


        }
        private static object DecodeNativeCall(ulong hash, List<object> args, bool returnValue, byte? returnType = null)
        {
            List<InputArgument> arguments = new List<InputArgument>();

            if (args == null || args.Count == 0)
            {
                return null;
            }

            for (ushort i = 0; i < args.Count; i++)
            {
                object x = args.ElementAt(i);
                switch (x)
                {
                    case int _:
                        arguments.Add((int)x);
                        break;
                    case bool _:
                        arguments.Add((bool)x);
                        break;
                    case float _:
                        arguments.Add((float)x);
                        break;
                    case string _:
                        arguments.Add((string)x);
                        break;
                    case Vector3 _:
                        Vector3 vector = (Vector3)x;
                        arguments.Add((float)vector.X);
                        arguments.Add((float)vector.Y);
                        arguments.Add((float)vector.Z);
                        break;
                    default:
                        GTA.UI.Notification.Show("[DecodeNativeCall][" + hash + "]: Type of argument not found!");
                        return null;
                }
            }

            if (!returnValue)
            {
                Function.Call((Hash)hash, arguments.ToArray());
                return null;
            }

            switch (returnType.Value)
            {
                case 0x00: // int
                    return Function.Call<int>((Hash)hash, arguments.ToArray());
                case 0x01: // bool
                    return Function.Call<bool>((Hash)hash, arguments.ToArray());
                case 0x02: // float
                    return Function.Call<float>((Hash)hash, arguments.ToArray());
                case 0x03: // string
                    return Function.Call<string>((Hash)hash, arguments.ToArray());
                case 0x04: // vector3
                    return Function.Call<Vector3>((Hash)hash, arguments.ToArray());
                default:
                    GTA.UI.Notification.Show("[DecodeNativeCall][" + hash + "]: Type of return not found!");
                    return null;
            }
        }

        private static void DecodeNativeResponse(Packets.NativeResponse packet)
        {
            object result = DecodeNativeCall(packet.Hash, packet.Args, true, packet.ResultType);

            if (Main.CheckNativeHash.ContainsKey(packet.Hash))
            {
                foreach (KeyValuePair<ulong, byte> hash in Main.CheckNativeHash)
                {
                    if (hash.Key == packet.Hash)
                    {
                        lock (Main.ServerItems)
                        {
                            Main.ServerItems.Add((int)result, hash.Value);
                        }
                        break;
                    }
                }
            }

            NetOutgoingMessage outgoingMessage = Client.CreateMessage();
            new Packets.NativeResponse()
            {
                Hash = 0,
                Args = new List<object>() { result },
                ID =  packet.ID
            }.Pack(outgoingMessage);
            Client.SendMessage(outgoingMessage, NetDeliveryMethod.ReliableOrdered, (byte)ConnectionChannel.Native);
            Client.FlushSendQueue();
        }
        #endregion // -- PLAYER --

        #endregion
        public static void Tick()
        {
            


            // Sync
            EntityPool.DoSync();
        }

    }
}
