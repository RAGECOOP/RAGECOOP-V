using System;
using System.Collections.Generic;
using System.Text;

using Lidgren.Network;

namespace RageCoop.Core
{
    internal enum FileResponse:byte
    {
        NeedToDownload=0,
        AlreadyExists=1,
        Completed=2,
        Loaded=3,
        LoadFailed=4,
    }
    internal partial class Packets
    {
        internal class FileTransferRequest : Packet
        {
            public int ID { get; set; }

            public string Name { get; set; }

            public long FileLength { get; set; }

            public override void Pack(NetOutgoingMessage message)
            {
                #region PacketToNetOutGoingMessage
                message.Write((byte)PacketType.FileTransferRequest);

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

        internal class FileTransferResponse : Packet
        {
            public int ID { get; set; }
            public FileResponse Response { get; set; }
            public override void Pack(NetOutgoingMessage message)
            {
                message.Write((byte)PacketType.FileTransferResponse);

                List<byte> byteArray = new List<byte>();

                // The ID from the download
                byteArray.AddInt(ID);

                byteArray.Add((byte)Response);

                byte[] result = byteArray.ToArray();

                message.Write(result.Length);
                message.Write(result);
            }

            public override void Unpack(byte[] array)
            {
                BitReader reader = new BitReader(array);

                ID = reader.ReadInt();
                Response = (FileResponse)reader.ReadByte();
            }
        }

        internal class FileTransferChunk : Packet
        {
            public int ID { get; set; }

            public byte[] FileChunk { get; set; }

            public override void Pack(NetOutgoingMessage message)
            {
                #region PacketToNetOutGoingMessage
                message.Write((byte)PacketType.FileTransferChunk);

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

        internal class FileTransferComplete : Packet
        {
            public int ID { get; set; }

            public override void Pack(NetOutgoingMessage message)
            {
                #region PacketToNetOutGoingMessage
                message.Write((byte)PacketType.FileTransferComplete);

                List<byte> byteArray = new List<byte>();

                // The ID for the download
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
        internal class AllResourcesSent : Packet
        {

            public override void Pack(NetOutgoingMessage message)
            {
                #region PacketToNetOutGoingMessage
                message.Write((byte)PacketType.AllResourcesSent);
                message.Write(0);
                #endregion
            }

            public override void Unpack(byte[] array)
            {
                #region NetIncomingMessageToPacket
                #endregion
            }
        }
    }
}
