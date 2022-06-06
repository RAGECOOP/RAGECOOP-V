using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RageCoop.Server.Scripting;
using System.IO;
using Ionic.Zip;

namespace RageCoop.Server
{
    internal static class Resources
    {
        public static readonly Engine ScriptingEngine = new();
        /// <summary>
        /// Pack client-side resources as a zip file
        /// </summary>
        public static void PackClientFiles()
        {
            Program.Logger.Info("Packing client side resources");
        }

        public static void LoadAll()
        {
            var path = Path.Combine("Resources", "Client");
            Directory.CreateDirectory(path);
            var clientResources = Directory.GetDirectories(path);
            if (clientResources.Length!=0)
            {
                // Pack client side resources as a zip file
                Program.Logger.Info("Packing client-side resources");

                var zippath = Path.Combine(path, "Resources.zip");
                if (File.Exists(zippath))
                {
                    File.Delete(zippath);
                }
                using (ZipFile zip = new ZipFile())
                {
                    zip.AddDirectory(path);
                    zip.Save(zippath);
                }
            }
            ScriptingEngine.LoadAll();
        }
    }
}
