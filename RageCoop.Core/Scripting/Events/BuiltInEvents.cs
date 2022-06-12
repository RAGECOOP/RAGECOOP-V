using System;
using System.Collections.Generic;
using System.Text;

namespace RageCoop.Core.Scripting.Events
{
    public class OnVehicleSpawned:CustomEvent
    {
        public int VehicleID { get; set; }
        public override int EventID { get; set; } = Hasher.Hash("RageCoop.OnVehicleSpawned");

        public override byte[] Serialize()
        {
            return BitConverter.GetBytes(VehicleID);
        }
        public override void Deserialize(byte[] data)
        {
            VehicleID= BitConverter.ToInt32(data, 0);
        }
    }
}
