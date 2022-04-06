using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CoopServer
{
    class Program
    {
        public static bool ReadyToStop = false;
        static void Main(string[] args)
        {
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

#if DEBUG
        private static async Task<double> GetCpuUsageForProcess()
        {
            DateTime startTime = DateTime.UtcNow;

            TimeSpan startCpuUsage = Process.GetCurrentProcess().TotalProcessorTime;
            await Task.Delay(500);

            DateTime endTime = DateTime.UtcNow;
            TimeSpan endCpuUsage = Process.GetCurrentProcess().TotalProcessorTime;
            double cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
            double totalMsPassed = (endTime - startTime).TotalMilliseconds;
            double cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);
            return cpuUsageTotal * 100;
        }
#endif
    }
}
