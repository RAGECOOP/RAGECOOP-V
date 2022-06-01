using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using RageCoop.Core.Logging;
using System.Linq;

[assembly: InternalsVisibleTo("RageCoop.Server")]
[assembly: InternalsVisibleTo("RageCoop.Client")]
namespace RageCoop.Core.Scripting
{
    internal class ScriptingEngine
    {
		private Type BaseScriptType;
		public Logger Logger { get; set; }
        public ScriptingEngine(Type baseScriptType,Logger logger)
        {
			BaseScriptType = baseScriptType;
			Logger = logger;
        }
		/// <summary>
		/// Loads scripts from the specified assembly file.
		/// </summary>
		/// <param name="filename">The path to the assembly file to load.</param>
		/// <returns><see langword="true" /> on success, <see langword="false" /> otherwise</returns>
		private bool LoadScriptsFromAssembly(string filename)
		{
			if (!IsManagedAssembly(filename))
				return false;

			Logger?.Debug($"Loading assembly {Path.GetFileName(filename)} ...");

			Assembly assembly;

			try
			{
				assembly = Assembly.LoadFrom(filename);
			}
			catch (Exception ex)
			{
				Logger?.Error( "Unable to load "+Path.GetFileName(filename));
				Logger?.Error(ex);
				return false;
			}

			return LoadScriptsFromAssembly(assembly, filename);
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
				foreach (var type in assembly.GetTypes().Where(x => IsSubclassOf(x,nameof(BaseScriptType))))
				{
					ConstructorInfo constructor = type.GetConstructor(System.Type.EmptyTypes);
					if (constructor != null && constructor.IsPublic)
					{
						try
						{
							// Invoke script constructor
							constructor.Invoke(null);
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

			Logger?.Info($"Loaded {count.ToString()} script(s) in {Path.GetFileName(filename)}");
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
