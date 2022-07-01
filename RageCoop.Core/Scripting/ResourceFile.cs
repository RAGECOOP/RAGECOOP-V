using System;
using System.Collections.Generic;
using System.IO;

namespace RageCoop.Core.Scripting
{
	public class ResourceFile
    {
		public string Name { get; internal set; }
		public bool IsDirectory { get; internal set; }
		public Func<Stream> GetStream { get; internal set; }
    }
}
