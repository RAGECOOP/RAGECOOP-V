global using GTA;
global using GTA.Native;
global using static GTA.Native.Function;
global using static GTA.Native.Hash;
global using static RageCoop.Client.Shared;
global using Console = GTA.Console;
global using static SHVDN.PInvoke;
using System.IO;

namespace RageCoop.Client
{
    internal static class Shared
    {
        public static string BasePath = "RageCoop";
        public static string DataPath = Path.Combine(BasePath, "Data");
        public static string LogPath = Path.Combine(DataPath, "RageCoop.Client.log");
        public static string SettingsPath = Path.Combine(DataPath, "Setting.json");

        public static string VehicleWeaponDataPath = Path.Combine(DataPath, "VehicleWeapons.json");
        public static string WeaponFixDataPath = Path.Combine(DataPath, "WeaponFixes.json");
        public static string WeaponInfoDataPath = Path.Combine(DataPath, "Weapons.json");
        public static string AnimationsDataPath = Path.Combine(DataPath, "Animations.json");

        public static string CefSubProcessPath = Path.Combine(BasePath, "SubProcess", "RageCoop.Client.CefHost.exe");
    }
}