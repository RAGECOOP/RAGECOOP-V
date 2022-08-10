using System;
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
            public override byte[] Serialize()
            {
                var data=new List<byte>(10);
                data.AddInt(TargetID);
                return data.ToArray();
            }
            public override void Deserialize(byte[] array)
            {
                var reader=new BitReader(array);
                TargetID = reader.ReadInt32();
            }
        }


        /// <summary>
        /// Sent to the host when a direct connection has been established
        /// </summary>
        internal class P2PConnect : Packet
        {
            public int ID { get; set; }
            public override PacketType Type => PacketType.P2PConnect;
            public override byte[] Serialize()
            {
                var data = new List<byte>(10);
                data.AddInt(ID);
                return data.ToArray();
            }
            public override void Deserialize(byte[] array)
            {
                var reader = new BitReader(array);
                ID = reader.ReadInt32();
            }
        }
    }
}
