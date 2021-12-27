using System.Linq;

namespace CoopServer
{
    public struct PlayerData
    {
        public string Username { get; internal set; }
        private int LastPedHandle { get; set; }
        private int CurrentPedHandle { get; set; }
        public int PedHandle
        {
            get => CurrentPedHandle;
            internal set
            {
                LastPedHandle = CurrentPedHandle == default ? value : CurrentPedHandle;
                CurrentPedHandle = value;

                if (CurrentPedHandle != LastPedHandle && Server.Resources.Any())
                {
                    foreach (Resource resource in Server.Resources)
                    {
                        resource.InvokePlayerPedHandleUpdate(Username);
                    }
                }
            }
        }
        private int LastVehicleHandle { get; set; }
        private int CurrentVehicleHandle { get; set; }
        public int VehicleHandle
        {
            get => CurrentPedHandle;
            internal set
            {
                LastVehicleHandle = CurrentVehicleHandle == default ? value : CurrentVehicleHandle;
                CurrentVehicleHandle = value;

                if (CurrentVehicleHandle != LastVehicleHandle && Server.Resources.Any())
                {
                    foreach (Resource resource in Server.Resources)
                    {
                        resource.InvokePlayerPedHandleUpdate(Username);
                    }
                }
            }
        }
        public bool IsInVehicle { get; internal set; }
        private LVector3 LastPosition { get; set; }
        private LVector3 CurrentPosition { get; set; }
        public LVector3 Position
        {
            get => CurrentPosition;
            internal set
            {
                LastPosition = CurrentPosition.Equals(default(LVector3)) ? value : CurrentPosition;
                CurrentPosition = value;

                if (Server.Resources.Any() && !LVector3.Equals(CurrentPosition, LastPosition))
                {
                    foreach (Resource resource in Server.Resources)
                    {
                        resource.InvokePlayerPositionUpdate(Username);
                    }
                }
            }
        }
        private int LastHealth { get; set; }
        private int CurrentHealth { get; set; }
        public int Health
        {
            get => CurrentHealth;
            internal set
            {
                LastHealth = CurrentHealth == default ? value : CurrentHealth;
                CurrentHealth = value;

                if (CurrentHealth != LastHealth && Server.Resources.Any())
                {
                    foreach (Resource resource in Server.Resources)
                    {
                        resource.InvokePlayerHealthUpdate(Username);
                    }
                }
            }
        }

        public bool IsInRangeOf(LVector3 position, float distance)
        {
            return LVector3.Subtract(Position, position).Length() < distance;
        }
    }
}
