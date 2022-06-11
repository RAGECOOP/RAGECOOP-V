using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ionic.Zip;
using System.IO;

namespace RageCoop.Client
{
    internal static class Resources
    {
        static Scripting.Engine ScriptingEngine = new Scripting.Engine();
        /// <summary>
        /// Load all resources from a server
        /// </summary>
        /// <param name="path">The path to the directory containing the resources.</param>
        public static void Load(string path)
        {
            ScriptingEngine.StopAll();
            foreach(var d in Directory.GetDirectories(path))
            {
                Directory.Delete(d, true);
            }
            using (var zip = ZipFile.Read(Path.Combine(path, "Resources.zip")))
            {
                zip.ExtractAll(path, ExtractExistingFileAction.OverwriteSilently);
            }
            ScriptingEngine.LoadAll(path);
        }
        public static void UnloadAll()
        {
            ScriptingEngine.StopAll();
        }
    }
}
