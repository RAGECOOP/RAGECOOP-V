namespace CoopServer
{
    public class Settings
    {
        public int ServerPort { get; set; } = 4499;
        public int MaxPlayers { get; set; } = 16;
        public string ServerName { get; set; } = "GTACoop:R server";
        public string WelcomeMessage { get; set; } = "Welcome on this server :)";
        public string GameMode { get; set; } = "";
        public bool Allowlist { get; set; } = false;
        public bool NpcsAllowed { get; set; } = true;
        public bool UPnP { get; set; } = true;
        public bool DebugMode { get; set; } = false;
    }
}
