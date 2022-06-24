using System;
using RageCoop.Core.Scripting;

namespace RageCoop.Server.Scripting
{
    /// <summary>
    /// Inherit from this class, constructor will be called automatically, but other scripts might have yet been loaded and <see cref="API"/> will be null, you should use <see cref="OnStart"/>. to initiate your script.
    /// </summary>
    public abstract class ServerScript : Scriptable
    {
        /// <summary>
        /// This method would be called from main thread after all scripts have been loaded.
        /// </summary>
        public override abstract void OnStart();

        /// <summary>
        /// This method would be called from main thread when the server is shutting down, you MUST terminate all background jobs/threads in this method.
        /// </summary>
        public override abstract void OnStop();

        public API API { get; set; }
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class Command : Attribute
    {
        /// <summary>
        /// Sets name of the command
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Set the Usage (Example: "Please use "/help"". ArgsLength required!)
        /// </summary>
        public string Usage { get; set; }

        /// <summary>
        /// Set the length of arguments (Example: 2 for "/message USERNAME MESSAGE". Usage required!)
        /// </summary>
        public short ArgsLength { get; set; }

        public Command(string name)
        {
            Name = name;
        }
    }

    public class CommandContext
    {
        /// <summary>
        /// Gets the client which executed the command
        /// </summary>
        public Client Client { get; internal set; }

        /// <summary>
        /// Gets the arguments (Example: "/message USERNAME MESSAGE", Args[0] for USERNAME)
        /// </summary>
        public string[] Args { get; internal set; }
    }
}
