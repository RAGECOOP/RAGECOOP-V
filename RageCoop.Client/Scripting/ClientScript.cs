namespace RageCoop.Client.Scripting
{
    /// <summary>
    /// Inherit from this class, constructor will be called automatically, but other scripts might have yet been loaded, you should use <see cref="OnStart"/>. to initiate your script.
    /// </summary>
    public abstract class ClientScript:Core.Scripting.Scriptable
    {
        /// <summary>
        /// This method would be called from main thread shortly after all scripts have been loaded.
        /// </summary>
        public override abstract void OnStart();

        /// <summary>
        /// This method would be called from main thread when the client disconnected from the server, you MUST terminate all background jobs/threads in this method.
        /// </summary>
        public override abstract void OnStop();

    }
}
