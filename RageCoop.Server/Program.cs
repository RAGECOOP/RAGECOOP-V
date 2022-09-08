using RageCoop.Core;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace RageCoop.Server
{
    internal class Program
    {
        private static bool Stopping = false;
        private static Logger mainLogger;

        private static void Main(string[] args)
        {
            if (args.Length >= 2 && args[0] == "update")
            {
                var target = args[1];
                int i = 0;
                while (i++ < 10)
                {
                    try
                    {
                        Console.WriteLine("Applying update to " + target);

                        CoreUtils.CopyFilesRecursively(new(AppDomain.CurrentDomain.BaseDirectory), new(target));
                        Process.Start(Path.Combine(target, "RageCoop.Server"));
                        Environment.Exit(0);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                        Thread.Sleep(3000);
                    }
                }
                Environment.Exit(i);
            }
            AppDomain.CurrentDomain.UnhandledException += UnhandledException;
            mainLogger = new Logger()
            {
                LogPath = "RageCoop.Server.log",
                UseConsole = true,
                Name = "Server"
            };
            try
            {
                Console.Title = "RAGECOOP";
                var setting = Util.Read<Settings>("Settings.xml");
#if DEBUG
                setting.LogLevel = 0;
#endif
                var server = new Server(setting, mainLogger);
                Console.CancelKeyPress += delegate (object sender, ConsoleCancelEventArgs e)
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
                            Thread.Sleep(1000);
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

                    var s = Console.ReadLine();
                    if (!Stopping && s != null)
                    {
                        server.ChatMessageReceived("Server", s, null);
                    }
                    Thread.Sleep(20);
                }
            }
            catch (Exception e)
            {
                Fatal(e);
            }
        }

        private static void UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            mainLogger.Error($"Unhandled exception thrown from user thread", e.ExceptionObject as Exception);
            mainLogger.Flush();
        }

        private static void Fatal(Exception e)
        {
            mainLogger.Error(e);
            mainLogger.Error($"Fatal error occurred, server shutting down.");
            mainLogger.Flush();
            Thread.Sleep(5000);
            Environment.Exit(1);
        }
    }
}
