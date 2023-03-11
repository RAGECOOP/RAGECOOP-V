using Newtonsoft.Json;
using RageCoop.Core.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RageCoop.Client.Scripting
{
    public class ClientFile : ResourceFile
    {
        public ClientFile() {
            GetStream = GetStreamMethod;
        }

        [JsonProperty]
        public string FullPath { get; internal set; }
        Stream GetStreamMethod()
        {
            if (IsDirectory)
            {
                return File.Open(FullPath, FileMode.Open);
            }
            throw new InvalidOperationException("Cannot open directory as file");
        }
    }
}
