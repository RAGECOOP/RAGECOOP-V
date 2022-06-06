using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RageCoop.Core;
using Lidgren.Network;

namespace RageCoop.Server.Scripting
{
    public abstract class ServerScript
    {
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class TriggerEvent : Attribute
    {
        public string EventName { get; set; }

        public TriggerEvent(string eventName)
        {
            EventName = eventName;
        }
    }

    public class EventContext
    {
        /// <summary>
        /// Gets the client which executed the command
        /// </summary>
        public Client Client { get; internal set; }

        /// <summary>
        /// Arguments (all standard but no string!)
        /// </summary>
        public object[] Args { get; internal set; }
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
