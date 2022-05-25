using System.Linq;
namespace RageCoop.Core
{
    public class PlayerData
    {
        public string Username { get; set; }

        /// <summary>
        /// Universal character ID.
        /// </summary>
        public int PedID
        {
            get; set;
        }

        /// <summary>
        /// Universal vehicle ID.
        /// </summary>
        public int VehicleID { get; set; }
        public bool IsInVehicle { get; internal set; }
        public LVector3 Position { get; set; }

        /// <summary>
        /// Player Latency in second.
        /// </summary>
        public float Latency { get; set; }
        public int Health { get; set; }

        public bool IsInRangeOf(LVector3 position, float distance)
        {
            return LVector3.Subtract(Position, position).Length() < distance;
        }
    }
}
