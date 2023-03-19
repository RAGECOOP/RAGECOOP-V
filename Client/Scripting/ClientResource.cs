using RageCoop.Core.Scripting;
using SHVDN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace RageCoop.Client.Scripting
{
    /// <summary>
    /// </summary>
    public class ClientResource
    {
        /// <summary>
        ///     Name of the resource
        /// </summary>
        [JsonInclude]
        public string Name { get; internal set; }

        /// <summary>
        ///     Directory where the scripts is loaded from
        /// </summary>
        [JsonInclude]
        public string ScriptsDirectory { get; internal set; }

        /// <summary>
        ///     A resource-specific folder that can be used to store your files.
        /// </summary>
        [JsonInclude]
        public string DataFolder { get; internal set; }

        /// <summary>
        ///     Get the <see cref="ClientFile" /> where this script is loaded from.
        /// </summary>
        [JsonInclude]
        public Dictionary<string, ClientFile> Files { get; internal set; } = new Dictionary<string, ClientFile>();

        /// <summary>
        ///     A <see cref="Core.Logger" /> instance that can be used to debug your resource.
        /// </summary>
        [JsonIgnore]
        public ResourceLogger Logger => ResourceLogger.Default;

    }

}
