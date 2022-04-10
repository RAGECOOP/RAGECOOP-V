using System.Linq;

namespace CoopServer
{
    public struct PlayerData
    {
        public string Username { get; internal set; } = string.Empty;
        private int _lastPedHandle { get; set; } = 0;
        private int _currentPedHandle { get; set; } = 0;
        public int PedHandle
        {
            get => _currentPedHandle;
            internal set
            {
                _lastPedHandle = _currentPedHandle == 0 ? value : _currentPedHandle;
                _currentPedHandle = value;

                if (_currentPedHandle != _lastPedHandle && Server.RunningResource != null)
                {
                    Server.RunningResource.InvokePlayerPedHandleUpdate(Username);
                }
            }
        }
        private int _lastVehicleHandle { get; set; } = 0;
        private int _currentVehicleHandle { get; set; } = 0;
        public int VehicleHandle
        {
            get => _currentVehicleHandle;
            internal set
            {
                _lastVehicleHandle = _currentVehicleHandle == 0 ? value : _currentVehicleHandle;
                _currentVehicleHandle = value;

                if (_currentVehicleHandle != _lastVehicleHandle && Server.RunningResource != null)
                {
                    Server.RunningResource.InvokePlayerPedHandleUpdate(Username);
                }
            }
        }
        public bool IsInVehicle { get; internal set; } = false;
        private LVector3 _lastPosition { get; set; } = new LVector3();
        private LVector3 _currentPosition { get; set; } = new LVector3();
        public LVector3 Position
        {
            get => _currentPosition;
            internal set
            {
                _lastPosition = _currentPosition.Equals(default(LVector3)) ? value : _currentPosition;
                _currentPosition = value;

                if (Server.RunningResource != null && !LVector3.Equals(_currentPosition, _lastPosition))
                {
                    Server.RunningResource.InvokePlayerPositionUpdate(Username);
                }
            }
        }
        private int _lastHealth { get; set; } = 0;
        private int _currentHealth { get; set; } = 0;
        public int Health
        {
            get => _currentHealth;
            internal set
            {
                _lastHealth = _currentHealth == 0 ? value : _currentHealth;
                _currentHealth = value;

                if (_currentHealth != _lastHealth && Server.RunningResource != null)
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
