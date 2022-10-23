using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using GTA;
using GTA.UI;
using SHVDN;
using Console = GTA.Console;
using Script = GTA.Script;

namespace RageCoop.Client.Loader
{
    public class Main : Script
    {
        private static readonly string GameDir = Directory.GetParent(typeof(ScriptDomain).Assembly.Location).FullName;
        private static readonly string ScriptsLocation = Path.Combine(GameDir, "RageCoop", "Scripts");
        private static readonly ConcurrentQueue<Action> TaskQueue = new ConcurrentQueue<Action>();
        private static int MainThreadID;

        public Main()
        {
            if (LoaderContext.PrimaryDomain != null)
                throw new InvalidOperationException(
                    "Improperly placed loader assembly, please re-install to fix this issue");
            Tick += OnTick;
            ScriptDomain.CurrentDomain.Tick += DomainTick;
            Aborted += (s, e) => LoaderContext.UnloadAll();
        }

        private void OnTick(object sender, EventArgs e)
        {
            while (Game.IsLoading) Yield();
            LoaderContext.CheckForUnloadRequest();
            if (!LoaderContext.IsLoaded(ScriptsLocation))
            {
                if (!File.Exists(Path.Combine(ScriptsLocation, "RageCoop.Client.dll")))
                {
                    Notification.Show("~r~Main assembly is missing, please re-install the client");
                    Abort();
                }

                LoaderContext.Load(ScriptsLocation);
            }
        }

        internal static void QueueToMainThread(Action task)
        {
            if (Thread.CurrentThread.ManagedThreadId != MainThreadID)
                TaskQueue.Enqueue(task);
            else
                task();
        }

        private static void DomainTick()
        {
            if (MainThreadID == default) MainThreadID = Thread.CurrentThread.ManagedThreadId;
            while (TaskQueue.TryDequeue(out var task))
                try
                {
                    task.Invoke();
                }
                catch (Exception ex)
                {
                    Console.Error(ex.ToString());
                }
        }
    }
}