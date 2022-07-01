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
		public Logger Logger;
		internal ServerResource(PluginConfig config) : base(config) { }
		internal static ServerResource LoadFrom(string resDir, string dataFolder, Logger logger = null, bool isTemp = false)
		{
			var conf = new PluginConfig(Path.GetFullPath(Path.Combine(resDir, Path.GetFileName(resDir)+".dll")))
			{
				PreferSharedTypes = true,
				EnableHotReload=!isTemp,
				IsUnloadable=true,
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
					Name=dir.Substring(resDir.Length+1)
				});
			}
			foreach (var file in Directory.GetFiles(resDir, "*", SearchOption.AllDirectories))
			{
				if (ToIgnore.Contains(Path.GetFileName(file))) { try { File.Delete(file); } catch { } continue; }
				var relativeName = file.Substring(resDir.Length+1);
				var rfile = new ResourceFile()
				{
					GetStream=() => { return new FileStream(file, FileMode.Open, FileAccess.Read); },
					IsDirectory=false,
					Name=relativeName
				};
				if (file.EndsWith(".dll"))
				{
					r.LoadScriptsFromAssembly(rfile, r.LoadAssemblyFromPath(Path.GetFullPath(file)));
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
		public List<ServerScript> Scripts { get; internal set; } = new List<ServerScript>();
		public Dictionary<string, ResourceFile> Files { get; internal set; } = new Dictionary<string, ResourceFile>();

		/// <summary>
		/// Loads scripts from the specified assembly object.
		/// </summary>
		/// <param name="filename">The path to the file associated with this assembly.</param>
		/// <param name="assembly">The assembly to load.</param>
		/// <returns><see langword="true" /> on success, <see langword="false" /> otherwise</returns>
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

			Logger?.Info($"Loaded {count} script(s) in {rfile.Name}");
			return count != 0;
		}
		public new void Dispose()
		{
			base.Dispose();
		}

	}
}
