using RageCoop.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RageCoop.Client
{
    internal static class DownloadManager
    {
        public static event EventHandler<string> DownloadCompleted;
        static DownloadManager()
        {
            Networking.RequestHandlers.Add(PacketType.FileTransferRequest, (data) =>
            {
                var fr = new Packets.FileTransferRequest();
                fr.Deserialize(data);
                if (fr.Name.EndsWith(".res"))
                {
                    _resources.Add(fr.Name);
                }
                return new Packets.FileTransferResponse()
                {
                    ID = fr.ID,
                    Response = AddFile(fr.ID, fr.Name, fr.FileLength) ? FileResponse.NeedToDownload : FileResponse.AlreadyExists
                };
            });
            Networking.RequestHandlers.Add(PacketType.FileTransferComplete, (data) =>
            {
                Packets.FileTransferComplete packet = new Packets.FileTransferComplete();
                packet.Deserialize(data);

                Main.Logger.Debug($"Finalizing download:{packet.ID}");
                Complete(packet.ID);

                // Inform the server that the download is completed
                return new Packets.FileTransferResponse()
                {
                    ID = packet.ID,
                    Response = FileResponse.Completed
                };
            });
            Networking.RequestHandlers.Add(PacketType.AllResourcesSent, (data) =>
            {
                try
                {
                    Main.Resources.Load(ResourceFolder, _resources.ToArray());
                    return new Packets.FileTransferResponse() { ID = 0, Response = FileResponse.Loaded };
                }
                catch (Exception ex)
                {
                    Main.Logger.Error("Error occurred when loading server resource:");
                    Main.Logger.Error(ex);
                    return new Packets.FileTransferResponse() { ID = 0, Response = FileResponse.LoadFailed };
                }
            });
        }
        public static string ResourceFolder => Path.GetFullPath(Path.Combine(Main.Settings.DataDirectory, "Resources", Main.Settings.LastServerAddress.Replace(":", ".")));
        private static readonly Dictionary<int, DownloadFile> InProgressDownloads = new Dictionary<int, DownloadFile>();
        private static readonly HashSet<string> _resources = new HashSet<string>();
        public static bool AddFile(int id, string name, long length)
        {
            var path = $"{ResourceFolder}\\{name}";
            Main.Logger.Debug($"Downloading file to {path} , id:{id}");
            if (!Directory.Exists(Directory.GetParent(path).FullName))
            {
                Directory.CreateDirectory(Directory.GetParent(path).FullName);
            }

            if (FileAlreadyExists(ResourceFolder, name, length))
            {
                Main.Logger.Debug($"File already exists! canceling download:{name}");
                DownloadCompleted?.Invoke(null, Path.Combine(ResourceFolder, name));
                return false;
            }
            /*
            if (!name.EndsWith(".zip"))
            {
                Main.Logger.Error($"File download blocked! [{name}]");
                return false;
            }
            */
            lock (InProgressDownloads)
            {
                InProgressDownloads.Add(id, new DownloadFile()
                {
                    FileID = id,
                    FileName = name,
                    FileLength = length,
                    Stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.ReadWrite)
                });
            }
            return true;
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

        public static void Write(int id, byte[] chunk)
        {
            lock (InProgressDownloads)
            {
                if (InProgressDownloads.TryGetValue(id, out DownloadFile file))
                {

                    file.Stream.Write(chunk, 0, chunk.Length);
                }
                else
                {
                    Main.Logger.Trace($"Received unhandled file chunk:{id}");
                }
            }
        }

        public static void Complete(int id)
        {

            if (InProgressDownloads.TryGetValue(id, out DownloadFile f))
            {
                InProgressDownloads.Remove(id);
                f.Dispose();
                Main.Logger.Info($"Download finished:{f.FileName}");
                DownloadCompleted?.Invoke(null, Path.Combine(ResourceFolder, f.FileName));
            }
            else
            {
                Main.Logger.Error($"Download not found! {id}");
            }
        }

        public static void Cleanup()
        {
            lock (InProgressDownloads)
            {
                foreach (var file in InProgressDownloads.Values)
                {
                    file.Dispose();
                }
                InProgressDownloads.Clear();
            }
            _resources.Clear();

        }
    }

    internal class DownloadFile : IDisposable
    {
        public int FileID { get; set; } = 0;
        public string FileName { get; set; } = string.Empty;
        public long FileLength { get; set; } = 0;
        public long FileWritten { get; set; } = 0;
        public FileStream Stream { get; set; }
        public void Dispose()
        {
            if (Stream != null)
            {
                Stream.Flush();
                Stream.Close();
                Stream.Dispose();
            }
        }
    }
}
