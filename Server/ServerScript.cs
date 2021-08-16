using System;
using System.Collections.Generic;

using Lidgren.Network;

namespace CoopServer
{
    class DefaultScript : ServerScript
    {
        private List<string> Admins = new();
        private string AdminPassword = "test123";

        public override void OnPlayerConnect(Entities.EntitiesPlayer client)
        {
            SendChatMessageToAll("Say hello to " + client.Username);
        }

        public override void OnPlayerDisconnect(Entities.EntitiesPlayer player, string reason)
        {
            Logging.Info(player.Username + " left the server, reason: " + reason);

            if (Admins.Contains(player.Username))
            {
                Admins.Remove(player.Username);
            }
        }

        public override bool OnChatMessage(string username, string message)
        {
            if (!message.StartsWith("/"))
            {
                return false;
            }

            string[] messageSplitted = message.Split(" ");
            int messageSplittedLength = messageSplitted.Length;

            if (messageSplittedLength == 0)
            {
                return true;
            }

            if (!Admins.Contains(username))
            {
                if (messageSplitted[0] != "/rcon")
                {
                    SendChatMessageToPlayer(username, "Please login with \"/rcon <PASSWORD>\"!");
                    return true;
                }

                if (messageSplitted.Length < 2)
                {
                    SendChatMessageToPlayer(username, "Password missing!");
                    return true;
                }

                if (messageSplitted[1] != AdminPassword)
                {
                    SendChatMessageToPlayer(username, "Wrong password!");
                    Logging.Warning("Player [" + username + "] tried to login rcon with [" + messageSplitted[1] + "]");
                    return true;
                }

                Admins.Add(username);

                SendChatMessageToPlayer(username, "Login successfully!");
                Logging.Info("Login successfully! [RCON][" + username + "]");
                return true;
            }

            if (messageSplitted[0] == "/kick")
            {
                if (messageSplittedLength < 3)
                {
                    SendChatMessageToPlayer(username, "Please use \"/kick <USERNAME> <REASON>\"");
                    return true;
                }

                try
                {
                    KickPlayerByUsername(messageSplitted[1], messageSplittedLength >= 3 ? messageSplitted[2] : "Kicked by " + username + "!");
                    SendChatMessageToPlayer(username, "Player [" + messageSplitted[1] + "] kicked!");
                }
                catch (Exception e)
                {
                    SendChatMessageToPlayer(username, e.Message);
                }
                return true;
            }

            SendChatMessageToPlayer(username, "Command \"" + messageSplitted[0] + "\" not found!");
            return true;
        }
    }

    public class ServerScript
    {
        public virtual void Start()
        {
            Logging.Info("Gamemode loaded successfully!");
        }

        public virtual void OnPlayerConnect(Entities.EntitiesPlayer player)
        {
            Logging.Info("New player [" + player.SocialClubName + " | " + player.Username + "] connected!");
        }

        public virtual void OnPlayerDisconnect(Entities.EntitiesPlayer player, string reason)
        {
            Logging.Info(player.Username + " left the server, reason: " + reason);
        }

        public virtual bool OnChatMessage(string username, string message) { return false; }

        protected static List<string> GetAllConnections()
        {
            List<string> result = new();

            lock (Server.MainNetServer.Connections)
            {
                Server.MainNetServer.Connections.ForEach(con => result.Add(NetUtility.ToHexString(con.RemoteUniqueIdentifier)));
            }

            return result;
        }

        protected static int GetAllPlayersCount() { lock (Server.Players) return Server.Players.Count; }
        protected static Dictionary<long, Entities.EntitiesPlayer> GetAllPlayers() { lock (Server.Players) return Server.Players; }

        protected static void KickPlayerByUsername(string username, string reason)
        {
            lock (Server.Players)
            {
                foreach (KeyValuePair<long, Entities.EntitiesPlayer> player in Server.Players)
                {
                    if (player.Value.Username == username)
                    {
                        Server.MainNetServer.Connections.Find(e => e.RemoteUniqueIdentifier == player.Key).Disconnect(reason);
                        return;
                    }
                }
            }

            throw new Exception("Player [" + username + "] not found!");
        }

        protected static void SendChatMessageToAll(string message, string username = "Server")
        {
            ChatMessagePacket packet = new()
            {
                Username = username,
                Message = message
            };

            Logging.Info(username + ": " + packet.Message);

            NetOutgoingMessage outgoingMessage = Server.MainNetServer.CreateMessage();
            packet.PacketToNetOutGoingMessage(outgoingMessage);
            Server.MainNetServer.SendMessage(outgoingMessage, Server.MainNetServer.Connections, NetDeliveryMethod.ReliableOrdered, 0);
        }

        protected static void SendChatMessageToPlayer(string username, string message, string from = "Server")
        {
            lock (Server.Players)
            {
                foreach (KeyValuePair<long, Entities.EntitiesPlayer> player in Server.Players)
                {
                    if (player.Value.Username == username)
                    {
                        ChatMessagePacket packet = new()
                        {
                            Username = from,
                            Message = message
                        };

                        Logging.Info(from + ": " + packet.Message);

                        NetOutgoingMessage outgoingMessage = Server.MainNetServer.CreateMessage();
                        packet.PacketToNetOutGoingMessage(outgoingMessage);
                        Server.MainNetServer.SendMessage(outgoingMessage, Server.MainNetServer.Connections.Find(con => con.RemoteUniqueIdentifier == player.Key), NetDeliveryMethod.ReliableOrdered, 0);
                        return;
                    }
                }
            }
        }
    }
}
