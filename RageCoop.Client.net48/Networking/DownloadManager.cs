using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace RageCoop.Client
{
    internal static class DownloadManager
    {
        static string downloadFolder = $"RageCoop\\Resources\\{Main.Settings.LastServerAddress.Replace(":", ".")}";

        private static readonly Dictionary<int, DownloadFile> InProgressDownloads = new Dictionary<int, DownloadFile>();
        public static void AddFile(int id, string name, long length)
        {
            Main.Logger.Debug($"Downloading file to {downloadFolder}\\{name} , id:{id}");
            if (!Directory.Exists(downloadFolder))
            {
                Directory.CreateDirectory(downloadFolder);
            }

            if (FileAlreadyExists(downloadFolder, name, length))
            {
                Main.Logger.Debug($"File already exists! canceling download:{name}");
                Cancel(id); 
                if (name=="Resources.zip")
                {
                    Main.Logger.Debug("Loading resources...");
                    Resources.Load(Path.Combine(downloadFolder));
                }
                return;
            }
            
            if (!name.EndsWith(".zip"))
            {
                Cancel(id);

                GTA.UI.Notification.Show($"The download of a file from the server was blocked! [{name}]", true);
                Main.Logger.Error($"The download of a file from the server was blocked! [{name}]");
                return;
            }
            lock (InProgressDownloads)
            {
                InProgressDownloads.Add(id, new DownloadFile()
                {
                    FileID = id,
                    FileName = name,
                    FileLength = length,
                    Stream = new FileStream($"{downloadFolder}\\{name}", FileMode.CreateNew, FileAccess.Write, FileShare.ReadWrite)
                });
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
                    return;
                }
            }

        }

        public static void Cancel(int id)
        {
            Main.Logger.Debug($"Canceling download:{id}");

            // Tell the server to stop sending chunks
            Networking.SendDownloadFinish(id);

            DownloadFile file;
            lock (InProgressDownloads)
            {
                if (InProgressDownloads.TryGetValue(id, out file))
                {
                    InProgressDownloads.Remove(id);
                    file.Dispose();
                }
            }
        }
        public static void Complete(int id)
        {
            DownloadFile f;

            if (InProgressDownloads.TryGetValue(id, out f))
            {
                lock (InProgressDownloads)
                {
                    InProgressDownloads.Remove(id);
                    f.Dispose();
                    Main.Logger.Info($"Download finished:{f.FileName}");
                    if (f.FileName=="Resources.zip")
                    {
                        Main.Logger.Debug("Loading resources...");
                        Resources.Load(Path.Combine(downloadFolder));
                    }
                    Networking.SendDownloadFinish(id);
                }
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

        }
    }

    public class DownloadFile: System.IDisposable
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
