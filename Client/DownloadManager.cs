using System.IO;

namespace CoopClient
{
    internal class DownloadManager
    {
        public byte FileID { get; set; }
        public Packets.DataFileType FileType { get; set; }
        public string FileName { get; set; }
        public long FileLength { get; set; }

        private readonly FileStream _stream;

        public DownloadManager()
        {
            string downloadFolder = $"scripts\\{Main.MainSettings.LastServerAddress.Replace(":", ".")}";
            if (!Directory.Exists(downloadFolder))
            {
                Directory.CreateDirectory(downloadFolder);
                if (FileAlreadyExists(downloadFolder))
                {
                    // Send the server we are already done
                    Main.MainNetworking.SendDownloadFinish(FileID);
                    return;
                }
            }

            _stream = new FileStream(downloadFolder + "\\" + FileName, FileMode.CreateNew);
        }

        /// <summary>
        /// Check if the file already exists and if the size correct otherwise delete this file
        /// </summary>
        /// <param name="folder"></param>
        private bool FileAlreadyExists(string folder)
        {
            string filePath = $"{folder}\\{FileName}";
            if (File.Exists(filePath))
            {
                if (new FileInfo(filePath).Length == FileLength)
                {
                    return true;
                }
                else
                {
                    // Delete the file because the length is wrong (maybe the file was updated)
                    File.Delete(filePath);
                }
            }

            return false;
        }

        public void DownloadPart(byte[] data)
        {
            _stream.Write(data, 0, data.Length);
            if (data.Length >= FileLength)
            {
                _stream.Close();
                _stream.Dispose();
            }
        }
    }
}
