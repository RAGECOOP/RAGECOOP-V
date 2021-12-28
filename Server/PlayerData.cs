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

                if (CurrentPedHandle != LastPedHandle && Server.RunningResource != null)
                {
                    Server.RunningResource.InvokePlayerPedHandleUpdate(Username);
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

                if (CurrentVehicleHandle != LastVehicleHandle && Server.RunningResource != null)
                {
                    Server.RunningResource.InvokePlayerPedHandleUpdate(Username);
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

                if (Server.RunningResource != null && !LVector3.Equals(CurrentPosition, LastPosition))
                {
                    Server.RunningResource.InvokePlayerPositionUpdate(Username);
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

                if (CurrentHealth != LastHealth && Server.RunningResource != null)
                {
                    Server.RunningResource.InvokePlayerHealthUpdate(Username);
                }
            }
        }

        public bool IsInRangeOf(LVector3 position, float distance)
        {
            return LVector3.Subtract(Position, position).Length() < distance;
        }
    }
}
