namespace CoopServer
{
    public class Settings
    {
        public int Port { get; set; } = 4499;
        public int MaxPlayers { get; set; } = 16;
        public string Name { get; set; } = "GTACoop:R server";
        public string WelcomeMessage { get; set; } = "Welcome on this server :)";
        public string Resource { get; set; } = "";
        public bool NpcsAllowed { get; set; } = true;
        public bool ModsAllowed {  get; set; } = false;
        public bool UPnP { get; set; } = true;
        public bool AnnounceSelf { get; set; } = false;
        public string MasterServer { get; set; } = "https://coop.entenkoeniq.de/servers";
        public bool DebugMode { get; set; } = false;
    }
}
