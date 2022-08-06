using System;
using System.Collections.Generic;
using Lidgren.Network;

namespace RageCoop.Core
{

    internal partial class Packets
    {

        internal class ChatMessage : Packet
        {
            public override PacketType Type { get { return PacketType.ChatMessage; } }
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

            public override byte[] Serialize()
            {

                List<byte> byteArray = new List<byte>();



                // Write Username
                byteArray.AddString(Username);


                // Write Message
                byteArray.AddArray(crypt(Message));

                return byteArray.ToArray();

            }

            public override void Deserialize(byte[] array)
            {
                #region NetIncomingMessageToPacket
                BitReader reader = new BitReader(array);

                // Read username
                int usernameLength = reader.ReadInt();
                Username = reader.ReadString(usernameLength);

                Message = decrypt(reader.ReadByteArray()).GetString();
                #endregion
            }
        }
    }
}
