using Lidgren.Network;
using RageCoop.Core;

namespace RageCoop.Server;

public partial class Server
{
    private void PedSync(Packets.PedSync packet, Client client)
    {
        QueueJob(() => Entities.Update(packet, client));

        var isPlayer = packet.ID == client.Player.ID;
        if (isPlayer) QueueJob(() => API.Events.InvokePlayerUpdate(client));

        if (Settings.UseP2P) return;
        foreach (var c in ClientsByNetHandle.Values)
        {
            // Don't send data back
            if (c.NetHandle == client.NetHandle) continue;

            // Check streaming distance
            if (isPlayer)
            {
                if (Settings.PlayerStreamingDistance != -1 &&
                    packet.Position.DistanceTo(c.Player.Position) > Settings.PlayerStreamingDistance) continue;
            }
            else if (Settings.NpcStreamingDistance != -1 &&
                     packet.Position.DistanceTo(c.Player.Position) > Settings.NpcStreamingDistance)
            {
                continue;
            }

            var outgoingMessage = MainNetServer.CreateMessage();
            packet.Pack(outgoingMessage);
            MainNetServer.SendMessage(outgoingMessage, c.Connection, NetDeliveryMethod.UnreliableSequenced,
                (byte)ConnectionChannel.PedSync);
        }
    }

    private void VehicleSync(Packets.VehicleSync packet, Client client)
    {
        QueueJob(() => Entities.Update(packet, client));
        var isPlayer = packet.ED.ID == client.Player?.LastVehicle?.ID;


        if (Settings.UseP2P) return;
        foreach (var c in ClientsByNetHandle.Values)
        {
            if (c.NetHandle == client.NetHandle) continue;
            if (isPlayer)
            {
                // Player's vehicle
                if (Settings.PlayerStreamingDistance != -1 &&
                    packet.ED.Position.DistanceTo(c.Player.Position) > Settings.PlayerStreamingDistance) continue;
            }
            else if (Settings.NpcStreamingDistance != -1 &&
                     packet.ED.Position.DistanceTo(c.Player.Position) > Settings.NpcStreamingDistance)
            {
                continue;
            }

            var outgoingMessage = MainNetServer.CreateMessage();
            packet.Pack(outgoingMessage);
            MainNetServer.SendMessage(outgoingMessage, c.Connection, NetDeliveryMethod.UnreliableSequenced,
                (byte)ConnectionChannel.VehicleSync);
        }
    }

    private void ProjectileSync(Packets.ProjectileSync packet, Client client)
    {
        if (Settings.UseP2P) return;
        Forward(packet, client, ConnectionChannel.ProjectileSync);
    }
}