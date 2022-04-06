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
                _streams.Add(id, new FileStream($"{downloadFolder}\\{name}", FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite));
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

        public static void Write(byte id, byte[] data)
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
                FileStream fs = _streams.ContainsKey(id) ? _streams[id] : null;
                if (fs == null)
                {
                    throw new System.Exception($"Stream for file {id} doesn't found!");
                }

                fs.Write(data, 0, data.Length);

                lock (_downloadFiles)
                {
                    DownloadFile file = _downloadFiles.FirstOrDefault(x => x.FileID == id);
                    if (file == null)
                    {
                        throw new System.Exception($"File {id} couldn't ne found in list!");
                    }

                    if (data.Length >= file.FileLength)
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

        public static void Cleanup()
        {
            lock (_streams) lock (_downloadFiles) lock (_filesFinished)
            {
                foreach (var stream in _streams)
                {
                    stream.Value.Close();
                    stream.Value.Dispose();
                }
                _streams.Clear();
                _downloadFiles.Clear();
                _filesFinished.Clear();
            }
        }
    }

    internal class DownloadFile
    {
        public byte FileID { get; set; } = 0;
        public Packets.DataFileType FileType { get; set; } = Packets.DataFileType.Script;
        public string FileName { get; set; } = string.Empty;
        public long FileLength { get; set; } = 0;
    }
}
