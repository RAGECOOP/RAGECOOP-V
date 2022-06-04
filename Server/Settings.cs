namespace RageCoop.Server
{
    public class Settings
    {
        public int Port { get; set; } = 4499;
        public int MaxPlayers { get; set; } = 32;
        public int MaxLatency { get; set; } = 500;
        public string Name { get; set; } = "RAGECOOP server";
        public string WelcomeMessage { get; set; } = "Welcome on this server :)";
        public bool UPnP { get; set; } = true;
        public bool AnnounceSelf { get; set; } = false;
        public string MasterServer { get; set; } = "[AUTO]";
        public bool DebugMode { get; set; } = false;
        /// <summary>
        /// NPC data won't be sent to a player if their distance is greater than this value. -1 for unlimited.
        /// </summary>
        public float NpcStreamingDistance { get; set; } = 1000;
        /// <summary>
        /// Player's data won't be sent to another player if their distance is greater than this value. -1 for unlimited.
        /// </summary>
        public float PlayerStreamingDistance { get; set; } = -1;
    }
}
