namespace CoopServer.Entities
{
    public class EntitiesPlayer
    {
        public string SocialClubName { get; set; }
        public string Username { get; set; }
        public float Latency { get; set; }
        private LVector3 LastPosition = new();
        private LVector3 CurrentPosition = new();
        public LVector3 Position
        {
            get
            {
                return CurrentPosition;
            }

            set
            {
                LastPosition = CurrentPosition;
                CurrentPosition = value;

                if (Server.GameMode != null && !LVector3.Equals(CurrentPosition, LastPosition))
                {
                    Server.GameMode.API.InvokePlayerPositionUpdate(this);
                }
            }
        }

        public bool IsInRangeOf(LVector3 position, float distance)
        {
            return LVector3.Subtract(Position, position).Length() < distance;
        }
    }
}
