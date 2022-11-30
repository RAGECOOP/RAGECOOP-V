using System;

namespace GTA
{
    internal class Console
    {
        private static readonly SHVDN.Console console = AppDomain.CurrentDomain.GetData("Console") as SHVDN.Console;
        public static void Warning(object format, params object[] objects)
        {
            console.PrintInfo("[~o~WARNING~w~] ", format.ToString(), objects);
        }
        public static void Error(object format, params object[] objects)
        {
            console.PrintError("[~r~ERROR~w~] ", format.ToString(), objects);
        }
        public static void Info(object format, params object[] objects)
        {
            console.PrintWarning("[~b~INFO~w~] ", format.ToString(), objects);
        }
    }
}