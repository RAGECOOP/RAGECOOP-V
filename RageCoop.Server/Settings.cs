namespace RageCoop.Server
{
    /// <summary>
    /// Settings for RageCoop Server
    /// </summary>
    public class Settings
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
        /// The website address to be shown on master server
        /// </summary>
        public string Website { get; set; } = "https://ragecoop.com/";

        /// <summary>
        /// The description to be shown on master server
        /// </summary>
        public string Description { get; set; } = "RAGECOOP server";

        /// <summary>
        /// The game mode to be shown on master server
        /// </summary>
        public string GameMode { get; set; } = "FreeRoam";

        /// <summary>
        /// The language to be shown on master server
        /// </summary>
        public string Language { get; set; } = "English";

        /// <summary>
        /// The message to send when a client connected (not visible to others)
        /// </summary>
        public string WelcomeMessage { get; set; } = "Welcome on this server :)";

        /// <summary>
        /// Whether or not to announce this server so it'll appear on server list.
        /// </summary>
        public bool AnnounceSelf { get; set; } = false;

        /// <summary>
        /// Master server address, mostly doesn't need to be changed.
        /// </summary>
        public string MasterServer { get; set; } = "https://masterserver.ragecoop.com/";

        /// <summary>
        /// See <see cref="Core.Logger.LogLevel"/>.
        /// </summary>
        public int LogLevel { get; set; } = 0;

        /// <summary>
        /// NPC data won't be sent to a player if their distance is greater than this value. -1 for unlimited.
        /// </summary>
        public float NpcStreamingDistance { get; set; } = 500;

        /// <summary>
        /// Player's data won't be sent to another player if their distance is greater than this value. -1 for unlimited.
        /// </summary>
        public float PlayerStreamingDistance { get; set; } = -1;

        /// <summary>
        /// If enabled, all clients will have same weather and time as host
        /// </summary>
        public bool WeatherTimeSync { get; set; } = true;

        /// <summary>
        /// List of all allowed username characters
        /// </summary>
        public string AllowedUsernameChars { get; set; } = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz1234567890-_";

        /// <summary>
        /// Whether to use direct connection between players to send entity information, <see cref="UseZeroTier"/> needs to be enabled if on WAN  for this feature to function properly.
        /// </summary>
        public bool UseP2P { get; set; } = false;

        /// <summary>
        /// Whether to enable zerotier VLAN functionality, allowing you to host a server behind NAT firewall, no port forward required.
        /// </summary>
        public bool UseZeroTier { get; set; } = false;

        /// <summary>
        /// Use in-game voice chat to communicate with other players
        /// </summary>
        public bool UseVoice { get; set; } = false;

        /// <summary>
        /// The zerotier network id to join, default value is zerotier's public Earth network.
        /// </summary>
        public string ZeroTierNetworkID { get; set; } = "8056c2e21c000001";

        /// <summary>
        /// Automatically update to nightly build when an update is avalible, check is performed every 10 minutes.
        /// </summary>
        public bool AutoUpdate { get; set; } = false;

        /// <summary>
        /// Kick godmode assholes
        /// </summary>
        public bool KickGodMode { get; set; } = false;

        /// <summary>
        /// Kick spamming assholes
        /// </summary>
        public bool KickSpamming { get; set; } = true;

        /// <summary>
        /// Player that spawned entities more than this amount will be kicked if <see cref="KickSpamming"/> is enabled.
        /// </summary>
        public int SpamLimit { get; set; } = 100;
    }
}
