using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RageCoop.Server
{
    internal class FileTransfer
    {
        public int ID { get; set; }
        public float Progress { get; set; }
        public string Name { get; set; }
        public bool Cancel { get; set; }=false;
    }
}
