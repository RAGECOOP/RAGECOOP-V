using System;
using Lidgren.Network;
using RageCoop.Core;
using System.Threading.Tasks;
using System.Threading;
using System.IO;

namespace RageCoop.Client
{
    internal static partial class Networking
    {
        public static NetClient Client;
        public static float Latency = 0;
        public static bool ShowNetworkInfo = false;
        public static Security Security;
        static Networking()
        {
            Security=new Security(Main.Logger);
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

        public static void ToggleConnection(string address,string username=null,string password=null)
        {
            if (IsOnServer)
            {
                Client.Disconnect("Bye!");
            }
            else
            {
                password = password ?? Main.Settings.Password;
                username=username ?? Main.Settings.Username;
                // 623c92c287cc392406e7aaaac1c0f3b0 = RAGECOOP
                NetPeerConfiguration config = new NetPeerConfiguration("623c92c287cc392406e7aaaac1c0f3b0")
                {
                    AutoFlushSendQueue = true
                };

                config.EnableMessageType(NetIncomingMessageType.ConnectionLatencyUpdated);
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
                        if(!GetServerPublicKey(address))
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
                            PassHashEncrypted=Security.Encrypt(password.GetHash())
                        };

                        Security.GetSymmetricKeysCrypted(out handshake.AesKeyCrypted, out handshake.AesIVCrypted);
                        handshake.Pack(outgoingMessage);
                        Client.Connect(ip[0], short.Parse(ip[1]), outgoingMessage);

                    }
                    catch(Exception ex)
                    {
                        Main.Logger.Error("Cannot connect to server: ", ex);
                        Main.QueueAction(() => GTA.UI.Notification.Show("Cannot connect to server: "+ex.Message));
                    }

                });
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

        #endregion // -- PLAYER --

        private static bool GetServerPublicKey(string address,int timeout=10000)
        {
            var msg=Client.CreateMessage();
            new Packets.PublicKeyRequest().Pack(msg); 
            var adds =address.Split(':');
            Client.SendUnconnectedMessage(msg,adds[0],int.Parse(adds[1]));
            return _publicKeyReceived.WaitOne(timeout); 
        }
        #endregion

    }
}
