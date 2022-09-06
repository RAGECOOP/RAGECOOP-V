using RageCoop.Core.Scripting;

namespace RageCoop.Client.Scripting
{
    /// <summary>
    /// Inherit from this class, constructor will be called automatically, but other scripts might have yet been loaded, you should use <see cref="OnStart"/>. to initiate your script.
    /// </summary>
    public abstract class ClientScript
    {
        /// <summary>
        /// This method would be called from background thread, call <see cref="API.QueueAction(System.Action)"/> to dispatch it to main thread.
        /// </summary>
        public abstract void OnStart();

        /// <summary>
        /// This method would be called from background thread when the client disconnected from the server, you MUST terminate all background jobs/threads in this method.
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
