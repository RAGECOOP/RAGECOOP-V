namespace CoopServer.Entities
{
    public class EntitiesPlayer
    {
        public string SocialClubName { get; set; }
        public string Username { get; set; }
        public float Latency { get; set; }
        public EntitiesPed Ped = new();
    }
}
