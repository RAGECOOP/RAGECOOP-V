using System;
using System.IO;

namespace CoopServer
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Console.Title = "GTACOOP:R Server";

                if (File.Exists("log.txt"))
                {
                    File.WriteAllText("log.txt", string.Empty);
                }

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
