using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using ICSharpCode.SharpZipLib.Zip;

[assembly: InternalsVisibleTo("RageCoop.Server")]
[assembly: InternalsVisibleTo("RageCoop.Client")]
namespace RageCoop.Core.Scripting
{
	
	internal class ResourceLoader
    {
		protected List<string> ToIgnore = new List<string>
		{
			"RageCoop.Client.dll",
			"RageCoop.Core.dll",
			"RageCoop.Server.dll",
			"ScriptHookVDotNet3.dll"
		};
		protected List<Resource> LoadedResources = new List<Resource>();
		private string BaseScriptType;
		public Logger Logger { get; set; }
		public ResourceLoader(string baseType,Logger logger)
		{
			BaseScriptType = baseType;
			Logger = logger;
		}
		/// <summary>
		/// Load a resource from a directory.
		/// </summary>
		/// <param name="path">Path of the directory.</param>
		protected void LoadResource(string path,string dataFolderRoot)
		{
			var r = new Resource()
			{
				Scripts = new List<Scriptable>(),
				Name=Path.GetFileName(path),
				DataFolder=Path.Combine(dataFolderRoot, Path.GetFileName(path))
			};
			Directory.CreateDirectory(r.DataFolder);
			foreach (var dir in Directory.GetDirectories(path, "*", SearchOption.AllDirectories))
			{
				r.Files.Add(dir, new ResourceFile()
				{
					IsDirectory=true,
					Name=dir.Substring(path.Length+1)
				});
			}
			foreach (var file in Directory.GetFiles(path,"*",SearchOption.AllDirectories))
			{
				var relativeName = file.Substring(path.Length+1);
				var rfile = new ResourceFile()
				{
					GetStream=() => { return new FileStream(file, FileMode.Open, FileAccess.Read); },
					IsDirectory=false,
					Name=relativeName
				};
				if (file.EndsWith(".dll"))
                {
					LoadScriptsFromAssembly(rfile,file, r);
				}
				r.Files.Add(relativeName,rfile);
			}
			LoadedResources.Add(r);
		}
		/// <summary>
		/// Load a resource from a zip
		/// </summary>
		/// <param name="file"></param>
		protected void LoadResource(ZipFile file,string dataFolderRoot)
		{
			var r = new Resource()
			{
				Scripts = new List<Scriptable>(),
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
		/// <summary>
		/// Loads scripts from the specified assembly file.
		/// </summary>
		/// <param name="path">The path to the assembly file to load.</param>
		/// <returns><see langword="true" /> on success, <see langword="false" /> otherwise</returns>
		private bool LoadScriptsFromAssembly(ResourceFile file,string path, Resource resource,bool shadowCopy=true)
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

				return LoadScriptsFromAssembly(file,assembly, path, resource);
			}
		}
		/// <summary>
		/// Loads scripts from the specified assembly object.
		/// </summary>
		/// <param name="filename">The path to the file associated with this assembly.</param>
		/// <param name="assembly">The assembly to load.</param>
		/// <returns><see langword="true" /> on success, <see langword="false" /> otherwise</returns>
		private bool LoadScriptsFromAssembly(ResourceFile rfile,Assembly assembly, string filename, Resource toload)
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
							var script = constructor.Invoke(null) as Scriptable;
							script.CurrentResource = toload;
							script.CurrentFile=rfile;
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
