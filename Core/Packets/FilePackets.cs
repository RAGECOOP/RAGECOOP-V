using System;
using System.Collections.Generic;
using System.Text;

using Lidgren.Network;

namespace RageCoop.Core
{
    public enum FileType:byte
    {
        Resource=0,
        Custom=1,
    }
    public partial class Packets
    {
        public class FileTransferRequest : Packet
        {
            public int ID { get; set; }

            public string Name { get; set; }

            public long FileLength { get; set; }

            public override void Pack(NetOutgoingMessage message)
            {
                #region PacketToNetOutGoingMessage
                message.Write((byte)PacketTypes.FileTransferRequest);

                List<byte> byteArray = new List<byte>();

                // The ID from the download
                byteArray.AddInt(ID);


                // The name of the file
                byte[] nameBytes = Encoding.UTF8.GetBytes(Name);
                byteArray.AddRange(BitConverter.GetBytes(nameBytes.Length));
                byteArray.AddRange(nameBytes);

                // The length of the file
                byteArray.AddRange(BitConverter.GetBytes(FileLength));

                byte[] result = byteArray.ToArray();

                message.Write(result.Length);
                message.Write(result);
                #endregion
            }

            public override void Unpack(byte[] array)
            {
                #region NetIncomingMessageToPacket
                BitReader reader = new BitReader(array);

                ID = reader.ReadInt();
                int nameArrayLength = reader.ReadInt();
                Name = reader.ReadString(nameArrayLength);
                FileLength = reader.ReadLong();
                #endregion
            }
        }

        public class FileTransferChunk : Packet
        {
            public int ID { get; set; }

            public byte[] FileChunk { get; set; }

            public override void Pack(NetOutgoingMessage message)
            {
                #region PacketToNetOutGoingMessage
                message.Write((byte)PacketTypes.FileTransferChunk);

                List<byte> byteArray = new List<byte>();

                // The ID from the download
                byteArray.AddInt(ID);

                // The chunk of the file
                byteArray.AddRange(BitConverter.GetBytes(FileChunk.Length));
                byteArray.AddRange(FileChunk);

                byte[] result = byteArray.ToArray();

                message.Write(result.Length);
                message.Write(result);
                #endregion
            }

            public override void Unpack(byte[] array)
            {
                #region NetIncomingMessageToPacket
                BitReader reader = new BitReader(array);

                ID = reader.ReadInt();
                int chunkLength = reader.ReadInt();
                FileChunk = reader.ReadByteArray(chunkLength);
                #endregion
            }
        }

        public class FileTransferComplete : Packet
        {
            public int ID { get; set; }

            public override void Pack(NetOutgoingMessage message)
            {
                #region PacketToNetOutGoingMessage
                message.Write((byte)PacketTypes.FileTransferComplete);

                List<byte> byteArray = new List<byte>();

                // The ID from the download
                byteArray.AddInt(ID);

                byte[] result = byteArray.ToArray();

                message.Write(result.Length);
                message.Write(result);
                #endregion
            }

            public override void Unpack(byte[] array)
            {
                #region NetIncomingMessageToPacket
                BitReader reader = new BitReader(array);

                ID = reader.ReadInt();
                #endregion
            }
        }
    }
}
