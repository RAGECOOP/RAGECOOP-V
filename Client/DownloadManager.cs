using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace CoopClient
{
    internal static class DownloadManager
    {
        private static readonly List<DownloadFile> _downloadFiles = new List<DownloadFile>();
        private static readonly Dictionary<byte, FileStream> _streams = new Dictionary<byte, FileStream>();
        private static readonly List<byte> _filesFinished = new List<byte>();
        public static bool DownloadComplete = false;

        public static void AddFile(byte id, Packets.DataFileType type, string name, long length)
        {
            string downloadFolder = $"scripts\\resources\\{Main.MainSettings.LastServerAddress.Replace(":", ".")}";

            if (!Directory.Exists(downloadFolder))
            {
                Directory.CreateDirectory(downloadFolder);
            }

            if (FileAlreadyExists(downloadFolder, name, length))
            {
                // Send the server we are already done
                Main.MainNetworking.SendDownloadFinish(id);

                lock (_filesFinished)
                {
                    _filesFinished.Add(id);
                }
                return;
            }

            lock (_downloadFiles)
            {
                _downloadFiles.Add(new DownloadFile()
                {
                    FileID = id,
                    FileType = type,
                    FileName = name,
                    FileLength = length
                });
            }

            lock (_streams)
            {
                _streams.Add(id, new FileStream($"{downloadFolder}\\{name}", FileMode.CreateNew, FileAccess.Write, FileShare.ReadWrite));
            }
        }

        /// <summary>
        /// Check if the file already exists and if the size correct otherwise delete this file
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="name"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        private static bool FileAlreadyExists(string folder, string name, long length)
        {
            string filePath = $"{folder}\\{name}";

            if (File.Exists(filePath))
            {
                if (new FileInfo(filePath).Length == length)
                {
                    return true;
                }

                // Delete the file because the length is wrong (maybe the file was updated)
                File.Delete(filePath);
            }

            return false;
        }

        public static void Write(byte id, byte[] chunk)
        {
            lock (_filesFinished)
            {
                if (_filesFinished.Contains(id))
                {
                    Cancel(id);
                    return;
                }
            }

            lock (_streams)
            {
                FileStream fs = _streams.FirstOrDefault(x => x.Key == id).Value;
                if (fs == null)
                {
                    Logger.Write($"Stream for file {id} not found!", Logger.LogLevel.Server);
                    return;
                }

                fs.Write(chunk, 0, chunk.Length);

                lock (_downloadFiles)
                {
                    DownloadFile file = _downloadFiles.FirstOrDefault(x => x.FileID == id);
                    if (file == null)
                    {
                        Logger.Write($"File {id} couldn't be found in the list!", Logger.LogLevel.Server);
                        return;
                    }

                    file.FileWritten += chunk.Length;

                    if (file.FileWritten >= file.FileLength)
                    {
                        Cancel(id);
                    }
                }
            }
        }

        public static void Cancel(byte id)
        {
            lock (_streams) lock (_downloadFiles)
            {
                FileStream fs = _streams.ContainsKey(id) ? _streams[id] : null;
                if (fs != null)
                {
                    fs.Close();
                    fs.Dispose();
                
                    _streams.Remove(id);
                }

                if (_downloadFiles.Any(x => x.FileID == id))
                {
                    _downloadFiles.Remove(_downloadFiles.First(x => x.FileID == id));
                }
            }
        }

        public static void Cleanup(bool everything)
        {
            lock (_streams) lock (_downloadFiles) lock (_filesFinished)
            {
                foreach (KeyValuePair<byte, FileStream> stream in _streams)
                {
                    stream.Value.Close();
                    stream.Value.Dispose();
                }
                _streams.Clear();
                _downloadFiles.Clear();
                _filesFinished.Clear();
            }

            if (everything)
            {
                DownloadComplete = false;
            }
        }
    }

    internal class DownloadFile
    {
        public byte FileID { get; set; } = 0;
        public Packets.DataFileType FileType { get; set; } = Packets.DataFileType.Script;
        public string FileName { get; set; } = string.Empty;
        public long FileLength { get; set; } = 0;
        public long FileWritten { get; set; } = 0;
    }
}
