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
        public int Port { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("players")]
        public int Players { get; set; }

        [JsonProperty("maxPlayers")]
        public int MaxPlayers { get; set; }

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
    }
}
