using GTA;
using RageCoop.Core.Scripting;

namespace RageCoop.Client.Scripting
{
    [ScriptAttributes(NoDefaultInstance = true)]
    public abstract class ClientScript : Script
    {
        /// <summary>
        ///     Get the <see cref="ResourceFile" /> instance where this script is loaded from.
        /// </summary>
        public ClientFile CurrentFile { get; internal set; }

        /// <summary>
        ///     Get the <see cref="ClientResource" /> that this script belongs to.
        /// </summary>
        public ClientResource CurrentResource { get; internal set; }

        /// <summary>
        ///     Eqivalent of <see cref="ClientResource.Logger" /> in <see cref="Script.CurrentResource" />
        /// </summary>
        public Core.Logger Logger => CurrentResource.Logger;
    }
}
