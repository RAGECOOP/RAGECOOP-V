using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;

namespace RageCoop.Core
{
    public class Loggger
    {
        
        public string LogPath;
        private StreamWriter logWriter;
        private bool UseConsole=false;
        public int LogLevel = 0;
        private string Buffer="";
        
        public Loggger(string path)
        {
            
            
            LogPath=path;
            Task.Run(() =>
            {
                while (true)
                {
                    Flush();
                    Thread.Sleep(1000);
                }
            });
            
        }
        public Loggger()
        {
            UseConsole=true;
            Task.Run(() =>
            {
                while (true)
                {
                    Flush();
                    Thread.Sleep(1000);
                }
            });
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
                string msg = string.Format("[{0}][{2}] [ERR] {1}", Date(),string.Join("\r\n",ex.Message,ex.StackTrace,ex.ToString()), Process.GetCurrentProcess().Id);

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
            if (LogLevel>1) { return; }
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
                    try
                    {
                        logWriter=new StreamWriter(LogPath ,true,Encoding.UTF8);
                        logWriter.Write(Buffer);
                        logWriter.Close();
                        Buffer="";
                    }
                    catch { }
                }

            }
        }
    }
}
