using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace RageCoop.Core
{

    internal class ServerInfo
    {
        public string address { get; set; }
        public string port { get; set; }
        public string name { get; set; }
        public string version { get; set; }
        public string players { get; set; }
        public string maxPlayers { get; set; }
        public string country { get; set; }
        public string description { get; set; }
        public string website { get; set; }
        public string gameMode { get; set; }
        public string language { get; set; }

        public bool useP2P { get; set; }

        public bool useZT { get; set; }

        public string ztID { get; set; }

        public string ztAddress { get; set; }
    }
}
