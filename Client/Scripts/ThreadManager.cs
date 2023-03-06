using RageCoop.Client.Scripting;
using RageCoop.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace RageCoop.Client
{

    /// <summary>
    /// Needed to properly stop all thread when the module unloads
    /// </summary>
    internal static class ThreadManager
    {
        private static readonly List<Thread> _threads = new();
        private static readonly Thread _watcher = new(RemoveStopped);
        static ThreadManager()
        {
            _watcher.Start();
        }
        private static void RemoveStopped()
        {
            while (!IsUnloading)
            {
                lock (_threads)
                {
                    _threads.RemoveAll(t => !t.IsAlive);
                }
                Thread.Sleep(1000);
            }
        }
        public static Thread CreateThread(Action callback, string name = "CoopThread", bool startNow = true)
        {
            lock (_threads)
            {
                var created = new Thread(() =>
                {
                    try
                    {
                        callback();
                    }
                    catch (ThreadInterruptedException) { }
                    catch (Exception ex)
                    {
                        Log.Error($"Unhandled exception caught in thread {Environment.CurrentManagedThreadId}", ex);
                    }
                    finally
                    {
                        Log.Debug($"Thread stopped: " + Environment.CurrentManagedThreadId);
                    }
                })
                {
                    Name = name
                };
                Log.Debug($"Thread created: {name}, id: {created.ManagedThreadId}");
                _threads.Add(created);
                if (startNow) created.Start();
                return created;
            }
        }

        public static void OnUnload()
        {
            Log.Debug("Stopping background threads");
            lock (_threads)
            {
                foreach (var thread in _threads)
                {
                    if (thread.IsAlive)
                    {
                        Log.Debug($"Waiting for thread {thread.ManagedThreadId} to stop");
                        // thread.Interrupt(); PlatformNotSupportedException ?
                        thread.Join();
                    }
                }
                _threads.Clear();
            }
            Log.Debug("Stopping thread watcher");
            _watcher.Join();
        }
    }
}
