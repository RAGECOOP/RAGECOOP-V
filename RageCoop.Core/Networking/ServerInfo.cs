namespace RageCoop.Core
{
    /// <summary>
    /// A json object representing a server's information as annouced to master server.
    /// </summary>
    public class ServerInfo
    {
#pragma warning disable 1591
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
        public string publicKeyModulus { get; set; }
        public string publicKeyExponent { get; set; }

        /// <summary>
        /// Optional override for the address shown in the server list (e.g. a No-IP hostname).
        /// If set, this is used instead of the real public IP detected by the master server.
        /// </summary>
        public string announcedAddress { get; set; }

    }
}
