using System.Linq;

namespace CoopServer
{
    public struct PlayerData
    {
        public string Username { get; set; }
        private int LastPedHandle { get; set; }
        private int CurrentPedHandle { get; set; }
        public int PedHandle
        {
            get => CurrentPedHandle;
            set
            {
                LastPedHandle = CurrentPedHandle;
                CurrentPedHandle = value;
                if (CurrentPedHandle != LastPedHandle)
                {
                    // TODO
                }
            }
        }
        private int LastVehicleHandle { get; set; }
        private int CurrentVehicleHandle { get; set; }
        public int VehicleHandle
        {
            get => CurrentPedHandle;
            set
            {
                LastVehicleHandle = CurrentVehicleHandle;
                CurrentVehicleHandle = value;
                if (CurrentVehicleHandle != LastVehicleHandle)
                {
                    // TODO
                }
            }
        }
        public bool IsInVehicle { get; set; }
        private LVector3 LastPosition { get; set; }
        private LVector3 CurrentPosition { get; set; }
        public LVector3 Position
        {
            get => CurrentPosition;
            set
            {
                LastPosition = CurrentPosition;
                CurrentPosition = value;

                if (Server.Resources.Any() && !LVector3.Equals(CurrentPosition, LastPosition))
                {
                    foreach (Resource resource in Server.Resources)
                    {
                        resource.InvokePlayerPositionUpdate(this);
                    }
                }
            }
        }
        private int CurrentHealth { get; set; }
        public int Health
        {
            get => CurrentHealth;
            set
            {
                if (Server.Resources.Any() && CurrentHealth != value)
                {
                    foreach (Resource resource in Server.Resources)
                    {
                        resource.InvokePlayerHealthUpdate(this);
                    }
                }

                CurrentHealth = value;
            }
        }

        public bool IsInRangeOf(LVector3 position, float distance)
        {
            return LVector3.Subtract(Position, position).Length() < distance;
        }
    }
}
