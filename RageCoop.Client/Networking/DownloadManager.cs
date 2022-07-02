using System.IO;
using System.Linq;
using System.Collections.Generic;
using RageCoop.Core;
using System;

namespace RageCoop.Client
{
    internal static class DownloadManager
    {
        static DownloadManager()
        {
            Networking.RequestHandlers.Add(PacketType.FileTransferRequest, (data) =>
            {
                var fr = new Packets.FileTransferRequest();
                fr.Unpack(data);
                return new Packets.FileTransferResponse()
                {
                    ID= fr.ID,
                    Response=AddFile(fr.ID,fr.Name,fr.FileLength) ? FileResponse.NeedToDownload : FileResponse.AlreadyExists
                };
            });
            Networking.RequestHandlers.Add(PacketType.FileTransferComplete, (data) =>
            {
                Packets.FileTransferComplete packet = new Packets.FileTransferComplete();
                packet.Unpack(data);

                Main.Logger.Debug($"Finalizing download:{packet.ID}");
                Complete(packet.ID);

                // Inform the server that the download is completed
                return new Packets.FileTransferResponse()
                {
                    ID= packet.ID,
                    Response=FileResponse.Completed
                };
            });
            Networking.RequestHandlers.Add(PacketType.AllResourcesSent, (data) =>
            {
                try
                {
                    Main.Resources.Load(ResourceFolder);
                    return new Packets.FileTransferResponse() { ID=0, Response=FileResponse.Loaded };
                }
                catch(Exception ex)
                {

                    Main.Logger.Error("Error occurred when loading server resource:");
                    Main.Logger.Error(ex);
                    return new Packets.FileTransferResponse() { ID=0, Response=FileResponse.LoadFailed };
                }
            });
        }
        public static string ResourceFolder { 
            get {
                return Path.Combine(Main.Settings.DataDirectory,"Resources", Main.Settings.LastServerAddress.Replace(":", "."));
            }
        } 
        private static readonly Dictionary<int, DownloadFile> InProgressDownloads = new Dictionary<int, DownloadFile>();
        public static bool AddFile(int id, string name, long length)
        {
            Main.Logger.Debug($"Downloading file to {ResourceFolder}\\{name} , id:{id}");
            if (!Directory.Exists(ResourceFolder))
            {
                Directory.CreateDirectory(ResourceFolder);
            }

            if (FileAlreadyExists(ResourceFolder, name, length))
            {
                Main.Logger.Debug($"File already exists! canceling download:{name}");
                return false;
            }
            
            if (!name.EndsWith(".zip"))
            {
                Main.Logger.Error($"File download blocked! [{name}]");
                return false;
            }
            lock (InProgressDownloads)
            {
                InProgressDownloads.Add(id, new DownloadFile()
                {
                    FileID = id,
                    FileName = name,
                    FileLength = length,
                    Stream = new FileStream($"{ResourceFolder}\\{name}", FileMode.CreateNew, FileAccess.Write, FileShare.ReadWrite)
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
                DownloadFile file;
                if (InProgressDownloads.TryGetValue(id, out file))
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
            DownloadFile f;

            if (InProgressDownloads.TryGetValue(id, out f))
            {
                InProgressDownloads.Remove(id);
                f.Dispose();
                Main.Logger.Info($"Download finished:{f.FileName}");
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
            foreach (var zip in Directory.GetDirectories(ResourceFolder, "*.zip"))
            {
                File.Delete(zip);
            }

        }
    }

    internal class DownloadFile: System.IDisposable
    {
        public int FileID { get; set; } = 0;
        public string FileName { get; set; } = string.Empty;
        public long FileLength { get; set; } = 0;
        public long FileWritten { get; set; } = 0;
        public FileStream Stream { get; set; }
        public void Dispose()
        {
            if(Stream!= null)
            {
                Stream.Flush();
                Stream.Close();
                Stream.Dispose();
            }
        }
    }
}
