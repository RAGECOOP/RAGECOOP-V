using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace CoopServer
{
    internal class DownloadManager
    {
        const int MAX_BUFFER = 1048576; // 1MB

        // Key = Nethandle
        // Value = List of Files
        private Dictionary<long, List<DownloadFile>> _files = new();

        public void Create(long nethandle)
        {
            if (!DirectoryAndFilesExists())
            {
                return;
            }

            List<DownloadFile> files = new();

            foreach (string file in Directory.GetFiles("clientside"))
            {
                FileInfo fileInfo = new(file);

                // ONLY JAVASCRIPT AND JSON FILES!
                if (!new string[] { ".js", ".json" }.Any(x => x == fileInfo.Extension))
                {
                    Logging.Error("Only files with \"*.js\" and \"*.json\" can be sent!");
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

                files.Add(newFile);
            }

            _files.Add(nethandle, files);
        }

        private bool DirectoryAndFilesExists()
        {
            if (!Directory.Exists("clientside") || Directory.GetFiles("clientside").Length == 0)
            {
                return false;
            }

            return true;
        }
    }

    internal class DownloadFile
    {
        public int FileID { get; set; }
        public string FileName { get; set; }
        public long FileLength { get; set; }

        private List<byte[]> _data = new();
        private long _sent = 0;

        public void Upload()
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
