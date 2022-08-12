using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace RageCoop.Core
{

    internal class ServerInfo
    {
        [JsonProperty("address")]
        public string Address { get; set; }

        [JsonProperty("port")]
        public string Port { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("players")]
        public string Players { get; set; }

        [JsonProperty("maxPlayers")]
        public string MaxPlayers { get; set; }

        [JsonProperty("country")]
        public string Country { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }
        
        [JsonProperty("website")]
        public string Website { get; set; }
        
        [JsonProperty("gameMode")]
        public string GameMode { get; set; }
        
        [JsonProperty("language")]
        public string Language { get; set; }


        [JsonProperty("useP2P")]
        public bool P2P { get; set; }

        [JsonProperty("useZT")]
        public bool ZeroTier { get; set; }

        [JsonProperty("ztID")]
        public string ZeroTierNetWorkID { get; set; }


        [JsonProperty("ztAddress")]
        public string ZeroTierAddress { get; set; }
    }
}
