using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;

namespace RageCoop.Core.Logging
{
    public class Logger :IDisposable
    {
        
        /// <summary>
        /// 0:Trace, 1:Debug, 2:Info, 3:Warning, 4:Error
        /// </summary>
        public int LogLevel = 0;
        public string LogPath;
        public bool UseConsole = false;
        private static StreamWriter logWriter;

        private string Buffer="";
        private Thread LoggerThread;
        private bool Stopping=false;
        
        public Logger(bool overwrite=true)
        {
            if (File.Exists(LogPath)&&overwrite) { File.Delete(LogPath); }
            LoggerThread=new Thread(() =>
            {
                while (!Stopping)
                {
                    Flush();
                    Thread.Sleep(1000);
                }
            });
            LoggerThread.Start();
        }

        public void Info(string message)
        {
            if (LogLevel>2) { return; }
            lock (Buffer)
            {
                string msg = string.Format("[{0}][{2}] [INF] {1}", Date(), message, Process.GetCurrentProcess().Id);

                Buffer+=msg+"\r\n";
            }
        }

        public void Warning(string message)
        {
            if (LogLevel>3) { return; }
            lock (Buffer)
            {
                string msg = string.Format("[{0}][{2}] [WRN] {1}", Date(), message, Process.GetCurrentProcess().Id);

                Buffer+=msg+"\r\n";

            }
        }

        public void Error(string message)
        {
            if (LogLevel>4) { return; }
            lock (Buffer)
            {
                string msg = string.Format("[{0}][{2}] [ERR] {1}", Date(), message, Process.GetCurrentProcess().Id);

                Buffer+=msg+"\r\n";
            }
        }
        public void Error(Exception ex)
        {
            if (LogLevel>4) { return; }
            lock (Buffer)
            {
                string msg = string.Format("[{0}][{2}] [ERR] {1}", Date(),"\r\n"+ex.ToString(), Process.GetCurrentProcess().Id);

                Buffer+=msg+"\r\n";
            }
        }

        public void Debug(string message)
        {

            if (LogLevel>1) { return; }
            lock (Buffer)
            {
                string msg = string.Format("[{0}][{2}] [DBG] {1}", Date(), message,Process.GetCurrentProcess().Id);

                Buffer+=msg+"\r\n";
            }
        }

        public void Trace(string message)
        {
            if (LogLevel>0) { return; }
            lock (Buffer)
            {
                string msg = string.Format("[{0}][{2}] [TRC] {1}", Date(), message, Process.GetCurrentProcess().Id);

                Buffer+=msg+"\r\n";
            }
        }

        private string Date()
        {
            return DateTime.Now.ToString();
        }
        public void Flush()
        {
            lock (Buffer)
            {
                if (Buffer!="")
                {
                    if (UseConsole)
                    {
                        Console.Write(Buffer);
                        Buffer="";
                    }
                    else
                    {
                        try
                        {
                            logWriter=new StreamWriter(LogPath, true, Encoding.UTF8);
                            logWriter.Write(Buffer);
                            logWriter.Close();
                            Buffer="";
                        }
                        catch { }
                    }
                }

            }
        }
        public void Dispose()
        {
            Stopping=true;
            LoggerThread?.Join();
        }
    }
}
