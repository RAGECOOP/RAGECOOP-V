namespace RageCoop.Server
{
    /// <summary>
    /// Settings for RageCoop Server
    /// </summary>
    public class ServerSettings
    {
        /// <summary>
        /// Port to listen for incoming connections
        /// </summary>
        public int Port { get; set; } = 4499;
        /// <summary>
        /// Maximum number of players on this server
        /// </summary>
        public int MaxPlayers { get; set; } = 32;
        /// <summary>
        /// Maximum latency allowed for a client, a client will be kicked if it's latency it's higher than this value
        /// </summary>
        public int MaxLatency { get; set; } = 500;
        /// <summary>
        /// The server name to be shown on master server
        /// </summary>
        public string Name { get; set; } = "RAGECOOP server";
        /// <summary>
        /// The message to send when a client connected (not visible to others)
        /// </summary>
        public string WelcomeMessage { get; set; } = "Welcome on this server :)";
        // public bool HolePunch { get; set; } = true;
        /// <summary>
        /// Whether or not to announce this server so it'll appear on server list.
        /// </summary>
        public bool AnnounceSelf { get; set; } = false;
        /// <summary>
        /// Master server address, mostly doesn't need to be changed.
        /// </summary>
        public string MasterServer { get; set; } = "[AUTO]";

        /// <summary>
        /// See <see cref="Core.Logger.LogLevel"/>.
        /// </summary>
        public int LogLevel { get; set; }=2;
        /// <summary>
        /// NPC data won't be sent to a player if their distance is greater than this value. -1 for unlimited.
        /// </summary>
        public float NpcStreamingDistance { get; set; } = 500;
        /// <summary>
        /// Player's data won't be sent to another player if their distance is greater than this value. -1 for unlimited.
        /// </summary>
        public float PlayerStreamingDistance { get; set; } = -1;
    }
}
