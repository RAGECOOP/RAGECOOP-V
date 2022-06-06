using System.Linq;
using System.Runtime.CompilerServices;
using GTA.Math;

[assembly: InternalsVisibleTo("RageCoop.Server")]
[assembly: InternalsVisibleTo("RageCoop.Client")]
namespace RageCoop.Core
{
    public class PlayerData
    {
        public string Username { get; internal set; }

        /// <summary>
        /// Universal character ID.
        /// </summary>
        public int PedID
        {
            get; internal set;
        }

        /// <summary>
        /// The ID of player's last vehicle.
        /// </summary>
        public int VehicleID { get; internal set; }
        public Vector3 Position { get;internal set; }

        /// <summary>
        /// Player Latency in second.
        /// </summary>
        public float Latency { get; internal set; }
        public int Health { get; internal set; }
    }
}
