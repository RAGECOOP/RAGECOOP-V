using Lidgren.Network;
using System.Collections.Generic;

namespace RageCoop.Core
{
    internal partial class Packets
    {
        /// <summary>
        /// Request direct connection to another client
        /// </summary>
        internal class ConnectionRequest : Packet
        {
            public int TargetID { get; set; }
            public override PacketType Type => PacketType.ConnectionRequest;
            protected override void Serialize(NetOutgoingMessage m)
            {
                var data = new List<byte>(10);
                m.Write(TargetID);
            }
            public override void Deserialize(NetIncomingMessage m)
            {

                TargetID = m.ReadInt32();
            }
        }


        /// <summary>
        /// Sent to the host when a direct connection has been established
        /// </summary>
        internal class P2PConnect : Packet
        {
            public int ID { get; set; }
            public override PacketType Type => PacketType.P2PConnect;
            protected override void Serialize(NetOutgoingMessage m)
            {
                var data = new List<byte>(10);
                m.Write(ID);

            }
            public override void Deserialize(NetIncomingMessage m)
            {

                ID = m.ReadInt32();
            }
        }
    }
}
