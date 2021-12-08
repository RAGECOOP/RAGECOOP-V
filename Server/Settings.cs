namespace CoopServer
{
    public class Settings
    {
        public int ServerPort { get; set; } = 4499;
        public int MaxPlayers { get; set; } = 16;
        public string ServerName { get; set; } = "GTACoop:R server";
        public string WelcomeMessage { get; set; } = "Welcome on this server :)";
        public string Resource { get; set; } = "";
        public bool Allowlist { get; set; } = false;
        public bool NpcsAllowed { get; set; } = true;
        public bool ModsAllowed {  get; set; } = false;
        public bool UPnP { get; set; } = true;
        public bool AnnounceSelf { get; set; } = false;
        public string MasterServer { get; set; } = "http://gtacoopr.000webhostapp.com/servers.php";
        public bool DebugMode { get; set; } = false;
    }
}
