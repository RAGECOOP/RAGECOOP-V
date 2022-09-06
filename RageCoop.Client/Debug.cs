using System;
using System.Collections.Generic;

namespace RageCoop.Client
{
    internal enum TimeStamp
    {
        AddPeds,
        PedTotal,
        AddVehicles,
        VehicleTotal,
        SendPed,
        SendPedState,
        SendVehicle,
        SendVehicleState,
        UpdatePed,
        UpdateVehicle,
        CheckProjectiles,
        GetAllEntities,
        Receive,
        ProjectilesTotal,
    }
    internal static class Debug
    {
        public static Dictionary<TimeStamp, long> TimeStamps = new Dictionary<TimeStamp, long>();
        private static int _lastNfHandle;
        static Debug()
        {
            foreach (TimeStamp t in Enum.GetValues(typeof(TimeStamp)))
            {
                TimeStamps.Add(t, 0);
            }
        }
        public static string Dump(this Dictionary<TimeStamp, long> d)
        {
            string s = "";
            foreach (KeyValuePair<TimeStamp, long> kvp in d)
            {
                s += kvp.Key + ":" + kvp.Value + "\n";
            }
            return s;
        }
        public static void ShowTimeStamps()
        {
            GTA.UI.Notification.Hide(_lastNfHandle);
            _lastNfHandle = GTA.UI.Notification.Show(Debug.TimeStamps.Dump());

        }
    }
}
