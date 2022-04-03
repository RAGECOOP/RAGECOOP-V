using System.IO;
using System.Linq;
using System.Collections.Generic;

using Lidgren.Network;

namespace CoopServer
{
    internal static class DownloadManager
    {
        private static readonly List<DownloadClient> _clients = new();
        private static readonly List<DownloadFile> _files = new();
        public static bool AnyFileExists = false;

        public static void InsertClient(long nethandle)
        {
            if (!AnyFileExists)
            {
                return;
            }

            _clients.Add(new DownloadClient(nethandle, new(_files)));
        }

        public static bool CheckForDirectoryAndFiles()
        {
            string[] filePaths;

            if (!Directory.Exists("clientside"))
            {
                return false;
            }

            filePaths = Directory.GetFiles("clientside");
            if (filePaths.Length == 0)
            {
                return false;
            }

            byte fileCount = 0;

            foreach (string file in filePaths)
            {
                FileInfo fileInfo = new(file);

                // ONLY JAVASCRIPT AND JSON FILES!
                if (!new string[] { ".js", ".json" }.Any(x => x == fileInfo.Extension))
                {
                    Logging.Warning("Only files with \"*.js\" and \"*.json\" can be sent!");
                    continue;
                }

                int MAX_BUFFER = fileInfo.Length < 5120 ? (int)fileInfo.Length : 5120; // 5KB
                byte[] buffer = new byte[MAX_BUFFER];
                bool fileCreated = false;
                DownloadFile newFile = null;

                using (FileStream fs = File.Open(file, FileMode.Open, FileAccess.Read))
                using (BufferedStream bs = new(fs))
                {
                    while (bs.Read(buffer, 0, MAX_BUFFER) != 0) // Reading 5KB chunks at time
                    {
                        if (!fileCreated && (fileCreated = true))
                        {
                            newFile = new() { FileID = fileCount, FileName = fileInfo.Name, FileLength = fileInfo.Length, FileData = new() };
                        }

                        newFile.FileData.Add(buffer);
                    }
                }

                _files.Add(newFile);
                fileCount++;
            }

            AnyFileExists = true;
            return true;
        }

        public static void Tick()
        {
            _clients.ForEach(client =>
            {
                if (!client.SendFiles())
                {
                    Client x = Server.Clients.FirstOrDefault(x => x.NetHandle == client.NetHandle);
                    if (x != null)
                    {
                        x.FilesReceived = true;
                    }
                }
            });
        }

        public static void RemoveClient(long nethandle)
        {
            DownloadClient client = _clients.FirstOrDefault(x => x.NetHandle == nethandle);
            if (client != null)
            {
                _clients.Remove(client);
            }
        }

        /// <summary>
        /// We try to remove the client when all files have been sent
        /// </summary>
        /// <param name="nethandle"></param>
        /// <param name="id"></param>
        public static void TryToRemoveClient(long nethandle, int id)
        {
            DownloadClient client = _clients.FirstOrDefault(x => x.NetHandle == nethandle);
            if (client == null)
            {
                return;
            }

            client.FilePosition++;

            if (client.DownloadComplete())
            {
                _clients.Remove(client);
            }
        }
    }

    internal class DownloadClient
    {
        public long NetHandle = 0;
        private readonly List<DownloadFile> _files = null;
        public int FilePosition = 0;
        private int _fileDataPosition = 0;

        public DownloadClient(long nethandle, List<DownloadFile> files)
        {
            NetHandle = nethandle;
            _files = files;

            NetConnection conn = Server.MainNetServer.Connections.FirstOrDefault(x => x.RemoteUniqueIdentifier == NetHandle);
            if (conn != null)
            {
                _files.ForEach(file =>
                {
                    NetOutgoingMessage outgoingMessage = Server.MainNetServer.CreateMessage();

                    new Packets.FileRequest()
                    {
                        ID = file.FileID,
                        FileType = (byte)Packets.DataFileType.Script,
                        FileName = file.FileName,
                        FileLength = file.FileLength
                    }.PacketToNetOutGoingMessage(outgoingMessage);

                    Server.MainNetServer.SendMessage(outgoingMessage, conn, NetDeliveryMethod.ReliableOrdered, (byte)ConnectionChannel.File);
                });
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>true if files should be sent otherwise false</returns>
        public bool SendFiles()
        {
            if (DownloadComplete())
            {
                return false;
            }

            DownloadFile file = _files[FilePosition];

            Send(NetHandle, file);

            if (_fileDataPosition >= file.FileData.Count)
            {
                FilePosition++;
                _fileDataPosition = 0;

                return DownloadComplete();
            }

            return true;
        }

        private void Send(long nethandle, DownloadFile file)
        {
            NetConnection conn = Server.MainNetServer.Connections.FirstOrDefault(x => x.RemoteUniqueIdentifier == nethandle);
            if (conn == null)
            {
                return;
            }

            NetOutgoingMessage outgoingMessage = Server.MainNetServer.CreateMessage();

            new Packets.FileTransferTick() { ID = file.FileID, FileChunk = file.FileData[_fileDataPosition++] }.PacketToNetOutGoingMessage(outgoingMessage);

            Server.MainNetServer.SendMessage(outgoingMessage, conn, NetDeliveryMethod.ReliableOrdered, (byte)ConnectionChannel.File);
        }

        public bool DownloadComplete()
        {
            return FilePosition >= _files.Count;
        }
    }

    internal class DownloadFile
    {
        public byte FileID { get; set; } = 0;
        public string FileName { get; set; } = string.Empty;
        public long FileLength { get; set; } = 0;

        public List<byte[]> FileData { get; set; } = null;
    }
}
