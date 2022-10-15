using Lidgren.Network;

namespace RageCoop.Core
{
    internal partial class Packets
    {

        internal class HolePunchInit : Packet
        {
            public override PacketType Type => PacketType.HolePunchInit;
            public int TargetID { get; set; }
            public string TargetInternal { get; set; }
            public string TargetExternal { get; set; }
            public bool Connect { get; set; }
            protected override void Serialize(NetOutgoingMessage m)
            {


                m.Write(TargetID);
                m.Write(TargetInternal);
                m.Write(TargetExternal);
                m.Write(Connect);


            }

            public override void Deserialize(NetIncomingMessage m)
            {
                #region NetIncomingMessageToPacket

                TargetID = m.ReadInt32();
                TargetInternal = m.ReadString();
                TargetExternal = m.ReadString();
                Connect = m.ReadBoolean();
                #endregion
            }
        }
        internal class HolePunch : Packet
        {
            public override PacketType Type => PacketType.HolePunch;
            public int Puncher { get; set; }

            /// <summary>
            /// 1:initial, 2:acknowledged, 3:confirmed
            /// </summary>
            public byte Status { get; set; }
            protected override void Serialize(NetOutgoingMessage m)
            {


                m.Write(Puncher);
                m.Write(Status);


            }

            public override void Deserialize(NetIncomingMessage m)
            {
                #region NetIncomingMessageToPacket

                Puncher = m.ReadInt32();
                Status = m.ReadByte();
                #endregion
            }
        }
    }
}
