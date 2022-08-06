using System;
using System.Collections.Generic;
using System.Net;

namespace RageCoop.Core
{
    internal partial class Packets
    {

        /// <summary>
        /// Used to measure the connection latency
        /// </summary>
        internal class PingPong : Packet
        {
            public override PacketType Type  => PacketType.PingPong;
        }

        /// <summary>
        /// Request direct connection to another client
        /// </summary>
        internal class ConnectionRequest : Packet
        {
            public int TargetID { get; set; }
            public IPEndPoint InternalEndPoint { get; set; }
            public override PacketType Type => PacketType.ConnectionRequest;
            public override byte[] Serialize()
            {
                var data=new List<byte>(10);
                data.AddInt(TargetID);
                data.AddString(InternalEndPoint.ToString());
                return data.ToArray();
            }
            public override void Deserialize(byte[] array)
            {
                var reader=new BitReader(array);
                TargetID = reader.ReadInt32();
                InternalEndPoint=CoreUtils.StringToEndPoint(reader.ReadString());
            }
        }
    }
}
