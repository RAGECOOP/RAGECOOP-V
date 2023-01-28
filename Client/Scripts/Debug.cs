using System;
using System.Collections.Generic;
using GTA.UI;

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
        ProjectilesTotal
    }

    internal static class Debug
    {
        public static Dictionary<TimeStamp, long> TimeStamps = new Dictionary<TimeStamp, long>();
        private static int _lastNfHandle;

        static Debug()
        {
            foreach (TimeStamp t in Enum.GetValues<TimeStamp>()) TimeStamps.Add(t, 0);
        }

        public static string Dump(this Dictionary<TimeStamp, long> d)
        {
            var s = "";
            foreach (var kvp in d) s += kvp.Key + ":" + kvp.Value + "\n";
            return s;
        }

        public static void ShowTimeStamps()
        {
            Notification.Hide(_lastNfHandle);
            _lastNfHandle = Notification.Show(TimeStamps.Dump());
        }
    }
}