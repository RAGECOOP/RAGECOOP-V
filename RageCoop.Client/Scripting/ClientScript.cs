using RageCoop.Core.Scripting;

namespace RageCoop.Client.Scripting
{
    /// <summary>
    /// Inherit from this class, constructor will be called automatically, but other scripts might have yet been loaded, you should use <see cref="OnStart"/>. to initiate your script.
    /// </summary>
    public abstract class ClientScript
    {
        /// <summary>
        /// This method would be called from main thread shortly after all scripts have been loaded.
        /// </summary>
        public abstract void OnStart();

        /// <summary>
        /// This method would be called from main thread when the client disconnected from the server, you MUST terminate all background jobs/threads in this method.
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

    }
}
