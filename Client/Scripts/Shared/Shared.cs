using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RageCoop.Client
{
    internal static partial class Shared
    {
        public static string BasePath = "RageCoop";
        public static string DataPath = Path.Combine(BasePath, "Data");
        public static string LogPath = Path.Combine(DataPath, "RageCoop.Client.log");
        public static string SettingsPath = Path.Combine(DataPath,"Setting.json");

        public static string VehicleWeaponDataPath = Path.Combine(DataPath, "VehicleWeapons.json");
        public static string WeaponFixDataPath = Path.Combine(DataPath, "WeaponFixes.json");
        public static string WeaponInfoDataPath = Path.Combine(DataPath, "Weapons.json");
        public static string AnimationsDataPath = Path.Combine(DataPath, "Animations.json");

        public static string CefSubProcessPath = Path.Combine(BasePath, "SubProcess", "RageCoop.Client.CefHost.exe");
    }
}
