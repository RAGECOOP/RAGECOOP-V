using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace RageCoop.Core
{

    public class Logger : IDisposable
    {

        /// <summary>
        /// 0:Trace, 1:Debug, 2:Info, 3:Warning, 4:Error
        /// </summary>
        public int LogLevel = 0;
        public string Name { get; set; }
        public string LogPath;
        public bool UseConsole = false;
        private StreamWriter logWriter;

        private string Buffer = "";
        private Thread LoggerThread;
        private bool Stopping = false;
        private bool FlushImmediately;

        public Logger(bool flushImmediately = false, bool overwrite = true)
        {
            FlushImmediately = flushImmediately;
            if (File.Exists(LogPath)&&overwrite) { File.Delete(LogPath); }
            Name=Process.GetCurrentProcess().Id.ToString();
            if (!flushImmediately)
            {
                LoggerThread=new Thread(() =>
                {
                    if (!UseConsole)
                    {
                        while (LogPath==default)
                        {
                            Thread.Sleep(100);
                        }
                        if (File.Exists(LogPath)&&overwrite) { File.Delete(LogPath); }
                    }
                    while (!Stopping)
                    {
                        Flush();
                        Thread.Sleep(1000);
                    }
                    Flush();
                });
                LoggerThread.Start();
            }
        }

        public void Info(string message)
        {
            if (LogLevel>2) { return; }
            lock (Buffer)
            {
                string msg = string.Format("[{0}][{2}] [INF] {1}", Date(), message, Name);

                Buffer+=msg+"\r\n";
            }
            if (FlushImmediately)
            {
                Flush();
            }
        }

        public void Warning(string message)
        {
            if (LogLevel>3) { return; }
            lock (Buffer)
            {
                string msg = string.Format("[{0}][{2}] [WRN] {1}", Date(), message, Name);

                Buffer+=msg+"\r\n";

            }
            if (FlushImmediately)
            {
                Flush();
            }
        }

        public void Error(string message)
        {
            if (LogLevel>4) { return; }
            lock (Buffer)
            {
                string msg = string.Format("[{0}][{2}] [ERR] {1}", Date(), message, Name);

                Buffer+=msg+"\r\n";
            }
            if (FlushImmediately)
            {
                Flush();
            }
        }
        public void Error(Exception ex)
        {
            if (LogLevel>4) { return; }
            lock (Buffer)
            {
                string msg = string.Format("[{0}][{2}] [ERR] {1}", Date(), "\r\n"+ex.ToString(), Name);
                // msg += string.Format("\r\n[{0}][{2}] [ERR] {1}", Date(), "\r\n"+ex.StackTrace, Process.GetCurrentProcess().Id);

                Buffer+=msg+"\r\n";
            }
            if (FlushImmediately)
            {
                Flush();
            }
        }

        public void Debug(string message)
        {

            if (LogLevel>1) { return; }
            lock (Buffer)
            {
                string msg = string.Format("[{0}][{2}] [DBG] {1}", Date(), message, Name);

                Buffer+=msg+"\r\n";
            }
            if (FlushImmediately)
            {
                Flush();
            }
        }

        public void Trace(string message)
        {
            if (LogLevel>0) { return; }
            lock (Buffer)
            {
                string msg = string.Format("[{0}][{2}] [TRC] {1}", Date(), message, Name);

                Buffer+=msg+"\r\n";
            }
            if (FlushImmediately)
            {
                Flush();
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
