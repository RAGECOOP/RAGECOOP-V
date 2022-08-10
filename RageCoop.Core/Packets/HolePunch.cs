using System;
using System.Collections.Generic;
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
            public override byte[] Serialize()
            {

                List<byte> byteArray = new List<byte>();
                byteArray.AddInt(TargetID);
                byteArray.AddString(TargetInternal);
                byteArray.AddString(TargetExternal);
                byteArray.AddBool(Connect);
                return byteArray.ToArray();

            }

            public override void Deserialize(byte[] array)
            {
                #region NetIncomingMessageToPacket
                BitReader reader = new BitReader(array);
                TargetID = reader.ReadInt32();
                TargetInternal = reader.ReadString();
                TargetExternal = reader.ReadString();
                Connect=reader.ReadBoolean();
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
            public byte Status { get;set;}
            public override byte[] Serialize()
            {

                List<byte> byteArray = new List<byte>();
                byteArray.AddInt(Puncher);
                byteArray.Add(Status);
                return byteArray.ToArray();

            }

            public override void Deserialize(byte[] array)
            {
                #region NetIncomingMessageToPacket
                BitReader reader = new BitReader(array);
                Puncher = reader.ReadInt32();
                Status = reader.ReadByte();
                #endregion
            }
        }
    }
}
