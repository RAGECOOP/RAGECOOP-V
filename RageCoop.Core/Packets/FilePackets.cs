
using Lidgren.Network;

namespace RageCoop.Core
{
    internal enum FileResponse : byte
    {
        NeedToDownload = 0,
        AlreadyExists = 1,
        Completed = 2,
        Loaded = 3,
        LoadFailed = 4,
    }
    internal partial class Packets
    {
        internal class FileTransferRequest : Packet
        {
            public override PacketType Type => PacketType.FileTransferRequest;
            public int ID { get; set; }

            public string Name { get; set; }

            public long FileLength { get; set; }

            protected override void Serialize(NetOutgoingMessage m)
            {



                // The ID from the download
                m.Write(ID);


                // The name of the file
                m.Write(Name);

                // The length of the file
                m.Write(FileLength);

            }

            public override void Deserialize(NetIncomingMessage m)
            {


                ID = m.ReadInt32();
                Name = m.ReadString();
                FileLength = m.ReadInt64();
            }
        }

        internal class FileTransferResponse : Packet
        {
            public override PacketType Type => PacketType.FileTransferResponse;
            public int ID { get; set; }
            public FileResponse Response { get; set; }
            protected override void Serialize(NetOutgoingMessage m)
            {

                // The ID from the download
                m.Write(ID);

                m.Write((byte)Response);

            }

            public override void Deserialize(NetIncomingMessage m)
            {

                ID = m.ReadInt32();
                Response = (FileResponse)m.ReadByte();
            }
        }

        internal class FileTransferChunk : Packet
        {
            public override PacketType Type => PacketType.FileTransferChunk;
            public int ID { get; set; }

            public byte[] FileChunk { get; set; }

            protected override void Serialize(NetOutgoingMessage m)
            {


                // The ID from the download
                m.Write(ID);
                m.WriteByteArray(FileChunk);

            }

            public override void Deserialize(NetIncomingMessage m)
            {

                ID = m.ReadInt32();
                FileChunk = m.ReadByteArray();
            }
        }

        internal class FileTransferComplete : Packet
        {
            public override PacketType Type => PacketType.FileTransferComplete;
            public int ID { get; set; }

            protected override void Serialize(NetOutgoingMessage m)
            {


                // The ID for the download
                m.Write(ID);

            }

            public override void Deserialize(NetIncomingMessage m)
            {


                ID = m.ReadInt32();
            }
        }
        internal class AllResourcesSent : Packet
        {

            public override PacketType Type => PacketType.AllResourcesSent;
        }
    }
}
