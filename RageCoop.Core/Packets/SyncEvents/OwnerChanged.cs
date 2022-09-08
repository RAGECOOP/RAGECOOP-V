
using Lidgren.Network;

namespace RageCoop.Core
{
    internal partial class Packets
    {

        internal class OwnerChanged : Packet
        {
            public override PacketType Type => PacketType.OwnerChanged;
            public int ID { get; set; }

            public int NewOwnerID { get; set; }

            protected override void Serialize(NetOutgoingMessage m)
            {
                m.Write(ID);
                m.Write(NewOwnerID);
            }

            public override void Deserialize(NetIncomingMessage m)
            {
                #region NetIncomingMessageToPacket


                ID = m.ReadInt32();
                NewOwnerID = m.ReadInt32();

                #endregion
            }
        }




    }
}
