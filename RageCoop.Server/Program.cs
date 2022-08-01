using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using RageCoop.Core;
namespace RageCoop.Server
{
    class Program
    {
        private static bool Stopping = false;
        static void Main(string[] args)
        {
            var mainLogger= new Logger()
            {
                LogPath="RageCoop.Server.log",
                UseConsole=true,
                Name="Server"
            };
            try
            {
                Console.Title = "RAGECOOP";
                var setting = Util.Read<ServerSettings>("Settings.xml");
#if DEBUG
                setting.LogLevel=0;
#endif
                var server = new Server(setting, mainLogger);
                Console.CancelKeyPress += delegate(object sender, ConsoleCancelEventArgs e)
                {
                    mainLogger.Info("Initiating shutdown sequence...");
                    mainLogger.Info("Press Ctrl+C again to commence an emergency shutdown.");
                    if (e.SpecialKey == ConsoleSpecialKey.ControlC)
                    {
                        if (!Stopping)
                        {
                            e.Cancel = true;
                            Stopping = true;
                            server.Stop();
                            mainLogger.Info("Server stopped.");
                            mainLogger.Dispose();
                            Thread.Sleep(3000);
                            Environment.Exit(0);
                        }
                        else
                        {
                            mainLogger.Flush();
                            Environment.Exit(1);
                        }
                    }
                };
                server.Start();
                mainLogger?.Info("Please use CTRL + C if you want to stop the server!");
                mainLogger?.Info("Type here to send chat messages or execute commands");
                mainLogger?.Flush();
                while (true)
                {
                    
                    var s=Console.ReadLine();
                    if (!Stopping && s!=null)
                    {
                        server.ChatMessageReceived("Server", s, null);
                    }
                    Thread.Sleep(20);
                }
            }
            catch (Exception e)
            {
                mainLogger.Error(e);
                mainLogger.Error($"Fatal error occurred, server shutting down.");
                mainLogger.Flush();
                Thread.Sleep(5000);
                Environment.Exit(1);
            }
        }
    }
}
