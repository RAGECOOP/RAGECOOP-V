using System;
using System.Collections.Generic;
using Lidgren.Network;

namespace RageCoop.Core
{

    internal partial class Packets
    {

        internal class ChatMessage : Packet
        {
            public override PacketType Type  => PacketType.ChatMessage;
            private Func<string, byte[]> crypt;
            private Func<byte[], byte[]> decrypt;
            public ChatMessage(Func<string, byte[]> crypter)
            {
                crypt = crypter;
            }
            public ChatMessage(Func<byte[], byte[]> decrypter)
            {
                decrypt = decrypter;
            }
            public string Username { get; set; }

            public string Message { get; set; }

            protected override void Serialize(NetOutgoingMessage m)
            {





                // Write Username
                m.Write(Username);


                // Write Message
                m.WriteByteArray(crypt(Message));

            }

            public override void Deserialize(NetIncomingMessage m)
            {
                #region NetIncomingMessageToPacket


                // Read username
                Username = m.ReadString();

                Message = decrypt(m.ReadByteArray()).GetString();
                #endregion
            }
        }
    }
}
