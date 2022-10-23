using GTA;
using RageCoop.Core;
using RageCoop.Core.Scripting;

namespace RageCoop.Client.Scripting
{
    /// <summary>
    ///     Inherit from this class, constructor will be called automatically, but other scripts might have yet been loaded,
    ///     you should use <see cref="OnStart" />. to initiate your script.
    /// </summary>
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

        /// <summary>
        ///     This method would be called from main thread, right after all script constructors are invoked.
        /// </summary>
        public abstract void OnStart();

        /// <summary>
        ///     This method would be called from main thread right before the whole <see cref="System.AppDomain" /> is unloded but
        ///     prior to <see cref="GTA.Script.Aborted" />.
        /// </summary>
        public abstract void OnStop();
    }
}