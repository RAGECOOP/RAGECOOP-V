using System;
using System.Collections.Generic;
using System.Linq;
using RageCoop.Core;
using System.Reflection;
using McMaster.NETCore.Plugins;
using System.IO;
using RageCoop.Core.Scripting;
using ICSharpCode.SharpZipLib.Zip;

namespace RageCoop.Server.Scripting
{
	/// <summary>
	/// A class representing a server side resource, each resource is isolated from another and will be started alongside the server.
	/// </summary>
	public class ServerResource : PluginLoader
	{
		private static readonly HashSet<string> ToIgnore = new()
		{
			"RageCoop.Client.dll",
			"RageCoop.Core.dll",
			"RageCoop.Server.dll",
			"ScriptHookVDotNet3.dll",
			"ScriptHookVDotNet.dll"
		};
		internal ServerResource(PluginConfig config) : base(config) { }
		internal static ServerResource LoadFrom(string resDir, string dataFolder, Logger logger = null, bool isTemp = false)
		{
			var conf = new PluginConfig(Path.GetFullPath(Path.Combine(resDir, Path.GetFileName(resDir)+".dll")))
			{
				PreferSharedTypes = true,
				EnableHotReload=false,
				IsUnloadable=false,
				LoadInMemory=true,
			};
			ServerResource r = new(conf);
			r.Logger= logger;
			r.Name=Path.GetFileName(resDir);
			if (!File.Exists(conf.MainAssemblyPath))
			{
				r.Dispose();
				throw new FileNotFoundException($"Main assembly for resource \"{r.Name}\" cannot be found.");
			}
			r.Scripts = new List<ServerScript>();
			r.DataFolder=Path.Combine(dataFolder, r.Name);
			r.Reloaded+=(s, e) => { r.Logger?.Info($"Resource: {r.Name} has been reloaded"); };

			Directory.CreateDirectory(r.DataFolder);
			foreach (var dir in Directory.GetDirectories(resDir, "*", SearchOption.AllDirectories))
			{
				r.Files.Add(dir, new ResourceFile()
				{
					IsDirectory=true,
					Name=dir.Substring(resDir.Length+1).Replace('\\','/')
				});;
			}
			foreach (var file in Directory.GetFiles(resDir, "*", SearchOption.AllDirectories))
			{
				if (ToIgnore.Contains(Path.GetFileName(file))) { try { File.Delete(file); } catch { } continue; }
				var relativeName = file.Substring(resDir.Length+1).Replace('\\', '/');
				var rfile = new ResourceFile()
				{
					GetStream=() => { return new FileStream(file, FileMode.Open, FileAccess.Read); },
					IsDirectory=false,
					Name=relativeName
				};
				if (file.EndsWith(".dll") && IsManagedAssembly(file))
				{
                    try
                    {
						r.LoadScriptsFromAssembly(rfile, r.LoadAssemblyFromPath(Path.GetFullPath(file)));
					}
					catch(FileLoadException ex)
                    {
						if(!ex.Message.EndsWith("Assembly with same name is already loaded"))
                        {
							logger?.Warning("Failed to load assembly: "+Path.GetFileName(file));
							logger?.Trace(ex.Message);
                        }
                    }
				}
				r.Files.Add(relativeName, rfile);
			}
			return r;
		}
		internal static ServerResource LoadFromZip(string zipPath, string tmpDir, string dataFolder, Logger logger = null)
		{
			tmpDir=Path.Combine(tmpDir, Path.GetFileNameWithoutExtension(zipPath));
			new FastZip().ExtractZip(zipPath, tmpDir, null);
			return LoadFrom(tmpDir, dataFolder, logger, true);
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
		public List<ServerScript> Scripts { get; internal set; } = new List<ServerScript>();
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
							script.CurrentFile=rfile;
							Scripts.Add(script);
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
			if(count != 0)
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
