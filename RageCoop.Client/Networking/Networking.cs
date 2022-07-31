using Lidgren.Network;
using RageCoop.Core;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace RageCoop.Client
{
    internal static partial class Networking
    {
        public static NetClient Client;
        public static float Latency = 0;
        public static bool ShowNetworkInfo = false;
        public static Security Security; 
        private static readonly Dictionary<int, Action<PacketType, byte[]>> PendingResponses = new Dictionary<int, Action<PacketType, byte[]>>();
        internal static readonly Dictionary<PacketType, Func<byte[], Packet>> RequestHandlers = new Dictionary<PacketType, Func<byte[], Packet>>();
        public static bool IsConnecting { get; private set; }
        static Networking()
        {
            Security=new Security(Main.Logger);
            RequestHandlers.Add(PacketType.PingPong, (b) =>
            {
                return new Packets.PingPong();
            });
            Task.Run(() =>
            {
                while (true)
                {
                    if (Client!=null)
                    {
                        ProcessMessage(Client.WaitMessage(200));
                    }
                    else
                    {
                        Thread.Sleep(20);
                    }
                }
            });
        }

        public static void ToggleConnection(string address, string username = null, string password = null)
        {
            if (IsOnServer)
            {
                Client.Disconnect("Bye!");
                Client=null;
            }
            else if (IsConnecting) {
                _publicKeyReceived.Set();
                IsConnecting = false;
                GTA.UI.Notification.Show("Connection has been canceled");
            }
            else
            {
                IsConnecting = true;
                password = password ?? Main.Settings.Password;
                username=username ?? Main.Settings.Username;
                // 623c92c287cc392406e7aaaac1c0f3b0 = RAGECOOP
                NetPeerConfiguration config = new NetPeerConfiguration("623c92c287cc392406e7aaaac1c0f3b0")
                {
                    AutoFlushSendQueue = false
                };

                config.EnableMessageType(NetIncomingMessageType.UnconnectedData);


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

                PlayerList.Cleanup();
                EntityPool.AddPlayer();
                Task.Run(() =>
                {
                    try
                    {
                        DownloadManager.Cleanup();
                        Client = new NetClient(config);
                        Client.Start();
                        Main.QueueAction(() => { GTA.UI.Notification.Show($"~y~Trying to connect..."); });
                        Menus.CoopMenu._serverConnectItem.Enabled=false;
                        Security.Regen();
                        if (!GetServerPublicKey(address))
                        {
                            Menus.CoopMenu._serverConnectItem.Enabled=true;
                            throw new TimeoutException("Failed to retrive server's public key");
                        }

                        // Send HandshakePacket
                        NetOutgoingMessage outgoingMessage = Client.CreateMessage();
                        var handshake = new Packets.Handshake()
                        {
                            PedID =  Main.LocalPlayerID,
                            Username =username,
                            ModVersion = Main.CurrentVersion,
                            PasswordEncrypted=Security.Encrypt(password.GetBytes())
                        };

                        Security.GetSymmetricKeysCrypted(out handshake.AesKeyCrypted, out handshake.AesIVCrypted);
                        handshake.Pack(outgoingMessage);
                        Client.Connect(ip[0], short.Parse(ip[1]), outgoingMessage);

                    }
                    catch (Exception ex)
                    {
                        Main.Logger.Error("Cannot connect to server: ", ex);
                        Main.QueueAction(() => GTA.UI.Notification.Show("Cannot connect to server: "+ex.Message));
                    }
                    IsConnecting=false;
                });
            }
        }
        public static bool IsOnServer
        {
            get { return Client?.ConnectionStatus == NetConnectionStatus.Connected; }
        }

        #region -- PLAYER --
        private static void PlayerConnect(Packets.PlayerConnect packet)
        {
            var p = new PlayerData
            {
                PedID = packet.PedID,
                Username= packet.Username,
            };
            GTA.UI.Notification.Show($"~h~{p.Username}~h~ connected.");
            PlayerList.SetPlayer(packet.PedID, packet.Username);

            Main.Logger.Debug($"player connected:{p.Username}");
        }
        private static void PlayerDisconnect(Packets.PlayerDisconnect packet)
        {
            var name = PlayerList.GetPlayer(packet.PedID).Username;
            GTA.UI.Notification.Show($"~h~{name}~h~ left.");
            PlayerList.RemovePlayer(packet.PedID);
            EntityPool.RemoveAllFromPlayer(packet.PedID);


        }

        #endregion // -- PLAYER --
        #region -- GET --

        private static bool GetServerPublicKey(string address, int timeout = 10000)
        {
            Security.ServerRSA=null;
            var msg = Client.CreateMessage();
            new Packets.PublicKeyRequest().Pack(msg);
            var adds = address.Split(':');
            Client.SendUnconnectedMessage(msg, adds[0], int.Parse(adds[1]));
            return _publicKeyReceived.WaitOne(timeout) && Security.ServerRSA!=null;
        }

        public static void GetResponse<T>(Packet request, Action<T> callback, ConnectionChannel channel = ConnectionChannel.RequestResponse) where T : Packet, new()
        {
            var received = new AutoResetEvent(false);
            var id = NewRequestID();
            PendingResponses.Add(id, (type, p) =>
            {
                var result = new T();
                result.Unpack(p);
                callback(result);
            });
            var msg = Client.CreateMessage();
            msg.Write((byte)PacketType.Request);
            msg.Write(id);
            request.Pack(msg);
            Client.SendMessage(msg, NetDeliveryMethod.ReliableOrdered, (int)channel);
        }

        #endregion
        private static int NewRequestID()
        {
            int ID = 0;
            while ((ID==0)
                || PendingResponses.ContainsKey(ID))
            {
                byte[] rngBytes = new byte[4];

                RandomNumberGenerator.Create().GetBytes(rngBytes);

                // Convert the bytes into an integer
                ID = BitConverter.ToInt32(rngBytes, 0);
            }
            return ID;
        }

    }
}
