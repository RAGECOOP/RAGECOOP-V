using System;
using System.Collections.Concurrent;
using System.IO;
using System.Windows.Forms;
using GTA;
using Console = GTA.Console;
namespace RageCoop.Client.Loader
{
    public class Main : Script
    {
        static readonly string GameDir = Directory.GetParent(typeof(SHVDN.ScriptDomain).Assembly.Location).FullName;

        static readonly string ScriptsLocation = Path.Combine(GameDir, "RageCoop", "Scripts");
        private static readonly ConcurrentQueue<Action> TaskQueue = new ConcurrentQueue<Action>();
        public Main()
        {
            Tick += OnTick;
            SHVDN.ScriptDomain.CurrentDomain.Tick += DomainTick;
            KeyDown += OnKeyDown;
        }

        private void OnKeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
        }

        private void OnTick(object sender, EventArgs e)
        {
            while (Game.IsLoading)
            {
                Script.Yield();
            }
            DomainContext.CheckForUnloadRequest();
            if (!DomainContext.IsLoaded(ScriptsLocation))
            {
                if (!File.Exists(Path.Combine(ScriptsLocation, "RageCoop.Client.dll")))
                {
                    GTA.UI.Notification.Show("~r~Main assembly is missing, please re-install the client");
                    Abort();
                }
                DomainContext.Load(ScriptsLocation);
            }
        }

        internal static void QueueToMainThread(Action task)
        {
            TaskQueue.Enqueue(task);
        }

        private static void DomainTick(object sender, EventArgs e)
        {
            while (TaskQueue.TryDequeue(out var task))
            {
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
}
