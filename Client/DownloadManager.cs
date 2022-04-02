using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoopClient
{
    internal class DownloadManager
    {
        public int FileID { get; set; }
        public Packets.DataFileType FileType { get; set; }
        public string FileName { get; set; }
        public int FileLength { get; set; }
        public int Downloaded { get; set; } = 0;

        public DownloadManager(int id, Packets.DataFileType type, string name, int length)
        {
            FileID = id;
            FileType = type;
            FileName = name;
            FileLength = length;
        }

        public void DownloadPart(byte[] bytes)
        {

        }

        public void Finish()
        {

        }
    }
}
