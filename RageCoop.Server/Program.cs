using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace RageCoop.Server
{
    class Program
    {
        public static bool ReadyToStop = false;
        static void Main(string[] args)
        {
            var mainLogger= new Core.Logger()
            {
                LogPath="RageCoop.Server.log",
                UseConsole=true,
            };
            try
            {
#if DEBUG
                new Thread(async () =>
                {
                    do
                    {
                        Console.Title = string.Format("RAGECOOP [{0,5:P2}] [{1:F}MB]", await GetCpuUsageForProcess(), Process.GetCurrentProcess().PrivateMemorySize64 * 0.000001);

                        Thread.Sleep(500);
                    } while (true);
                }).Start();
#else
                Console.Title = "RAGECOOP";
#endif

                Console.CancelKeyPress += delegate(object sender, ConsoleCancelEventArgs e)
                {
                    if (e.SpecialKey == ConsoleSpecialKey.ControlC)
                    {
                        if (!ReadyToStop)
                        {
                            e.Cancel = true;
                            ReadyToStop = true;
                        }
                        else
                        {
                            Environment.Exit(1);
                        }
                    }
                };

                _ = new Server(Util.Read<ServerSettings>("Settings.xml"), mainLogger);
            }
            catch (Exception e)
            {
                mainLogger.Error(e);
                mainLogger.Error($"Fatal error occurred, server shutting down.");
                Thread.Sleep(3000);
            }
            mainLogger.Dispose();
        }
    }
}
