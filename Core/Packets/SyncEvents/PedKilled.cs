
using Lidgren.Network;

namespace RageCoop.Core
{
    internal partial class Packets
    {

        internal class PedKilled : Packet
        {
            public override PacketType Type => PacketType.PedKilled;
            public int VictimID { get; set; }

            protected override void Serialize(NetOutgoingMessage m)
            {



                m.Write(VictimID);


            }

            public override void Deserialize(NetIncomingMessage m)
            {
                #region NetIncomingMessageToPacket


                VictimID = m.ReadInt32();

                #endregion
            }
        }




    }
}
