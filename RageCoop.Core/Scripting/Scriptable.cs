using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Linq;
namespace RageCoop.Core.Scripting
{
	public abstract class Scriptable
	{
		public abstract void OnStart();
		public abstract void OnStop();

		/// <summary>
		/// Get the <see cref="ResourceFile"/> instance where this script is loaded from.
		/// </summary>
		public ResourceFile CurrentFile { get; internal set; }

		/// <summary>
		/// Get the <see cref="Resource"/> object this script belongs to, this property will be initiated before <see cref="OnStart"/> (will be null if you access it in the constructor).
		/// </summary>
		public Resource CurrentResource { get; internal set; }
	}
}
