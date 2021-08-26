namespace CoopServer
{
    public struct PlayerData
    {
        public string SocialClubName { get; set; }
        public string Username { get; set; }
        private LVector3 LastPosition { get; set; }
        private LVector3 CurrentPosition { get; set; }
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
