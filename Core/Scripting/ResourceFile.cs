using System;
using System.IO;
using System.Text.Json.Serialization;

namespace RageCoop.Core.Scripting
{
    /// <summary>
    /// </summary>
    public class ResourceFile
    {
        /// <summary>
        ///     Full name with relative path of this file
        /// </summary>
        [JsonInclude]
        public string Name { get; internal set; }

        /// <summary>
        ///     Whether this is a directory
        /// </summary>
        [JsonInclude]
        public bool IsDirectory { get; internal set; }

        /// <summary>
        ///     Get a stream that can be used to read file content.
        /// </summary>
        [JsonIgnore]
        public Func<Stream> GetStream { get; internal set; }
    }
}