using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace RageCoop.Core.Scripting
{
	public class Resource
	{
		/// <summary>
		/// Name of the resource
		/// </summary>
		public string Name { get; internal set; }
		/// <summary>
		/// A resource-specific folder that can be used to store your files.
		/// </summary>
		public string DataFolder { get;internal set; }
		public List<Scriptable> Scripts { get; internal set; } = new List<Scriptable>();
		public Dictionary<string,ResourceFile> Files { get; internal set; }=new Dictionary<string,ResourceFile>();
	}
	public class ResourceFile
    {
		public string Name { get; internal set; }
		public bool IsDirectory { get; internal set; }
		public Func<Stream> GetStream { get; internal set; }
    }
}
