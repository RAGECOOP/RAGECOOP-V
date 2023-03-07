using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using ICSharpCode.SharpZipLib.Zip;
using RageCoop.Core;
using RageCoop.Core.Scripting;
using SHVDN;

namespace RageCoop.Client.Scripting
{
    internal class Resources
    {
        public static string TempPath = Path.Combine(Path.GetTempPath(), "RageCoop");

        internal readonly ConcurrentDictionary<string, ClientResource> LoadedResources = new();

        static Resources()
        {
            if (Directory.Exists(TempPath))
                try
                {
                    Directory.Delete(TempPath, true);
                }
                catch
                {
                }

            TempPath = CoreUtils.GetTempDirectory(TempPath);
            Directory.CreateDirectory(TempPath);
        }


        public unsafe void Load(string path, string[] zips)
        {
            LoadedResources.Clear();
            foreach (var zip in zips)
            {
                var zipPath = Path.Combine(path, zip);
                Log?.Info($"Loading resource: {Path.GetFileNameWithoutExtension(zip)}");
                Unpack(zipPath, Path.Combine(path, "Data"));
            }
        }

        public unsafe void Unload()
        {
            var dirs = LoadedResources.Values.Select(x => x.ScriptsDirectory);
            foreach (var dir in dirs)
            {
                SHVDN.Core.RuntimeController.RequestUnload(dir);
            }

            // Unregister associated handler
            foreach (var handlers in API.CustomEventHandlers.Values)
            {
                foreach (var handler in handlers.ToArray())
                {
                    if (dirs.Contains(handler.Directory, StringComparer.OrdinalIgnoreCase))
                    {
                        handlers.Remove(handler);
                        Log.Debug($"Unregistered handler from script directory {handler.Directory}");
                    }
                }
            }
            LoadedResources.Clear();
        }

        private unsafe ClientResource Unpack(string zipPath, string dataFolderRoot)
        {
            var r = new ClientResource
            {
                Name = Path.GetFileNameWithoutExtension(zipPath),
                DataFolder = Path.Combine(dataFolderRoot, Path.GetFileNameWithoutExtension(zipPath)),
                ScriptsDirectory = Path.Combine(TempPath, Path.GetFileNameWithoutExtension(zipPath))
            };
            Directory.CreateDirectory(r.DataFolder);
            var scriptsDir = r.ScriptsDirectory;
            if (Directory.Exists(scriptsDir))
                Directory.Delete(scriptsDir, true);
            else if (File.Exists(scriptsDir)) File.Delete(scriptsDir);
            Directory.CreateDirectory(scriptsDir);

            new FastZip().ExtractZip(zipPath, scriptsDir, null);


            foreach (var dir in Directory.GetDirectories(scriptsDir, "*", SearchOption.AllDirectories))
                r.Files.Add(dir, new ClientFile
                {
                    IsDirectory = true,
                    Name = dir.Substring(scriptsDir.Length + 1).Replace('\\', '/'),
                    FullPath = dir
                });
            foreach (var file in Directory.GetFiles(scriptsDir, "*", SearchOption.AllDirectories))
            {
                var relativeName = file.Substring(scriptsDir.Length + 1).Replace('\\', '/');
                var rfile = new ClientFile
                {
                    IsDirectory = false,
                    Name = relativeName,
                    FullPath = file
                };
                r.Files.Add(relativeName, rfile);
            }
            SHVDN.Core.RuntimeController.RequestLoad(scriptsDir);
            LoadedResources.TryAdd(r.Name, r);
            return r;
        }
    }
}