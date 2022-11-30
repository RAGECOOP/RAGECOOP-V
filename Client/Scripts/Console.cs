using System;

namespace GTA
{
    /// <summary>
    ///     Wrapper that provides access to SHVDN's in-game console
    /// </summary>
    public class Console
    {
        private static SHVDN.Console console => (SHVDN.Console)AppDomain.CurrentDomain.GetData("Console");

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