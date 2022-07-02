using System.IO;
using RageCoop.Core.Scripting;
using RageCoop.Core;
using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;

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
		/// <summary>
		/// A resource-specific folder that can be used to store your files.
		/// </summary>
		public string DataFolder { get; internal set; }
		/// <summary>
		/// Get all <see cref="ClientScript"/> instance in this resource.
		/// </summary>
		public List<ClientScript> Scripts { get; internal set; } = new List<ClientScript>();
		/// <summary>
		/// Get the <see cref="ResourceFile"/> where this script is loaded from.
		/// </summary>
		public Dictionary<string, ResourceFile> Files { get; internal set; } = new Dictionary<string, ResourceFile>();
	}
	internal class Resources
	{
		public Resources(){
			BaseScriptType = "RageCoop.Client.Scripting.ClientScript";
			Logger = Main.Logger;
		}
		private void StartAll()
		{
			lock (LoadedResources)
			{
				foreach (var d in LoadedResources)
				{
					foreach (var s in d.Scripts)
					{
                        try
                        {
							s.OnStart();
						}
						catch(Exception ex)
                        {
							Logger.Error("Error occurred when starting script:"+s.GetType().FullName);
							Logger?.Error(ex);
                        }
					}
				}
			}
		}
		private void StopAll()
		{
			lock (LoadedResources)
			{
				foreach (var d in LoadedResources)
				{
					foreach (var s in d.Scripts)
					{
						try
						{
							s.OnStop();
						}
						catch (Exception ex)
						{
							Logger.Error("Error occurred when stopping script:"+s.GetType().FullName);
							Logger?.Error(ex);
						}
					}
				}
			}
		}
		/// <summary>
		/// Load all resources from the server
		/// </summary>
		/// <param name="path">The path to the directory containing all resources to load.</param>
		public void Load(string path)
		{
			Unload();
			foreach (var d in Directory.GetDirectories(path))
			{
				if(Path.GetFileName(d).ToLower() != "data")
				{
					Directory.Delete(d, true);
				}
			}
			Directory.CreateDirectory(path);
			foreach (var zipPath in Directory.GetFiles(path,"*.zip",SearchOption.TopDirectoryOnly))
			{
				Logger?.Info($"Loading resource: {Path.GetFileNameWithoutExtension(zipPath)}");
				LoadResource(new ZipFile(zipPath),Path.Combine(path,"data"));
			}
			StartAll();
		}
		public void Unload()
        {
			StopAll();
			LoadedResources.Clear();
        }
		private List<string> ToIgnore = new List<string>
		{
			"RageCoop.Client.dll",
			"RageCoop.Core.dll",
			"RageCoop.Server.dll",
			"ScriptHookVDotNet3.dll"
		};
		private List<ClientResource> LoadedResources = new List<ClientResource>();
		private string BaseScriptType;
		public Logger Logger { get; set; }

		private void LoadResource(ZipFile file, string dataFolderRoot)
		{
			var r = new ClientResource()
			{
				Scripts = new List<ClientScript>(),
				Name=Path.GetFileNameWithoutExtension(file.Name),
				DataFolder=Path.Combine(dataFolderRoot, Path.GetFileNameWithoutExtension(file.Name))
			};
			Directory.CreateDirectory(r.DataFolder);

			foreach (ZipEntry entry in file)
			{
				ResourceFile rFile;
				r.Files.Add(entry.Name, rFile=new ResourceFile()
				{
					Name=entry.Name,
					IsDirectory=entry.IsDirectory,
				});
				if (!entry.IsDirectory)
				{
					rFile.GetStream=() => { return file.GetInputStream(entry); };
					if (entry.Name.EndsWith(".dll"))
					{
						var tmp = Path.GetTempFileName();
						var f = File.OpenWrite(tmp);
						rFile.GetStream().CopyTo(f);
						f.Close();
						LoadScriptsFromAssembly(rFile, tmp, r, false);
					}
				}
			}
			LoadedResources.Add(r);
		}
		private bool LoadScriptsFromAssembly(ResourceFile file, string path, ClientResource resource, bool shadowCopy = true)
		{
			lock (LoadedResources)
			{
				if (!IsManagedAssembly(path)) { return false; }
				if (ToIgnore.Contains(file.Name)) { try { File.Delete(path); } catch { }; return false; }

				Logger?.Debug($"Loading assembly {file.Name} ...");

				Assembly assembly;

				try
				{
					if (shadowCopy)
					{
						var temp = Path.GetTempFileName();
						File.Copy(path, temp, true);
						assembly = Assembly.LoadFrom(temp);
					}
					else
					{
						assembly = Assembly.LoadFrom(path);
					}
				}
				catch (Exception ex)
				{
					Logger?.Error("Unable to load "+file.Name);
					Logger?.Error(ex);
					return false;
				}

				return LoadScriptsFromAssembly(file, assembly, path, resource);
			}
		}
		private bool LoadScriptsFromAssembly(ResourceFile rfile, Assembly assembly, string filename, ClientResource toload)
		{
			int count = 0;

			try
			{
				// Find all script types in the assembly
				foreach (var type in assembly.GetTypes().Where(x => IsSubclassOf(x, BaseScriptType)))
				{
					ConstructorInfo constructor = type.GetConstructor(System.Type.EmptyTypes);
					if (constructor != null && constructor.IsPublic)
					{
						try
						{
							// Invoke script constructor
							var script = constructor.Invoke(null) as ClientScript;
							// script.CurrentResource = toload;
							script.CurrentFile=rfile;
							script.CurrentResource=toload;
							toload.Scripts.Add(script);
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
		private bool IsManagedAssembly(string filename)
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
		private bool IsSubclassOf(Type type, string baseTypeName)
		{
			for (Type t = type.BaseType; t != null; t = t.BaseType)
				if (t.FullName == baseTypeName)
					return true;
			return false;
		}
	}

}
