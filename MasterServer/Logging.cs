using System;
using System.IO;

namespace MasterServer
{
    class Logging
    {
        private static readonly object Lock = new();

        public static void Info(string message)
        {
            lock (Lock)
            {
                string msg = string.Format("[{0}] [INFO] {1}", Date(), message);

                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine(msg);
                Console.ResetColor();

                using StreamWriter sw = new("log.txt", true);
                sw.WriteLine(msg);
            }
        }

        public static void Warning(string message)
        {
            lock (Lock)
            {
                string msg = string.Format("[{0}] [WARNING] {1}", Date(), message);

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(msg);
                Console.ResetColor();

                using StreamWriter sw = new("log.txt", true);
                sw.WriteLine(msg);
            }
        }

        public static void Error(string message)
        {
            lock (Lock)
            {
                string msg = string.Format("[{0}] [ERROR] {1}", Date(), message);

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(msg);
                Console.ResetColor();

                using StreamWriter sw = new("log.txt", true);
                sw.WriteLine(msg);
            }
        }

        private static string Date()
        {
            return DateTime.Now.ToString();
        }
    }
}
