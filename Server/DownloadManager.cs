using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace CoopServer
{
    internal static class DownloadManager
    {
        const int MAX_BUFFER = 1048576; // 1MB

        private static readonly List<DownloadClient> _clients = new();
        private static readonly List<DownloadFile> _files = new();
        public static bool AnyFileExists = false;

        public static void InsertClient(long nethandle)
        {
            if (!AnyFileExists)
            {
                return;
            }

            _clients.Add(new DownloadClient() { NetHandle = nethandle, FilesCount = _files.Count });
        }

        public static bool CheckForDirectoryAndFiles()
        {
            string[] filePaths = Directory.GetFiles("clientside");

            if (!Directory.Exists("clientside") || filePaths.Length == 0)
            {
                AnyFileExists = false;
                return false;
            }

            foreach (string file in filePaths)
            {
                FileInfo fileInfo = new(file);

                // ONLY JAVASCRIPT AND JSON FILES!
                if (!new string[] { ".js", ".json" }.Any(x => x == fileInfo.Extension))
                {
                    Logging.Warning("Only files with \"*.js\" and \"*.json\" can be sent!");
                    continue;
                }

                Logging.Debug($"===== {fileInfo.Name} =====");

                byte[] buffer = new byte[MAX_BUFFER];
                int bytesRead = 0;
                bool fileCreated = false;
                DownloadFile newFile = null;
                byte fileCount = 0;

                using (FileStream fs = File.Open(file, FileMode.Open, FileAccess.Read))
                using (BufferedStream bs = new(fs))
                {
                    while ((bytesRead = bs.Read(buffer, 0, MAX_BUFFER)) != 0) // Reading 1MB chunks at time
                    {
                        if (!fileCreated)
                        {
                            newFile = new() { FileID = fileCount, FileName = fileInfo.Name, FileLength = fileInfo.Length };
                            fileCreated = true;
                        }

                        newFile.AddData(buffer);

                        Logging.Debug($"{bytesRead}");
                    }
                }

                _files.Add(newFile);
            }

            Logging.Info($"{_files.Count} files found!");

            AnyFileExists = true;
            return true;
        }

        public static void Tick()
        {
            _clients.ForEach(client =>
            {
                if (!client.SendFiles(_files))
                {
                    Client x = Server.Clients.FirstOrDefault(x => x.NetHandle == client.NetHandle);
                    if (x != null)
                    {
                        x.FilesSent = true;
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
    }

    internal class DownloadClient
    {
        public long NetHandle { get; set; }
        public int FilesCount { get; set; }
        public int FilesSent = 0;

        /// <summary>
        /// 
        /// </summary>
        /// <returns>true if files should be sent otherwise false</returns>
        public bool SendFiles(List<DownloadFile> files)
        {
            if (FilesSent >= FilesCount)
            {
                return false;
            }

            DownloadFile file = files.FirstOrDefault(x => !x.DownloadFinished());
            if (file == null)
            {
                return false;
            }

            file.Send(NetHandle);

            // Check it again, maybe this file is finish now
            if (file.DownloadFinished())
            {
                FilesSent++;

                if (FilesSent >= FilesCount)
                {
                    return false;
                }
            }

            return true;
        }
    }

    internal class DownloadFile
    {
        public int FileID { get; set; }
        public string FileName { get; set; }
        public long FileLength { get; set; }

        private readonly List<byte[]> _data = new();
        private readonly long _sent = 0;

        public void Send(long nethandle)
        {
            // TODO
        }

        public void AddData(byte[] data)
        {
            _data.Add(data);
        }

        public bool DownloadFinished()
        {
            return _sent >= FileLength;
        }
    }
}
