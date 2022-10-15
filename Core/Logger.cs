using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace RageCoop.Core
{

    public enum LogLevel
    {
        Trace = 0,
        Debug = 1,
        Info = 2,
        Warning = 3,
        Error = 4
    }

    /// <summary>
    /// 
    /// </summary>
    public class Logger : MarshalByRefObject, IDisposable
    {
        public class LogLine
        {
            internal LogLine() { }
            public DateTime TimeStamp;
            public LogLevel LogLevel;
            public string Message;
        }
        /// <summary>
        /// 0:Trace, 1:Debug, 2:Info, 3:Warning, 4:Error
        /// </summary>
        public int LogLevel = 0;
        /// <summary>
        /// Name of this logger
        /// </summary>
        public string Name = "Logger";
        public readonly string DateTimeFormat = "HH:mm:ss";

        /// <summary>
        /// Whether to use UTC time for timestamping the log
        /// </summary>
        public readonly bool UseUtc = false;
        public List<StreamWriter> Writers = new List<StreamWriter> { new StreamWriter(Console.OpenStandardOutput()) };
        public int FlushInterval = 1000;
        public event FlushDelegate OnFlush;
        public bool FlushImmediately = false;
        public delegate void FlushDelegate(LogLine line, string fomatted);

        private readonly Thread LoggerThread;
        private bool Stopping = false;
        private readonly ConcurrentQueue<LogLine> _queuedLines = new ConcurrentQueue<LogLine>();
        internal Logger()
        {
            Name = Process.GetCurrentProcess().Id.ToString();
            if (!FlushImmediately)
            {
                LoggerThread = new Thread(() =>
                  {
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
            Enqueue(2, message);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public void Warning(string message)
        {
            Enqueue(3, message);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public void Error(string message)
        {
            Enqueue(4, message);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="error"></param>
        public void Error(string message, Exception error)
        {
            Enqueue(4, $"{message}:\n {error}");
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ex"></param>
        public void Error(Exception ex)
        {
            Enqueue(4, ex.ToString());
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public void Debug(string message)
        {
            Enqueue(1, message);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public void Trace(string message)
        {
            Enqueue(0, message);
        }
        public void Enqueue(int level, string message)
        {
            if (level < LogLevel) { return; }
            _queuedLines.Enqueue(new LogLine()
            {
                Message = message,
                TimeStamp = UseUtc ? DateTime.UtcNow : DateTime.Now,
                LogLevel = (LogLevel)level
            });
            if (FlushImmediately)
            {
                Flush();
            }
        }

        private string Format(LogLine line)
        {
            return string.Format("[{0}][{2}] [{3}] {1}", line.TimeStamp.ToString(DateTimeFormat), line.Message, Name, line.LogLevel.ToString());
        }
        /// <summary>
        /// 
        /// </summary>
        public void Flush()
        {
            lock (_queuedLines)
            {
                try
                {
                    while (_queuedLines.TryDequeue(out var line))
                    {
                        var formatted = Format(line);
                        Writers.ForEach(x => { x.WriteLine(formatted); x.Flush(); });
                        OnFlush?.Invoke(line, formatted);
                    }
                }
                catch { }
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
