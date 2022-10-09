using ICSharpCode.SharpZipLib.Zip;
using RageCoop.Core;
using RageCoop.Core.Scripting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace RageCoop.Client.Scripting
{
    /// <summary>
    /// 
    /// </summary>
    public class ClientResource
    {
        /// <summary>
        /// Name of the resource
        /// </summary>
        public string Name { get; internal set; }
        public string BaseDirectory { get; internal set; }
        /// <summary>
        /// A resource-specific folder that can be used to store your files.
        /// </summary>
        public string DataFolder => Path.Combine(BaseDirectory, "Data");
        /// <summary>
        /// Get all <see cref="ClientScript"/> instance in this resource.
        /// </summary>
        public List<ClientScript> Scripts { get; internal set; } = new List<ClientScript>();
        /// <summary>
        /// Get the <see cref="ResourceFile"/> where this script is loaded from.
        /// </summary>
        public Dictionary<string, ResourceFile> Files { get; internal set; } = new Dictionary<string, ResourceFile>();

        /// <summary>
        /// A <see cref="Core.Logger"/> instance that can be used to debug your resource.
        /// </summary>
        public Logger Logger { get; internal set; }
    }
    internal class Resources
    {
        static readonly API API = Main.API;
        internal readonly ConcurrentDictionary<string, ClientResource> LoadedResources = new ConcurrentDictionary<string, ClientResource>();
        private const string BaseScriptType = "RageCoop.Client.Scripting.ClientScript";
        private Logger Logger { get; set; }
        public Resources()
        {
            Logger = Main.Logger;
        }
        public void Load(string path, string[] zips)
        {
            LoadedResources.Clear();
            foreach (var zip in zips)
            {
                var zipPath = Path.Combine(path, zip);
                Logger?.Info($"Loading resource: {Path.GetFileNameWithoutExtension(zip)}");
                Unpack(zipPath, Path.Combine(path, "Data"));
            }
            ResourceDomain.Load(path);
        }
        public void Unload()
        {
            ResourceDomain.UnloadAll();
        }

        private void Unpack(string zipPath, string dataFolderRoot)
        {
            var r = new ClientResource()
            {
                Logger = Main.API.Logger,
                Scripts = new List<ClientScript>(),
                Name = Path.GetFileNameWithoutExtension(zipPath),
                BaseDirectory = Path.Combine(Directory.GetParent(zipPath).FullName, Path.GetFileNameWithoutExtension(zipPath))
            };
            Directory.CreateDirectory(r.DataFolder);
            var resDir = r.BaseDirectory;
            if (Directory.Exists(resDir)) { Directory.Delete(resDir, true); }
            else if (File.Exists(resDir)) { File.Delete(resDir); }
            Directory.CreateDirectory(resDir);

            new FastZip().ExtractZip(zipPath, resDir, null);


            foreach (var dir in Directory.GetDirectories(resDir, "*", SearchOption.AllDirectories))
            {
                r.Files.Add(dir, new ResourceFile()
                {
                    IsDirectory = true,
                    Name = dir.Substring(resDir.Length + 1).Replace('\\', '/')
                });
            }
            var assemblies = new Dictionary<ResourceFile, Assembly>();
            foreach (var file in Directory.GetFiles(resDir, "*", SearchOption.AllDirectories))
            {
                if (Path.GetFileName(file).CanBeIgnored()) { try { File.Delete(file); } catch { } continue; }
                var relativeName = file.Substring(resDir.Length + 1).Replace('\\', '/');
                var rfile = new ResourceFile()
                {
                    GetStream = () => { return new FileStream(file, FileMode.Open, FileAccess.Read); },
                    IsDirectory = false,
                    Name = relativeName
                };
                r.Files.Add(relativeName, rfile);
            }

            LoadedResources.TryAdd(r.Name.ToLower(), r);
        }
    }

}
