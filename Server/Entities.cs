using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RageCoop.Core;

namespace RageCoop.Server
{
    internal static class EntitiesBlah
    {
        public static Dictionary<long,SyncedCharacter> Peds=new Dictionary<long,SyncedCharacter>();
        public static Dictionary<long, SyncedVehicle> Vehicles = new Dictionary<long, SyncedVehicle>();

    }
    internal class SyncedVehicle
    {
        public long Owner { get; set; }
        // <index, (enum)VehicleSeat>
        // public Dictionary<int, int> Seats=new Dictionary<int, int>();
    }
    internal class SyncedCharacter
    {
        public long Owner { get; set; }
    }
}
