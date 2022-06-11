using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RageCoop.Core.Scripting;
using System.IO;
using System.Reflection;
namespace RageCoop.Server.Scripting
{
	internal class ScriptingEngine
	{
		public List<ServerScript> Scripts = new List<ServerScript>();
		protected List<string> ToIgnore = new List<string>
		{
			"RageCoop.Client.dll",
			"RageCoop.Core.dll",
			"RageCoop.Server.dll",
			"ScriptHookVDotNet3.dll"
		};
		private string BaseScriptType;
		public Core.Logging.Logger Logger { get; set; }
		public ScriptingEngine()
		{
			BaseScriptType = "RageCoop.Server.Scripting.ServerScript";
			Logger = Program.Logger;
		}
		/// <summary>
		/// Load all assemblies inside this directory.
		/// </summary>
		/// <param name="path">Path of the directory.</param>
		protected void LoadFromDirectory(string path)
		{
			foreach (var assembly in Directory.GetFiles(path, "*.dll", SearchOption.AllDirectories))
			{
				LoadScriptsFromAssembly(assembly);
			}
		}
		/// <summary>
		/// Loads scripts from the specified assembly file.
		/// </summary>
		/// <param name="path">The path to the assembly file to load.</param>
		/// <returns><see langword="true" /> on success, <see langword="false" /> otherwise</returns>
		protected bool LoadScriptsFromAssembly(string path)
		{
			if (!IsManagedAssembly(path)) { return false; }
			if (ToIgnore.Contains(Path.GetFileName(path))) { return false; }

			Logger?.Debug($"Loading assembly {Path.GetFileName(path)} ...");

			Assembly assembly;

			try
			{
				assembly = Assembly.LoadFrom(path);
			}
			catch (Exception ex)
			{
				Logger?.Error("Unable to load "+Path.GetFileName(path));
				Logger?.Error(ex);
				return false;
			}

			return LoadScriptsFromAssembly(assembly, path);
		}
		/// <summary>
		/// Loads scripts from the specified assembly object.
		/// </summary>
		/// <param name="filename">The path to the file associated with this assembly.</param>
		/// <param name="assembly">The assembly to load.</param>
		/// <returns><see langword="true" /> on success, <see langword="false" /> otherwise</returns>
		private bool LoadScriptsFromAssembly(Assembly assembly, string filename)
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
							Scripts.Add(constructor.Invoke(null) as ServerScript);
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
				Logger?.Error($"Failed to load assembly {Path.GetFileName(filename)}: ");
				Logger?.Error(ex);

				return false;
			}

			Logger?.Info($"Loaded {count} script(s) in {Path.GetFileName(filename)}");
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
		public void LoadAll()
		{
			var path = Path.Combine("Resources", "Server");
			Directory.CreateDirectory(path);
			foreach (var resource in Directory.GetDirectories(path))
			{
				Logger.Info($"Loading resource: {Path.GetFileName(resource)}");
				LoadFromDirectory(resource);
			}
            foreach (var s in Scripts)
            {
				s.OnStart();
            }
		}
		public void StopAll()
        {
			foreach (var s in Scripts)
			{
				s.OnStop();
			}
			Scripts.Clear();
		}
	}
}
