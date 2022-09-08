using RageCoop.Core.Scripting;
using System;

namespace RageCoop.Server.Scripting
{
    /// <summary>
    /// Inherit from this class, constructor will be called automatically, but other scripts might have yet been loaded and <see cref="API"/> will be null, you should use <see cref="OnStart"/>. to initiate your script.
    /// </summary>
    public abstract class ServerScript
    {
        /// <summary>
        /// This method would be called from listener thread after all scripts have been loaded.
        /// </summary>
        public abstract void OnStart();

        /// <summary>
        /// This method would be called from listener thread when the server is shutting down, you MUST terminate all background jobs/threads in this method.
        /// </summary>
        public abstract void OnStop();

        /// <summary>
        /// Get the <see cref="Scripting.API"/> instance that can be used to control the server.
        /// </summary>
        public API API { get; set; }

        /// <summary>
        /// Get the <see cref="ServerResource"/> this script belongs to, this property won't be initiated before <see cref="OnStart"/>.
        /// </summary>
        public ServerResource CurrentResource { get; internal set; }
        /// <summary>
        /// Get the <see cref="ResourceFile"/> that the script belongs to.
        /// </summary>
        public ResourceFile CurrentFile { get; internal set; }

        /// <summary>
        /// Eqivalent of <see cref="ServerResource.Logger"/> in <see cref="CurrentResource"/>
        /// </summary>
        public Core.Logger Logger => CurrentResource.Logger;
    }
    /// <summary>
    /// Decorate your method with this attribute and use <see cref="API.RegisterCommands{T}"/> or <see cref="API.RegisterCommands(object)"/> to register commands.
    /// </summary>
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name">Name of the command</param>
        public Command(string name)
        {
            Name = name;
        }
    }

    /// <summary>
    /// The context containg command information. 
    /// </summary>
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
