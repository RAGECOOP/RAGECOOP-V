using System.Linq;
using GTA.Math;
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
        public Vector3 Position { get; set; }

        /// <summary>
        /// Player Latency in second.
        /// </summary>
        public float Latency { get; set; }
        public int Health { get; set; }
    }
}
