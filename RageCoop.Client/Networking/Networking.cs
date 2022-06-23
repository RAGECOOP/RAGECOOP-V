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

        static Networking()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    if (Client!=null)
                    {
                        ProcessMessage(Client.WaitMessage(200));
                        Client.FlushSendQueue();
                    }
                    else
                    {
                        Thread.Sleep(20);
                    }
                }
            });
        }

        public static void ToggleConnection(string address)
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
                    AutoFlushSendQueue = true
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

                EntityPool.AddPlayer();

                // Send HandshakePacket
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
        }
        private static void PlayerDisconnect(Packets.PlayerDisconnect packet)
        {
            var name=PlayerList.GetPlayer(packet.PedID).Username;
            GTA.UI.Notification.Show($"{name} left.");
            PlayerList.RemovePlayer(packet.PedID);
            EntityPool.RemoveAllFromPlayer(packet.PedID);


        }
        /*
        private static void DecodeNativeCallWithResponse(Packets.NativeResponse packet)
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
        */
        #endregion // -- PLAYER --

        #endregion

    }
}
