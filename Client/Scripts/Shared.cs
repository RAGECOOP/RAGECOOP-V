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

namespace RageCoop.Client
{
    internal static class Shared
    {
        private static string GetBasePath()
        {
            FileInfo info;
            string realScriptDir = Directory.GetParent(Instance.FilePath).FullName;
        nextTarget:
            info = new(realScriptDir);
            if (info.LinkTarget != null)
            {
                realScriptDir = info.LinkTarget;
                goto nextTarget;
            }
            if (Path.GetFileName(realScriptDir).ToLower() != "scripts")
                throw new FileNotFoundException($"Unexpected link target {realScriptDir}");

            var baseDir = Directory.GetParent(realScriptDir).FullName;
            Logger.Debug($"Base directory is {baseDir}");
            return baseDir;
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