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
            public override PacketType Type { get { return PacketType.FileTransferRequest; } }
            public int ID { get; set; }

            public string Name { get; set; }

            public long FileLength { get; set; }

            public override byte[] Serialize()
            {

                List<byte> byteArray = new List<byte>();

                // The ID from the download
                byteArray.AddInt(ID);


                // The name of the file
                byte[] nameBytes = Encoding.UTF8.GetBytes(Name);
                byteArray.AddRange(BitConverter.GetBytes(nameBytes.Length));
                byteArray.AddRange(nameBytes);

                // The length of the file
                byteArray.AddRange(BitConverter.GetBytes(FileLength));

                return byteArray.ToArray();

            }

            public override void Deserialize(byte[] array)
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
            public override PacketType Type { get { return PacketType.FileTransferResponse; } }
            public int ID { get; set; }
            public FileResponse Response { get; set; }
            public override byte[] Serialize()
            {

                List<byte> byteArray = new List<byte>();

                // The ID from the download
                byteArray.AddInt(ID);

                byteArray.Add((byte)Response);

                return byteArray.ToArray();
            }

            public override void Deserialize(byte[] array)
            {
                BitReader reader = new BitReader(array);

                ID = reader.ReadInt();
                Response = (FileResponse)reader.ReadByte();
            }
        }

        internal class FileTransferChunk : Packet
        {
            public override PacketType Type { get { return PacketType.FileTransferChunk; } }
            public int ID { get; set; }

            public byte[] FileChunk { get; set; }

            public override byte[] Serialize()
            {
                List<byte> byteArray = new List<byte>();

                // The ID from the download
                byteArray.AddInt(ID);

                // The chunk of the file
                byteArray.AddInt(FileChunk.Length);
                byteArray.AddRange(FileChunk);

                return byteArray.ToArray();

            }

            public override void Deserialize(byte[] array)
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
            public override PacketType Type { get { return PacketType.FileTransferComplete; } }
            public int ID { get; set; }

            public override byte[] Serialize()
            {
                List<byte> byteArray = new List<byte>();

                // The ID for the download
                byteArray.AddInt(ID);

                return byteArray.ToArray();

            }

            public override void Deserialize(byte[] array)
            {
                #region NetIncomingMessageToPacket
                BitReader reader = new BitReader(array);

                ID = reader.ReadInt();
                #endregion
            }
        }
        internal class AllResourcesSent : Packet
        {

            public override PacketType Type { get { return PacketType.AllResourcesSent; } }
            public override byte[] Serialize()
            {
                return new byte[0];
            }

            public override void Deserialize(byte[] array)
            {
            }
        }
    }
}
