using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ICSharpCode.SharpZipLib.Zip;
using RageCoop.Client.Loader;
using RageCoop.Core;
using RageCoop.Core.Scripting;
using SHVDN;

namespace RageCoop.Client.Scripting
{
    /// <summary>
    /// </summary>
    public class ClientResource
    {
        /// <summary>
        ///     Name of the resource
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        ///     Directory where the scripts is loaded from
        /// </summary>
        public string ScriptsDirectory { get; internal set; }

        /// <summary>
        ///     A resource-specific folder that can be used to store your files.
        /// </summary>
        public string DataFolder { get; internal set; }

        /// <summary>
        ///     Get all <see cref="ClientScript" /> instance in this resource.
        /// </summary>
        public List<ClientScript> Scripts { get; internal set; } = new List<ClientScript>();

        /// <summary>
        ///     Get the <see cref="ResourceFile" /> where this script is loaded from.
        /// </summary>
        public Dictionary<string, ResourceFile> Files { get; internal set; } = new Dictionary<string, ResourceFile>();

        /// <summary>
        ///     A <see cref="Core.Logger" /> instance that can be used to debug your resource.
        /// </summary>
        public Logger Logger { get; internal set; }
    }

    internal class Resources
    {
        public static string TempPath;

        internal readonly ConcurrentDictionary<string, ClientResource> LoadedResources =
            new ConcurrentDictionary<string, ClientResource>();

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

        public Resources()
        {
            Logger = Main.Logger;
        }

        private Logger Logger { get; }

        public void Load(string path, string[] zips)
        {
            LoadedResources.Clear();
            foreach (var zip in zips)
            {
                var zipPath = Path.Combine(path, zip);
                Logger?.Info($"Loading resource: {Path.GetFileNameWithoutExtension(zip)}");
                Unpack(zipPath, Path.Combine(path, "Data"));
            }

            Directory.GetFiles(path, "*.dll", SearchOption.AllDirectories).Where(x => x.CanBeIgnored())
                .ForEach(x => File.Delete(x));

            // Load it in main thread
            API.QueueActionAndWait(() =>
            {
                Main.QueueToMainThreadAndWait(() =>
                {
                    foreach (var res in LoadedResources)
                        Directory.GetFiles(res.Value.ScriptsDirectory, "*.dll", SearchOption.AllDirectories)
                            .ForEach(x => ScriptDomain.CurrentDomain.StartScripts(x));
                    SetupScripts();
                });
            });
        }

        public void Unload()
        {
            StopScripts();
            LoadedResources.Clear();
            LoaderContext.RequestUnload();
        }

        private ClientResource Unpack(string zipPath, string dataFolderRoot)
        {
            var r = new ClientResource
            {
                Logger = API.Logger,
                Scripts = new List<ClientScript>(),
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
                r.Files.Add(dir, new ResourceFile
                {
                    IsDirectory = true,
                    Name = dir.Substring(scriptsDir.Length + 1).Replace('\\', '/')
                });
            var assemblies = new Dictionary<ResourceFile, Assembly>();
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
                        API.Logger.Warning(
                            $"Failed to delete API assembly: {file}. This may or may cause some unexpected behaviours.\n{ex}");
                    }

                    continue;
                }

                var relativeName = file.Substring(scriptsDir.Length + 1).Replace('\\', '/');
                var rfile = new ResourceFile
                {
                    GetStream = () => { return new FileStream(file, FileMode.Open, FileAccess.Read); },
                    IsDirectory = false,
                    Name = relativeName
                };
                r.Files.Add(relativeName, rfile);
            }

            LoadedResources.TryAdd(r.Name, r);
            return r;
        }

        private void SetupScripts()
        {
            foreach (var s in GetClientScripts())
            {
                try
                {
                    API.Logger.Debug("Starting script: " + s.GetType().FullName);
                    var script = (ClientScript)s;
                    if (LoadedResources.TryGetValue(Directory.GetParent(script.Filename).Name, out var r))
                        script.CurrentResource = r;
                    else
                        API.Logger.Warning("Failed to locate resource for script: " + script.Filename);
                    var res = script.CurrentResource;
                    script.CurrentFile = res?.Files.Values.Where(x =>
                        x.Name.ToLower() == script.Filename.Substring(res.ScriptsDirectory.Length + 1)
                            .Replace('\\', '/')).FirstOrDefault();
                    res?.Scripts.Add(script);
                    script.OnStart();
                }
                catch (Exception ex)
                {
                    API.Logger.Error($"Failed to start {s.GetType().FullName}", ex);
                }

                API.Logger.Debug("Started script: " + s.GetType().FullName);
            }
        }

        private void StopScripts()
        {
            foreach (var s in GetClientScripts())
                try
                {
                    API.Logger.Debug("Stopping script: " + s.GetType().FullName);
                    ((ClientScript)s).OnStop();
                }
                catch (Exception ex)
                {
                    API.Logger.Error($"Failed to stop {s.GetType().FullName}", ex);
                }
        }

        public static object[] GetClientScripts()
        {
            return ScriptDomain.CurrentDomain.RunningScripts.Where(x =>
                x.ScriptInstance.GetType().IsScript(typeof(ClientScript))).Select(x => x.ScriptInstance).ToArray();
        }
    }
}