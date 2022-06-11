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
        public static readonly ScriptingEngine Engine = new();
        /// <summary>
        /// Pack client-side resources as a zip file
        /// </summary>
        public static bool HasClientResources=false;

        public static void LoadAll()
        {
            var path = Path.Combine("Resources", "Client");
            Directory.CreateDirectory(path);
            var clientResources = Directory.GetDirectories(path);
            if (clientResources.Length!=0)
            {
                // Pack client side resources as a zip file
                Program.Logger.Info("Packing client-side resources");

                try
                {
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
                    HasClientResources=true;
                }
                catch(Exception ex)
                {
                    Program.Logger.Error("Failed to pack client resources");
                    Program.Logger.Error(ex);
                }
            }
            Engine.LoadAll();
        }
        public static void UnloadAll()
        {
            Engine.StopAll();
        }
        public static void SendTo(Client client)
        {

            string path;
            if (HasClientResources && File.Exists(path = Path.Combine("Resources", "Client", "Resources.zip")))
            {
                Task.Run(() =>
                {
                    Program.Logger.Info($"Sending resources to client:{client.Player.Username}");
                    Server.SendFile(path, "Resources.zip", client);
                    Program.Logger.Info($"Resources sent to:{client.Player.Username}");

                });
            }
        }
    }
}
