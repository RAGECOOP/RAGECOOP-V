global using GTA;
global using GTA.Native;
global using static GTA.Native.Function;
global using static GTA.Native.Hash;
global using static RageCoop.Client.Shared;
global using static RageCoop.Client.Main;
global using Console = GTA.Console;
global using static RageCoop.Core.Shared;
using System.IO;
using System;
using SHVDN;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace RageCoop.Client
{
    [AttributeUsage(AttributeTargets.Field)]
    class DebugTunableAttribute : Attribute
    {
    }
    internal static class Shared
    {
        private static unsafe string GetBasePath()
        {
            using var fs = File.OpenRead(Instance.FilePath);
            var buf = stackalloc char[1024];
            GetFinalPathNameByHandleW(fs.SafeFileHandle.DangerousGetHandle(), buf, 1024, 0);
            ErrorCheck32();
            var scriptDir = Directory.GetParent(Marshal.PtrToStringUni((IntPtr)buf)).FullName;
            if (Path.GetFileName(scriptDir).ToLower() != "scripts")
                throw new Exception("Unexpected script location");

            var result = Directory.GetParent(scriptDir).FullName;
            Logger.Debug($"Base path is: {result}");
            return result;
        }

        public static string BasePath = GetBasePath();
        public static string DataPath = Path.Combine(BasePath, "Data");
        public static string SettingsPath = Path.Combine(DataPath, "Setting.json");

        public static string VehicleWeaponDataPath = Path.Combine(DataPath, "VehicleWeapons.json");
        public static string WeaponFixDataPath = Path.Combine(DataPath, "WeaponFixes.json");
        public static string WeaponInfoDataPath = Path.Combine(DataPath, "Weapons.json");
        public static string AnimationsDataPath = Path.Combine(DataPath, "Animations.json");

        public static string CefSubProcessPath = Path.Combine(BasePath, "SubProcess", "RageCoop.Client.CefHost.exe");
    }
}