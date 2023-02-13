using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Timers;
using Lidgren.Network;
using RageCoop.Core;

namespace RageCoop.Client
{
    internal static class HolePunch
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
                if (!Networking.IsOnServer) return;
                foreach (var p in PlayerList.Players.Values.ToArray())
                    if (p.InternalEndPoint != null && p.ExternalEndPoint != null && (p.Connection == null ||
                            p.Connection.Status == NetConnectionStatus.Disconnected))
                    {
                        Log.Trace(
                            $"Sending HolePunch message to {p.InternalEndPoint},{p.ExternalEndPoint}. {p.Username}:{p.ID}");
                        var msg = Networking.Peer.CreateMessage();
                        new Packets.HolePunch
                        {
                            Puncher = Main.LocalPlayerID,
                            Status = p.HolePunchStatus
                        }.Pack(msg);
                        Networking.Peer.SendUnconnectedMessage(msg,
                            new List<IPEndPoint> { p.InternalEndPoint, p.ExternalEndPoint });
                    }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        public static void Add(Packets.HolePunchInit p)
        {
            if (PlayerList.Players.TryGetValue(p.TargetID, out var player))
            {
                Log.Debug($"{p.TargetID},{player.Username} added to HolePunch target");
                player.InternalEndPoint = CoreUtils.StringToEndPoint(p.TargetInternal);
                player.ExternalEndPoint = CoreUtils.StringToEndPoint(p.TargetExternal);
                player.ConnectWhenPunched = p.Connect;
            }
            else
            {
                Log.Warning("No player with specified TargetID found for hole punching:" + p.TargetID);
            }
        }

        public static void Punched(Packets.HolePunch p, IPEndPoint from)
        {
            Log.Debug($"HolePunch message received from:{from}, status:{p.Status}");
            if (PlayerList.Players.TryGetValue(p.Puncher, out var puncher))
            {
                Log.Debug("Puncher identified as: " + puncher.Username);
                puncher.HolePunchStatus = (byte)(p.Status + 1);
                if (p.Status >= 3)
                {
                    Log.Debug("HolePunch sucess: " + from + ", " + puncher.ID);
                    if (puncher.ConnectWhenPunched && (puncher.Connection == null ||
                                                       puncher.Connection.Status == NetConnectionStatus.Disconnected))
                    {
                        Log.Debug("Connecting to peer: " + from);
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