using System;
using System.Collections.Generic;
using System.Text;

using Lidgren.Network;

namespace CoopServer
{
    internal partial class Packets
    {
        public enum DataFileType
        {
            Script = 0,
            Map = 1
        }

        public class FileTransferRequest : Packet
        {
            public byte ID { get; set; }

            public byte FileType { get; set; }

            public string FileName { get; set; }

            public long FileLength { get; set; }

            public override void PacketToNetOutGoingMessage(NetOutgoingMessage message)
            {
                #region PacketToNetOutGoingMessage
                message.Write((byte)PacketTypes.FileTransferRequest);

                List<byte> byteArray = new List<byte>();

                // The ID from the download
                byteArray.Add(ID);

                // The type of the file
                byteArray.Add(FileType);

                // The name of the file
                byte[] nameBytes = Encoding.UTF8.GetBytes(FileName);
                byteArray.AddRange(BitConverter.GetBytes(nameBytes.Length));
                byteArray.AddRange(nameBytes);

                // The length of the file
                byteArray.AddRange(BitConverter.GetBytes(FileLength));

                byte[] result = byteArray.ToArray();

                message.Write(result.Length);
                message.Write(result);
                #endregion
            }

            public override void NetIncomingMessageToPacket(byte[] array)
            {
                #region NetIncomingMessageToPacket
                BitReader reader = new BitReader(array);

                ID = reader.ReadByte();
                FileType = reader.ReadByte();
                int nameArrayLength = reader.ReadInt();
                FileName = reader.ReadString(nameArrayLength);
                FileLength = reader.ReadLong();
                #endregion
            }
        }

        public class FileTransferTick : Packet
        {
            public byte ID { get; set; }

            public byte[] FileChunk { get; set; }

            public override void PacketToNetOutGoingMessage(NetOutgoingMessage message)
            {
                #region PacketToNetOutGoingMessage
                message.Write((byte)PacketTypes.FileTransferTick);

                List<byte> byteArray = new List<byte>();

                // The ID from the download
                byteArray.Add(ID);

                // The chunk of the file
                byteArray.AddRange(BitConverter.GetBytes(FileChunk.Length));
                byteArray.AddRange(FileChunk);

                byte[] result = byteArray.ToArray();

                message.Write(result.Length);
                message.Write(result);
                #endregion
            }

            public override void NetIncomingMessageToPacket(byte[] array)
            {
                #region NetIncomingMessageToPacket
                BitReader reader = new BitReader(array);

                ID = reader.ReadByte();
                int chunkLength = reader.ReadInt();
                FileChunk = reader.ReadByteArray(chunkLength);
                #endregion
            }
        }

        public class FileTransferComplete : Packet
        {
            public byte ID { get; set; }

            public override void PacketToNetOutGoingMessage(NetOutgoingMessage message)
            {
                #region PacketToNetOutGoingMessage
                message.Write((byte)PacketTypes.FileTransferComplete);

                List<byte> byteArray = new List<byte>();

                // The ID from the download
                byteArray.Add(ID);

                byte[] result = byteArray.ToArray();

                message.Write(result.Length);
                message.Write(result);
                #endregion
            }

            public override void NetIncomingMessageToPacket(byte[] array)
            {
                #region NetIncomingMessageToPacket
                BitReader reader = new BitReader(array);

                ID = reader.ReadByte();
                #endregion
            }
        }
    }
}
