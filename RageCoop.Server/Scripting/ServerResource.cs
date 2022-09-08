using ICSharpCode.SharpZipLib.Zip;
using McMaster.NETCore.Plugins;
using RageCoop.Core;
using RageCoop.Core.Scripting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace RageCoop.Server.Scripting
{
    /// <summary>
    /// A class representing a server side resource, each resource is isolated from another and will be started alongside the server.
    /// </summary>
    public class ServerResource : PluginLoader
    {

        internal ServerResource(PluginConfig config) : base(config) { }
        internal static ServerResource LoadFrom(string resDir, string dataFolder, Logger logger = null)
        {
            var runtimeLibs = Path.Combine(resDir, "RuntimeLibs", CoreUtils.GetInvariantRID());
            if (Directory.Exists(runtimeLibs))
            {
                logger?.Debug("Applying runtime libraries from " + CoreUtils.GetInvariantRID());
                CoreUtils.CopyFilesRecursively(new(runtimeLibs), new(resDir));
            }

            runtimeLibs = Path.Combine(resDir, "RuntimeLibs", RuntimeInformation.RuntimeIdentifier);
            if (Directory.Exists(runtimeLibs))
            {
                logger?.Debug("Applying runtime libraries from " + CoreUtils.GetInvariantRID());
                CoreUtils.CopyFilesRecursively(new(runtimeLibs), new(resDir));
            }

            var conf = new PluginConfig(Path.GetFullPath(Path.Combine(resDir, Path.GetFileName(resDir) + ".dll")))
            {
                PreferSharedTypes = true,
                EnableHotReload = false,
                IsUnloadable = false,
                LoadInMemory = true,
            };
            ServerResource r = new(conf);
            r.Logger = logger;
            r.Name = Path.GetFileName(resDir);
            if (!File.Exists(conf.MainAssemblyPath))
            {
                r.Dispose();
                throw new FileNotFoundException($"Main assembly for resource \"{r.Name}\" cannot be found.");
            }
            r.Scripts = new();
            r.DataFolder = Path.Combine(dataFolder, r.Name);
            r.Reloaded += (s, e) => { r.Logger?.Info($"Resource: {r.Name} has been reloaded"); };

            Directory.CreateDirectory(r.DataFolder);
            foreach (var dir in Directory.GetDirectories(resDir, "*", SearchOption.AllDirectories))
            {
                r.Files.Add(dir, new ResourceFile()
                {
                    IsDirectory = true,
                    Name = dir.Substring(resDir.Length + 1).Replace('\\', '/')
                }); ;
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
                if (file.EndsWith(".dll") && !relativeName.Contains('/') && IsManagedAssembly(file))
                {
                    assemblies.Add(rfile, r.LoadAssemblyFromPath(Path.GetFullPath(file)));
                }
                r.Files.Add(relativeName, rfile);
            }
            foreach (var a in assemblies)
            {
                if (a.Key.Name.ToLower() == r.Name.ToLower() + ".dll")
                {

                    try
                    {
                        r.LoadScriptsFromAssembly(a.Key, a.Value);
                    }
                    catch (FileLoadException ex)
                    {
                        if (!ex.Message.EndsWith("Assembly with same name is already loaded"))
                        {
                            logger?.Warning("Failed to load assembly: " + a.Key.Name);
                            logger?.Trace(ex.Message);
                        }
                    }
                }
            }
            return r;
        }
        internal static ServerResource LoadFrom(Stream input, string name, string tmpDir, string dataFolder, Logger logger = null)
        {
            tmpDir = Path.Combine(tmpDir, name);
            if (Directory.Exists(tmpDir)) { Directory.Delete(tmpDir, true); }
            Directory.CreateDirectory(tmpDir);
            new FastZip().ExtractZip(input, tmpDir, FastZip.Overwrite.Always, null, null, null, true, true);
            return LoadFrom(tmpDir, dataFolder, logger);
        }
        /// <summary>
        /// Name of the resource
        /// </summary>
        public string Name { get; internal set; }
        /// <summary>
        /// A resource-specific folder that can be used to store your files.
        /// </summary>
        public string DataFolder { get; internal set; }
        /// <summary>
        /// Get all <see cref="ServerScript"/> instance in this resource
        /// </summary>
        public Dictionary<string, ServerScript> Scripts { get; internal set; } = new();
        /// <summary>
        /// Get all <see cref="ResourceFile"/> that can be used to acces files in this resource
        /// </summary>
        public Dictionary<string, ResourceFile> Files { get; internal set; } = new Dictionary<string, ResourceFile>();
        /// <summary>
        /// Get a <see cref="Logger"/> instance that can be used to show information in console.
        /// </summary>
        public Logger Logger;
        private bool LoadScriptsFromAssembly(ResourceFile rfile, Assembly assembly)
        {
            int count = 0;

            try
            {
                // Find all script types in the assembly
                foreach (var type in assembly.GetTypes().Where(x => x.IsSubclassOf(typeof(ServerScript))))
                {
                    ConstructorInfo constructor = type.GetConstructor(System.Type.EmptyTypes);
                    if (constructor != null && constructor.IsPublic)
                    {
                        try
                        {
                            // Invoke script constructor
                            var script = constructor.Invoke(null) as ServerScript;
                            script.CurrentResource = this;
                            script.CurrentFile = rfile;
                            Scripts.Add(script.GetType().FullName, script);
                            count++;
                        }
                        catch (Exception ex)
                        {
                            Logger?.Error($"Error occurred when loading script: {type.FullName}.");
                            Logger?.Error(ex);
                        }
                    }
                    else
                    {
                        Logger?.Error($"Script {type.FullName} has an invalid contructor.");
                    }
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                Logger?.Error($"Failed to load assembly {rfile.Name}: ");
                Logger?.Error(ex);
                foreach (var e in ex.LoaderExceptions)
                {
                    Logger?.Error(e);
                }
                return false;
            }
            if (count != 0)
            {
                Logger?.Info($"Loaded {count} script(s) in {rfile.Name}");
            }
            return count != 0;
        }

        internal new void Dispose()
        {
            base.Dispose();
        }
        private static bool IsManagedAssembly(string filename)
        {
            try
            {
                using (Stream file = new FileStream(filename, FileMode.Open, FileAccess.Read))
                {
                    if (file.Length < 64)
                        return false;

                    using (BinaryReader bin = new BinaryReader(file))
                    {
                        // PE header starts at offset 0x3C (60). Its a 4 byte header.
                        file.Position = 0x3C;
                        uint offset = bin.ReadUInt32();
                        if (offset == 0)
                            offset = 0x80;

                        // Ensure there is at least enough room for the following structures:
                        //     24 byte PE Signature & Header
                        //     28 byte Standard Fields         (24 bytes for PE32+)
                        //     68 byte NT Fields               (88 bytes for PE32+)
                        // >= 128 byte Data Dictionary Table
                        if (offset > file.Length - 256)
                            return false;

                        // Check the PE signature. Should equal 'PE\0\0'.
                        file.Position = offset;
                        if (bin.ReadUInt32() != 0x00004550)
                            return false;

                        // Read PE magic number from Standard Fields to determine format.
                        file.Position += 20;
                        var peFormat = bin.ReadUInt16();
                        if (peFormat != 0x10b /* PE32 */ && peFormat != 0x20b /* PE32Plus */)
                            return false;

                        // Read the 15th Data Dictionary RVA field which contains the CLI header RVA.
                        // When this is non-zero then the file contains CLI data otherwise not.
                        file.Position = offset + (peFormat == 0x10b ? 232 : 248);
                        return bin.ReadUInt32() != 0;
                    }
                }
            }
            catch
            {
                // This is likely not a valid assembly if any IO exceptions occur during reading
                return false;
            }
        }
    }
}
