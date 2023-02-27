using Newtonsoft.Json;
using RageCoop.Core.Scripting;
using SHVDN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
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
        [JsonProperty]
        public string Name { get; internal set; }

        /// <summary>
        ///     Directory where the scripts is loaded from
        /// </summary>
        [JsonProperty]
        public string ScriptsDirectory { get; internal set; }

        /// <summary>
        ///     A resource-specific folder that can be used to store your files.
        /// </summary>
        [JsonProperty]
        public string DataFolder { get; internal set; }

        /// <summary>
        ///     Get the <see cref="ClientFile" /> where this script is loaded from.
        /// </summary>
        [JsonProperty]
        public Dictionary<string, ClientFile> Files { get; internal set; } = new Dictionary<string, ClientFile>();

        /// <summary>
        /// List of the path of loaded modules, don't modify
        /// </summary>
        [JsonProperty]
        public List<string> Modules = new();

        /// <summary>
        ///     A <see cref="Core.Logger" /> instance that can be used to debug your resource.
        /// </summary>
        [JsonIgnore]
        public ResourceLogger Logger => ResourceLogger.Default;

    }

}
