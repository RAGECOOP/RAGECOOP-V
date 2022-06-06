using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RageCoop.Core.Scripting;
using System.IO;
namespace RageCoop.Server.Scripting
{
    internal class Engine:ScriptingEngine
    {
        public Engine() : base("RageCoop.Server.Scripting.ServerScript", Program.Logger)
        {
        }
        public void LoadAll()
        {
            var path = Path.Combine("Resources", "Server");
            Directory.CreateDirectory(path);
            foreach (var resource in Directory.GetDirectories(path))
            {
                Logger.Info($"Loading resource: {Path.GetFileName(resource)}");
                LoadResource(resource);
            }
        }
        private void LoadResource(string path)
        {
            foreach(var assembly in Directory.GetFiles(path,"*.dll",SearchOption.AllDirectories))
            {
                LoadScriptsFromAssembly(assembly);
            }
        }
    }
}
