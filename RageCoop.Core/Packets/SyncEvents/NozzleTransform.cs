
using Lidgren.Network;

namespace RageCoop.Core
{
    internal partial class Packets
    {
        internal class NozzleTransform : Packet
        {
            public override PacketType Type => PacketType.NozzleTransform;
            public int VehicleID { get; set; }

            public bool Hover { get; set; }

            protected override void Serialize(NetOutgoingMessage m)
            {



                m.Write(VehicleID);
                m.Write(Hover);



            }

            public override void Deserialize(NetIncomingMessage m)
            {
                #region NetIncomingMessageToPacket

                VehicleID = m.ReadInt32();
                Hover = m.ReadBoolean();

                #endregion
            }
        }




    }
}
