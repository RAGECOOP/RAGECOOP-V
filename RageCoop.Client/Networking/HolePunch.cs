using Lidgren.Network;
using RageCoop.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Timers;

namespace RageCoop.Client
{
    internal static partial class HolePunch
    {
        static HolePunch()
        {
            // Periodically send hole punch message as needed
            var timer = new Timer(1000);
            timer.Elapsed += DoPunch;
            timer.Enabled = true;
        }

        private static void DoPunch(object sender, ElapsedEventArgs e)
        {
            try
            {
                if (!Networking.IsOnServer) { return; }
                foreach (var p in PlayerList.Players.Values.ToArray())
                {
                    if (p.InternalEndPoint != null && p.ExternalEndPoint != null && (p.Connection == null || p.Connection.Status == NetConnectionStatus.Disconnected))
                    {
                        Main.Logger.Trace($"Sending HolePunch message to {p.InternalEndPoint},{p.ExternalEndPoint}. {p.Username}:{p.ID}");
                        var msg = Networking.Peer.CreateMessage();
                        new Packets.HolePunch
                        {
                            Puncher = Main.LocalPlayerID,
                            Status = p.HolePunchStatus
                        }.Pack(msg);
                        Networking.Peer.SendUnconnectedMessage(msg, new List<IPEndPoint> { p.InternalEndPoint, p.ExternalEndPoint });
                    }
                }
            }
            catch (Exception ex)
            {
                Main.Logger.Error(ex);
            }
        }

        public static void Add(Packets.HolePunchInit p)
        {
            if (PlayerList.Players.TryGetValue(p.TargetID, out var player))
            {
                Main.Logger.Debug($"{p.TargetID},{player.Username} added to HolePunch target");
                player.InternalEndPoint = CoreUtils.StringToEndPoint(p.TargetInternal);
                player.ExternalEndPoint = CoreUtils.StringToEndPoint(p.TargetExternal);
                player.ConnectWhenPunched = p.Connect;
            }
            else
            {
                Main.Logger.Warning("No player with specified TargetID found for hole punching:" + p.TargetID);
            }
        }
        public static void Punched(Packets.HolePunch p, IPEndPoint from)
        {
            Main.Logger.Debug($"HolePunch message received from:{from}, status:{p.Status}");
            if (PlayerList.Players.TryGetValue(p.Puncher, out var puncher))
            {
                Main.Logger.Debug("Puncher identified as: " + puncher.Username);
                puncher.HolePunchStatus = (byte)(p.Status + 1);
                if (p.Status >= 3)
                {
                    Main.Logger.Debug("HolePunch sucess: " + from + ", " + puncher.ID);
                    if (puncher.ConnectWhenPunched && (puncher.Connection == null || puncher.Connection.Status == NetConnectionStatus.Disconnected))
                    {
                        Main.Logger.Debug("Connecting to peer: " + from);
                        var msg = Networking.Peer.CreateMessage();
                        new Packets.P2PConnect { ID = Main.LocalPlayerID }.Pack(msg);
                        puncher.Connection = Networking.Peer.Connect(from, msg);
                        Networking.Peer.FlushSendQueue();
                    }
                }
            }
        }
    }
}
