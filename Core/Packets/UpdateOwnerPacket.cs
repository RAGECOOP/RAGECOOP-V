using System;
using System.Collections.Generic;
using System.Text;

using Lidgren.Network;

namespace RageCoop.Core
{
    public partial class Packets
    {
        /// <summary>
        /// Non-critical stuff, such as damage model, landing gear, health, etc..
        /// </summary>
        public class UpdateOwner : Packet
        {
            public int ID { get; set; }
            public int OwnerID { get; set; }
            public override void Pack(NetOutgoingMessage message)
            {
                #region PacketToNetOutGoingMessage
                message.Write((byte)PacketTypes.PlayerConnect);

                List<byte> byteArray = new List<byte>();

                // Write ID
                byteArray.AddRange(BitConverter.GetBytes(ID));

                // Write OwnerID
                byteArray.AddRange(BitConverter.GetBytes(OwnerID));

                byte[] result = byteArray.ToArray();

                message.Write(result.Length);
                message.Write(result);
                #endregion
            }

            public override void Unpack(byte[] array)
            {
                BitReader reader = new BitReader(array);

                // Read player ID
                ID = reader.ReadInt();

                // Read Username
                OwnerID = reader.ReadInt();
            }
        }
    }
}
