using System;
using System.IO;

namespace CoopServer
{
    class Program
    {
        public static bool ReadyToStop = false;
        static void Main(string[] args)
        {
            try
            {
                Console.Title = "GTACOOP:R Server";

                if (File.Exists("log.txt"))
                {
                    File.WriteAllText("log.txt", string.Empty);
                }

                Console.CancelKeyPress += delegate(object sender, ConsoleCancelEventArgs e)
                {
                    if (e.SpecialKey == ConsoleSpecialKey.ControlC)
                    {
                        e.Cancel = true;
                        ReadyToStop = true;
                    }
                };

                _ = new Server();
            }
            catch (Exception e)
            {
                Logging.Error(e.ToString());
                Console.ReadLine();
            }
        }
    }
}
