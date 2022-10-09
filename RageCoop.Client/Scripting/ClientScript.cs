using RageCoop.Core.Scripting;

namespace RageCoop.Client.Scripting
{
    /// <summary>
    /// Inherit from this class, constructor will be called automatically, but other scripts might have yet been loaded, you should use <see cref="OnStart"/>. to initiate your script.
    /// </summary>
    [GTA.ScriptAttributes(Author = "RageCoop", NoDefaultInstance = true, SupportURL = "https://github.com/RAGECOOP/RAGECOOP-V")]
    public abstract class ClientScript : GTA.Script
    {
        /// <summary>
        /// An <see cref="Scripting.API"/> instance to communicate with RageCoop
        /// </summary>
        protected static API API => Main.API;

        /// <summary>
        /// This method would be called from main thread, right after the constructor.
        /// </summary>
        public abstract void OnStart();

        /// <summary>
        /// This method would be called from main thread right before the whole <see cref="System.AppDomain"/> is unloded but prior to <see cref="GTA.Script.Aborted"/>.
        /// </summary>
        public abstract void OnStop();

        /// <summary>
        /// Get the <see cref="ResourceFile"/> instance where this script is loaded from.
        /// </summary>
        public ResourceFile CurrentFile { get; internal set; }

        /// <summary>
        /// Get the <see cref="ClientResource"/> that this script belongs to.
        /// </summary>
        public ClientResource CurrentResource { get; internal set; }

        /// <summary>
        /// Eqivalent of <see cref="ClientResource.Logger"/> in <see cref="CurrentResource"/>
        /// </summary>
        public Core.Logger Logger => CurrentResource.Logger;

    }
}
