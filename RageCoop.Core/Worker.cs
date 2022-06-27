using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;

namespace RageCoop.Core
{
    public class Worker:IDisposable
    {
        private SemaphoreSlim _semaphoreSlim;
        private Thread _workerThread;
        private bool _stopping=false;
        public string Name { get; set; }
        public bool IsBusy { get;private set; }
        internal Worker(int maxJobs = Int32.MaxValue,string name="Worker")
        {
            Name = name;
            _semaphoreSlim = new SemaphoreSlim(maxJobs);
            _workerThread=new Thread(() =>
            {
                while (!_stopping)
                {
                    IsBusy=false;
                    _semaphoreSlim.Wait();
                    if(Jobs.TryDequeue(out var job))
                    {
                        IsBusy=true;
                        job.Invoke();
                    }
                    else
                    {
                        throw new InvalidOperationException("Hmm... that's unexpected.");
                    }
                }
                IsBusy=false;
            });
            _workerThread.Start();
        }
        public void QueueWork(Action work)
        {
            Jobs.Enqueue(work);
            _semaphoreSlim.Release();
        }
        public void Stop()
        {
            _stopping=true;
            if (_workerThread.IsAlive)
            {
                _workerThread.Join();
            }
        }
        public void Dispose()
        {
            Stop();
            _semaphoreSlim.Dispose();
        }
        private ConcurrentQueue<Action> Jobs=new ConcurrentQueue<Action>();
    }
}
