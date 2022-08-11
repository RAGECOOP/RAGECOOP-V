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
        public static CoopPeer Peer;
        public static float Latency => ServerConnection.AverageRoundtripTime/2;
        public static bool ShowNetworkInfo = false;
        public static Security Security;
        public static NetConnection ServerConnection;
        private static readonly Dictionary<int, Action<PacketType, byte[]>> PendingResponses = new Dictionary<int, Action<PacketType, byte[]>>();
        internal static readonly Dictionary<PacketType, Func<byte[], Packet>> RequestHandlers = new Dictionary<PacketType, Func<byte[], Packet>>();
        internal static float SimulatedLatency=0;
        public static bool IsConnecting { get; private set; }
        static Networking()
        {
            Security=new Security(Main.Logger);
        }

        public static void ToggleConnection(string address, string username = null, string password = null)
        {
            Peer?.Dispose();
            if (IsOnServer)
            {
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
                    AutoFlushSendQueue = false,
                    SimulatedMinimumLatency =SimulatedLatency,
                    AcceptIncomingConnections = true,
                    MaximumConnections = 32,
                    PingInterval = 5
                };

                config.EnableMessageType(NetIncomingMessageType.UnconnectedData);
                config.EnableMessageType(NetIncomingMessageType.NatIntroductionSuccess);

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
                        Peer = new CoopPeer(config);
                        Peer.OnMessageReceived+= (s, m) =>
                        {
                            try { ProcessMessage(m); }
                            catch (Exception ex) { Main.Logger.Error(ex); }
                        };
                        Main.QueueAction(() => { GTA.UI.Notification.Show($"~y~Trying to connect..."); });
                        Menus.CoopMenu._serverConnectItem.Enabled=false;
                        Security.Regen();
                        if (!GetServerPublicKey(address))
                        {
                            Menus.CoopMenu._serverConnectItem.Enabled=true;
                            throw new TimeoutException("Failed to retrive server's public key");
                        }

                        // Send HandshakePacket
                        NetOutgoingMessage outgoingMessage = Peer.CreateMessage();
                        var handshake = new Packets.Handshake()
                        {
                            PedID =  Main.LocalPlayerID,
                            Username =username,
                            ModVersion = Main.CurrentVersion,
                            PasswordEncrypted=Security.Encrypt(password.GetBytes()),
                            InternalEndPoint = new System.Net.IPEndPoint(CoreUtils.GetLocalAddress(ip[0]), Peer.Port)
                        };

                        Security.GetSymmetricKeysCrypted(out handshake.AesKeyCrypted, out handshake.AesIVCrypted);
                        handshake.Pack(outgoingMessage);
                        ServerConnection = Peer.Connect(ip[0], short.Parse(ip[1]), outgoingMessage);

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
            get { return ServerConnection?.Status == NetConnectionStatus.Connected; }
        }

        #region -- PLAYER --
        private static void PlayerConnect(Packets.PlayerConnect packet)
        {
            var p = new Player
            {
                PedID = packet.PedID,
                Username= packet.Username,
            };
            PlayerList.SetPlayer(packet.PedID, packet.Username);

            Main.Logger.Debug($"player connected:{p.Username}");
            Main.QueueAction(() =>
            GTA.UI.Notification.Show($"~h~{p.Username}~h~ connected."));
        }
        private static void PlayerDisconnect(Packets.PlayerDisconnect packet)
        {
            var player = PlayerList.GetPlayer(packet.PedID);
            if (player==null) { return; }
            PlayerList.RemovePlayer(packet.PedID);
            Main.QueueAction(() => {
                EntityPool.RemoveAllFromPlayer(packet.PedID);
                GTA.UI.Notification.Show($"~h~{player.Username}~h~ left.");
            });
        }

        #endregion // -- PLAYER --
        #region -- GET --

        private static bool GetServerPublicKey(string address, int timeout = 10000)
        {
            Security.ServerRSA=null;
            var msg = Peer.CreateMessage();
            new Packets.PublicKeyRequest().Pack(msg);
            var adds = address.Split(':');
            Peer.SendUnconnectedMessage(msg, adds[0], int.Parse(adds[1]));
            return _publicKeyReceived.WaitOne(timeout) && Security.ServerRSA!=null;
        }

        public static void GetResponse<T>(Packet request, Action<T> callback, ConnectionChannel channel = ConnectionChannel.RequestResponse) where T : Packet, new()
        {
            var received = new AutoResetEvent(false);
            var id = NewRequestID();
            PendingResponses.Add(id, (type, p) =>
            {
                var result = new T();
                result.Deserialize(p);
                callback(result);
            });
            var msg = Peer.CreateMessage();
            msg.Write((byte)PacketType.Request);
            msg.Write(id);
            request.Pack(msg);
            Peer.SendMessage(msg,ServerConnection, NetDeliveryMethod.ReliableOrdered, (int)channel);
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
