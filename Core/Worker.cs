using System;
using System.Collections.Concurrent;
using System.Threading;

namespace RageCoop.Core
{
    /// <summary>
    /// A worker that constantly execute jobs in a background thread.
    /// </summary>
    public class Worker : IDisposable
    {
        private readonly SemaphoreSlim _semaphoreSlim;
        private readonly Thread _workerThread;
        private bool _stopping = false;
        /// <summary>
        /// Name of the worker
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Whether this worker is busy executing job(s).
        /// </summary>
        public bool IsBusy { get; private set; }
        internal Worker(string name, Logger logger, int maxJobs = Int32.MaxValue)
        {
            Name = name;
            _semaphoreSlim = new SemaphoreSlim(0, maxJobs);
            _workerThread = new Thread(() =>
              {
                  while (!_stopping)
                  {
                      IsBusy = false;
                      _semaphoreSlim.Wait();
                      if (Jobs.TryDequeue(out var job))
                      {
                          IsBusy = true;
                          try
                          {
                              job.Invoke();
                          }
                          catch (Exception ex)
                          {
                              logger.Error("Error occurred when executing queued job:");
                              logger.Error(ex);
                          }
                      }
                      else
                      {
                          throw new InvalidOperationException("Hmm... that's unexpected.");
                      }
                  }
                  IsBusy = false;
              });
            _workerThread.Start();
        }
        /// <summary>
        /// Queue a job to be executed
        /// </summary>
        /// <param name="work"></param>
        public void QueueJob(Action work)
        {
            Jobs.Enqueue(work);
            _semaphoreSlim.Release();
        }
        /// <summary>
        /// Finish current job and stop the worker.
        /// </summary>
        public void Stop()
        {
            _stopping = true;
            QueueJob(() => { });
            if (_workerThread.IsAlive)
            {
                _workerThread.Join();
            }
        }
        /// <summary>
        /// Finish current job and stop the worker.
        /// </summary>
        public void Dispose()
        {
            Stop();
            _semaphoreSlim.Dispose();
        }
        private readonly ConcurrentQueue<Action> Jobs = new ConcurrentQueue<Action>();
    }
}
