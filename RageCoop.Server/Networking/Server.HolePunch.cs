using Lidgren.Network;
using RageCoop.Core;

namespace RageCoop.Server
{
    public partial class Server
    {
        private void HolePunch(Client host, Client client)
        {
            // Send to host
            Send(new Packets.HolePunchInit
            {
                Connect = false,
                TargetID = client.Player.ID,
                TargetInternal = client.InternalEndPoint.ToString(),
                TargetExternal = client.EndPoint.ToString()
            }, host, ConnectionChannel.Default, NetDeliveryMethod.ReliableOrdered);

            // Send to client
            Send(new Packets.HolePunchInit
            {
                Connect = true,
                TargetID = host.Player.ID,
                TargetInternal = host.InternalEndPoint.ToString(),
                TargetExternal = host.EndPoint.ToString()
            }, client, ConnectionChannel.Default, NetDeliveryMethod.ReliableOrdered);
        }
    }
}
