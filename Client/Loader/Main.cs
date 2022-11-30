using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using GTA;
using GTA.UI;
using SHVDN;
using Console = GTA.Console;
using Script = GTA.Script;

namespace RageCoop.Client.Loader
{
    [ScriptAttributes(Author = "RageCoop", NoScriptThread = true,
        SupportURL = "https://github.com/RAGECOOP/RAGECOOP-V")]
    public class Main : Script
    {
        private static readonly string GameDir = Directory.GetParent(typeof(ScriptDomain).Assembly.Location).FullName;
        private static readonly string ScriptsLocation = Path.Combine(GameDir, "RageCoop", "Scripts");
        private bool _loaded;

        public Main()
        {
            if (LoaderContext.PrimaryDomain != null) return;
            Tick += OnTick;
            KeyDown += (s, e) => LoaderContext.KeyEventAll(e.KeyCode, true);
            KeyUp += (s, e) => LoaderContext.KeyEventAll(e.KeyCode, false);
            Aborted += (s, e) => LoaderContext.UnloadAll();
        }

        private void OnTick(object sender, EventArgs e)
        {
            if (!_loaded) { _loaded = !Game.IsLoading; return; }
            LoaderContext.CheckForUnloadRequest();
            if (!LoaderContext.IsLoaded(ScriptsLocation))
            {
                if (!File.Exists(Path.Combine(ScriptsLocation, "RageCoop.Client.dll")))
                {
                    Notification.Show("~r~Main assembly is missing, please re-install the client");
                    Abort();
                    return;
                }
                LoaderContext.Load(ScriptsLocation);
            }
            LoaderContext.TickAll();
        }
    }
}