using RageCoop.Core.Scripting;
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
        ///     Get all <see cref="ClientScript" /> instance in this resource.
        /// </summary>
        public List<ClientScript> Scripts { get; internal set; } = new List<ClientScript>();

        /// <summary>
        ///     Get the <see cref="ResourceFile" /> where this script is loaded from.
        /// </summary>
        public Dictionary<string, ResourceFile> Files { get; internal set; } = new Dictionary<string, ResourceFile>();

        /// <summary>
        ///     A <see cref="Core.Logger" /> instance that can be used to debug your resource.
        /// </summary>
        public Core.Logger Logger { get; internal set; }
    }

    public class PlayerInfo
    {
        public byte HolePunchStatus { get; internal set; }
        public bool IsHost { get; internal set; }
        public string Username { get; internal set; }
        public int ID { get; internal set; }
        public int EntityHandle { get; internal set; }
        public IPEndPoint InternalEndPoint { get; internal set; }
        public IPEndPoint ExternalEndPoint { get; internal set; }
        public float Ping { get; internal set; }
        public float PacketTravelTime { get; internal set; }
        public bool DisplayNameTag { get; internal set; }
        public bool HasDirectConnection { get; internal set; }
    }
}
