using Lidgren.Network;
using RageCoop.Core;
using RageCoop.Core.Scripting;
using RageCoop.Server.Scripting;
using System;
using System.Linq;
using System.Text;

namespace RageCoop.Server
{
    public partial class Server
    {
        private void DisconnectAndLog(NetConnection senderConnection, PacketType type, Exception e)
        {
            Logger?.Error($"Error receiving a packet of type {type}");
            Logger?.Error(e.Message);
            Logger?.Error(e.StackTrace);
            senderConnection.Disconnect(e.Message);
        }

        private void GetHandshake(NetConnection connection, Packets.Handshake packet)
        {
            Logger?.Debug("New handshake from: [Name: " + packet.Username + " | Address: " + connection.RemoteEndPoint.Address.ToString() + "]");
            if (!packet.ModVersion.StartsWith(Version.ToString(3)))
            {
                connection.Deny($"RAGECOOP version {Version.ToString(3)} required!");
                return;
            }
            if (string.IsNullOrWhiteSpace(packet.Username))
            {
                connection.Deny("Username is empty or contains spaces!");
                return;
            }
            if (packet.Username.Any(p => !_allowedCharacterSet.Contains(p)))
            {
                connection.Deny("Username contains special chars!");
                return;
            }
            if (ClientsByNetHandle.Values.Any(x => x.Username.ToLower() == packet.Username.ToLower()))
            {
                connection.Deny("Username is already taken!");
                return;
            }

            try
            {
                Security.AddConnection(connection.RemoteEndPoint, packet.AesKeyCrypted, packet.AesIVCrypted);

                var args = new HandshakeEventArgs()
                {
                    EndPoint = connection.RemoteEndPoint,
                    ID = packet.PedID,
                    Username = packet.Username,
                    PasswordHash = Security.Decrypt(packet.PasswordEncrypted, connection.RemoteEndPoint).GetString().GetSHA256Hash().ToHexString(),
                };
                API.Events.InvokePlayerHandshake(args);
                if (args.Cancel)
                {
                    connection.Deny(args.DenyReason);
                    return;
                }
            }
            catch (Exception ex)
            {
                Logger?.Error($"Cannot process handshake packet from {connection.RemoteEndPoint}");
                Logger?.Error(ex);
                connection.Deny("Malformed handshak packet!");
                return;
            }

            var handshakeSuccess = MainNetServer.CreateMessage();
            var currentClients = ClientsByID.Values.ToArray();
            var players = new Packets.PlayerData[currentClients.Length];
            for (int i = 0; i < players.Length; i++)
            {
                players[i] = new Packets.PlayerData()
                {
                    ID = currentClients[i].Player.ID,
                    Username = currentClients[i].Username,
                };
            }

            new Packets.HandshakeSuccess()
            {
                Players = players
            }.Pack(handshakeSuccess);
            connection.Approve(handshakeSuccess);
            Client tmpClient;

            // Add the player to Players
            lock (ClientsByNetHandle)
            {
                var player = new ServerPed(this)
                {
                    ID = packet.PedID,
                };
                Entities.Add(player);
                ClientsByNetHandle.Add(connection.RemoteUniqueIdentifier,
                    tmpClient = new Client(this)
                    {
                        NetHandle = connection.RemoteUniqueIdentifier,
                        Connection = connection,
                        Username = packet.Username,
                        Player = player,
                        InternalEndPoint = packet.InternalEndPoint,
                    }
                );
                player.Owner = tmpClient;
                ClientsByName.Add(packet.Username.ToLower(), tmpClient);
                ClientsByID.Add(player.ID, tmpClient);
                if (ClientsByNetHandle.Count == 1)
                {
                    _hostClient = tmpClient;
                }
            }

            Logger?.Debug($"Handshake sucess, Player:{packet.Username} PedID:{packet.PedID}");

        }

        // The connection has been approved, now we need to send all other players to the new player and the new player to all players
        private void PlayerConnected(Client newClient)
        {
            if (newClient == _hostClient)
            {
                API.SendCustomEvent(new() { newClient }, CustomEvents.IsHost, true);
            }

            // Send new client to all players
            var cons = MainNetServer.Connections.Exclude(newClient.Connection);
            if (cons.Count != 0)
            {
                NetOutgoingMessage outgoingMessage = MainNetServer.CreateMessage();
                new Packets.PlayerConnect()
                {
                    PedID = newClient.Player.ID,
                    Username = newClient.Username
                }.Pack(outgoingMessage);

                MainNetServer.SendMessage(outgoingMessage, cons, NetDeliveryMethod.ReliableOrdered, 0);

            }

            // Send all props to this player
            BaseScript.SendServerPropsTo(new(Entities.ServerProps.Values), new() { newClient });

            // Send all blips to this player
            BaseScript.SendServerBlipsTo(new(Entities.Blips.Values), new() { newClient });

            // Create P2P connection
            if (Settings.UseP2P)
            {
                ClientsByNetHandle.Values.ForEach(target =>
                {
                    if (target == newClient) { return; }
                    HolePunch(target, newClient);
                });
            }

            Logger?.Info($"Player {newClient.Username} connected!");

            if (!string.IsNullOrEmpty(Settings.WelcomeMessage))
            {
                SendChatMessage("Server", Settings.WelcomeMessage, newClient);
            }
        }

        // Send all players a message that someone has left the server
        private void PlayerDisconnected(Client localClient)
        {
            var cons = MainNetServer.Connections.Exclude(localClient.Connection);
            if (cons.Count != 0)
            {
                NetOutgoingMessage outgoingMessage = MainNetServer.CreateMessage();
                new Packets.PlayerDisconnect()
                {
                    PedID = localClient.Player.ID,

                }.Pack(outgoingMessage);
                MainNetServer.SendMessage(outgoingMessage, cons, NetDeliveryMethod.ReliableOrdered, 0);
            }
            Entities.CleanUp(localClient);
            QueueJob(() => API.Events.InvokePlayerDisconnected(localClient));
            Logger?.Info($"Player {localClient.Username} disconnected! ID:{localClient.Player.ID}");
            if (ClientsByNetHandle.ContainsKey(localClient.NetHandle)) { ClientsByNetHandle.Remove(localClient.NetHandle); }
            if (ClientsByName.ContainsKey(localClient.Username.ToLower())) { ClientsByName.Remove(localClient.Username.ToLower()); }
            if (ClientsByID.ContainsKey(localClient.Player.ID)) { ClientsByID.Remove(localClient.Player.ID); }
            if (localClient == _hostClient)
            {

                _hostClient = ClientsByNetHandle.Values.FirstOrDefault();
                _hostClient?.SendCustomEvent(CustomEvents.IsHost, true);
            }
            Security.RemoveConnection(localClient.Connection.RemoteEndPoint);
        }
    }
}
