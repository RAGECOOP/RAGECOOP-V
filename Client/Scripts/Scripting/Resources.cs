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

namespace RageCoop.Client.Scripting
{
    internal class Resources
    {
        public static string TempPath;

        internal readonly ConcurrentDictionary<string, ClientResource> LoadedResources = new();

        static Resources()
        {
            TempPath = Path.Combine(Path.GetTempPath(), "RageCoop");
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
            HashSet<IntPtr> modules = new();
            foreach (var res in LoadedResources.Values)
            {
                foreach (var module in res.Modules)
                {
                    fixed (char* pModulePath = module)
                    {
                        Log.Debug($"Unloading module: {module}");
                        SHVDN.Core.ScheduleUnload(pModulePath);
                        var hModule = Util.GetModuleHandleW(module);
                        if (hModule == IntPtr.Zero)
                            Log.Warning("Failed to get module handler for " + Path.GetFileName(module));
                        else
                            modules.Add(hModule);
                    }
                }
            }

            // TODO
            /*
            // Unregister associated handler
            foreach (var handlers in API.CustomEventHandlers.Values)
            {
                foreach (var handler in handlers.ToArray())
                {
                    if (modules.Contains((IntPtr)handler.Module))
                    {
                        Log.Debug($"Unregister handler from module {handler.Module}");
                        handlers.Remove(handler);
                    }
                }
            }
            */
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
                if (Path.GetFileName(file).CanBeIgnored())
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(
                            $"Failed to delete API assembly: {file}. This may or may cause some unexpected behaviours.\n{ex}");
                    }

                    continue;
                }

                var relativeName = file.Substring(scriptsDir.Length + 1).Replace('\\', '/');
                var rfile = new ClientFile
                {
                    IsDirectory = false,
                    Name = relativeName,
                    FullPath = file
                };
                r.Files.Add(relativeName, rfile);
                if (file.EndsWith(".dll"))
                {
                    fixed (char* pModulePath = file)
                    {
                        SHVDN.Core.ScheduleLoad(pModulePath);
                        r.Modules.Add(file);
                    }
                }
            }

            LoadedResources.TryAdd(r.Name, r);
            return r;
        }
    }
}