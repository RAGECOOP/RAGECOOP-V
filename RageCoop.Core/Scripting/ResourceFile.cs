using System;
using System.IO;

namespace RageCoop.Core.Scripting
{
    /// <summary>
    /// 
    /// </summary>
    public class ResourceFile
    {
        /// <summary>
        /// Full name with relative path of this file
        /// </summary>
        public string Name { get; internal set; }
        /// <summary>
        /// Whether this is a directory
        /// </summary>
        public bool IsDirectory { get; internal set; }
        /// <summary>
        /// Get a stream that can be used to read file content.
        /// </summary>
        public Func<Stream> GetStream { get; internal set; }
    }
}
