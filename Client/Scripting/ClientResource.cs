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
        public string Name { get; internal set; }

        /// <summary>
        ///     Directory where the scripts is loaded from
        /// </summary>
        public string ScriptsDirectory { get; internal set; }

        /// <summary>
        ///     A resource-specific folder that can be used to store your files.
        /// </summary>
        public string DataFolder { get; internal set; }

        /// <summary>
        ///     Get the <see cref="ResourceFile" /> where this script is loaded from.
        /// </summary>
        public Dictionary<string, ResourceFile> Files { get; internal set; } = new Dictionary<string, ResourceFile>();

        /// <summary>
        ///     A <see cref="Core.Logger" /> instance that can be used to debug your resource.
        /// </summary>
        [JsonIgnore]
        // TODO: call the api and use logging sinks
        public Core.Logger Logger => throw new NotImplementedException();

        /// <summary>
        ///     Get all <see cref="ClientScript" /> instance in this resource.
        /// </summary>
        [JsonIgnore]
        public List<ClientScript> Scripts { get; } = SHVDN.Core.ListScripts().OfType<ClientScript>().ToList();

    }

}
