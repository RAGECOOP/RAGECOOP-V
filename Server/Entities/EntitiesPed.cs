namespace CoopServer.Entities
{
    public struct EntitiesPed
    {
        public LVector3 Position { get; set; }

        public bool IsInRangeOf(LVector3 position, float distance)
        {
            return LVector3.Subtract(Position, position).Length() < distance;
        }
    }
}
