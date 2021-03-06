using System;
using System.Collections.Generic;
using Lidgren.Network;

namespace RageCoop.Core
{
    internal partial class Packets
    {
        internal class PingPong : Packet
        {

            public override void Pack(NetOutgoingMessage message)
            {
                #region PacketToNetOutGoingMessage
                message.Write((byte)PacketType.PingPong);
                message.Write(1);
                message.Write((byte)0);
                #endregion
            }

            public override void Unpack(byte[] array)
            {
            }
        }
    }
}
