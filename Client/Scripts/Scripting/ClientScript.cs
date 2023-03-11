using GTA;
using RageCoop.Core;
using RageCoop.Core.Scripting;

namespace RageCoop.Client.Scripting
{
    public abstract class ClientScript : Script
    {
        /// <summary>
        ///     Get the <see cref="ResourceFile" /> instance where this script is loaded from.
        /// </summary>
        public ResourceFile CurrentFile { get; internal set; }

        /// <summary>
        ///     Get the <see cref="ClientResource" /> that this script belongs to.
        /// </summary>
        public ClientResource CurrentResource { get; internal set; }

        /// <summary>
        ///     Eqivalent of <see cref="ClientResource.Logger" /> in <see cref="CurrentResource" />
        /// </summary>
        public Logger Logger => CurrentResource.Logger;
    }
}