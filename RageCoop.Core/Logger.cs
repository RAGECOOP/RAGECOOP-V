using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace RageCoop.Core
{
    /// <summary>
    /// 
    /// </summary>
    public class Logger : IDisposable
    {

        /// <summary>
        /// 0:Trace, 1:Debug, 2:Info, 3:Warning, 4:Error
        /// </summary>
        public int LogLevel = 0;
        /// <summary>
        /// Name of this logger
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Path to log file.
        /// </summary>
        public string LogPath;
        /// <summary>
        /// Whether to flush messages to console instead of log file
        /// </summary>
        public bool UseConsole = false;
        private StreamWriter logWriter;

        private string Buffer = "";
        private readonly Thread LoggerThread;
        private bool Stopping = false;
        private readonly bool FlushImmediately;

        internal Logger(bool flushImmediately = false, bool overwrite = true)
        {
            FlushImmediately = flushImmediately;
            if (File.Exists(LogPath) && overwrite) { File.Delete(LogPath); }
            Name = Process.GetCurrentProcess().Id.ToString();
            if (!flushImmediately)
            {
                LoggerThread = new Thread(() =>
                  {
                      if (!UseConsole)
                      {
                          while (LogPath == default)
                          {
                              Thread.Sleep(100);
                          }
                          if (File.Exists(LogPath) && overwrite) { File.Delete(LogPath); }
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
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public void Info(string message)
        {
            if (LogLevel > 2) { return; }
            lock (Buffer)
            {
                string msg = string.Format("[{0}][{2}] [INF] {1}", Date(), message, Name);

                Buffer += msg + "\r\n";
            }
            if (FlushImmediately)
            {
                Flush();
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public void Warning(string message)
        {
            if (LogLevel > 3) { return; }
            lock (Buffer)
            {
                string msg = string.Format("[{0}][{2}] [WRN] {1}", Date(), message, Name);

                Buffer += msg + "\r\n";

            }
            if (FlushImmediately)
            {
                Flush();
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public void Error(string message)
        {
            if (LogLevel > 4) { return; }
            lock (Buffer)
            {
                string msg = string.Format("[{0}][{2}] [ERR] {1}", Date(), message, Name);

                Buffer += msg + "\r\n";
            }
            if (FlushImmediately)
            {
                Flush();
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="error"></param>
        public void Error(string message, Exception error)
        {
            if (LogLevel > 4) { return; }
            lock (Buffer)
            {
                string msg = string.Format("[{0}][{2}] [ERR] {1}:{3}", Date(), message, Name, error.Message);
                Buffer += msg + "\r\n";
                Trace(error.ToString());

            }
            if (FlushImmediately)
            {
                Flush();
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ex"></param>
        public void Error(Exception ex)
        {
            if (LogLevel > 4) { return; }
            lock (Buffer)
            {
                string msg = string.Format("[{0}][{2}] [ERR] {1}", Date(), "\r\n" + ex.Message, Name);
                Buffer += msg + "\r\n";
                Trace(ex.ToString());
            }
            if (FlushImmediately)
            {
                Flush();
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public void Debug(string message)
        {

            if (LogLevel > 1) { return; }
            lock (Buffer)
            {
                string msg = string.Format("[{0}][{2}] [DBG] {1}", Date(), message, Name);

                Buffer += msg + "\r\n";
            }
            if (FlushImmediately)
            {
                Flush();
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public void Trace(string message)
        {
            if (LogLevel > 0) { return; }
            lock (Buffer)
            {
                string msg = string.Format("[{0}][{2}] [TRC] {1}", Date(), message, Name);

                Buffer += msg + "\r\n";
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
        /// <summary>
        /// 
        /// </summary>
        public void Flush()
        {
            lock (Buffer)
            {
                if (Buffer != "")
                {
                    if (UseConsole)
                    {
                        Console.Write(Buffer);
                        Buffer = "";
                    }
                    else
                    {
                        try
                        {
                            logWriter = new StreamWriter(LogPath, true, Encoding.UTF8);
                            logWriter.Write(Buffer);
                            logWriter.Close();
                            Buffer = "";
                        }
                        catch { }
                    }
                }

            }
        }
        /// <summary>
        /// Stop backdround thread and flush all pending messages.
        /// </summary>
        public void Dispose()
        {
            Stopping = true;
            LoggerThread?.Join();
        }
    }
}
