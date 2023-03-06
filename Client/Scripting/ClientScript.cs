using GTA;
using RageCoop.Core;
using RageCoop.Core.Scripting;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace RageCoop.Client.Scripting
{
    [JsonDontSerialize]
    [ScriptAttributes(NoDefaultInstance = true)]
    public abstract class ClientScript : Script
    {
        ConcurrentQueue<Func<bool>> _jobQueue = new();
        Queue<Func<bool>> _reAdd = new();
        /// <summary>
        /// Fully qualified path to the module that the current script runs in.
        /// </summary>
        public static readonly string FullPath;
        static unsafe ClientScript()
        {
            char* buf = stackalloc char[260];
            // TODO: needs some fix up here
            // SHVDN.PInvoke.GetModuleFileNameW(SHVDN.Core.CurrentModule, buf, 260);
            if (Marshal.GetLastWin32Error() != 0)
                throw new Win32Exception("Failed to get path for current module");
            FullPath = new(buf);
        }

        public ClientScript()
        {
            CurrentResource = APIBridge.GetResouceFromFilePath(FullPath);
            if (CurrentResource == null)
                throw new Exception("No resource associated with this script is found");

            CurrentFile = CurrentResource.Files.Values.FirstOrDefault(x => x?.FullPath?.ToLower() == FullPath.ToLower());
            if (CurrentFile == null)
            {
                Logger.Warning("No file associated with curent script was found");
            }
        }
        protected void QueueAction(Func<bool> action) => _jobQueue.Enqueue(action);
        protected void QueueAction(Action action) => QueueAction(() => { action(); return true; });
        protected override void OnTick()
        {
            base.OnTick();
            DoQueuedJobs();
        }
        private void DoQueuedJobs()
        {
            while (_reAdd.TryDequeue(out var toAdd))
                _jobQueue.Enqueue(toAdd);
            while (_jobQueue.TryDequeue(out var job))
            {
                if (!job())
                    _reAdd.Enqueue(job);
            }
        }

        /// <summary>
        ///     Get the <see cref="ClientFile" /> instance where this script is loaded from.
        /// </summary>
        public ClientFile CurrentFile { get; }

        /// <summary>
        ///     Get the <see cref="ClientResource" /> that this script belongs to.
        /// </summary>
        public ClientResource CurrentResource { get; }

        /// <summary>
        ///     Eqivalent of <see cref="ClientResource.Logger" /> in <see cref="CurrentResource" />
        /// </summary>
        public ResourceLogger Logger => CurrentResource.Logger;
    }
}
