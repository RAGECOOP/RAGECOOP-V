using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace RageCoop.Server.Scripting
{
    /// <summary>
    /// 
    /// </summary>
    public class ChatEventArgs : EventArgs
    {
        /// <summary>
        /// The client that sent this message
        /// </summary>
        public Client Sender { get; set; }
        /// <summary>
        /// Message
        /// </summary>
        public string Message { get; set; }
    }
    /// <summary>
    /// 
    /// </summary>
    public class CustomEventReceivedArgs : EventArgs
    {
        /// <summary>
        /// The <see cref="Client"/> that triggered this event
        /// </summary>
        public Client Sender { get; set; }
        /// <summary>
        /// The event hash
        /// </summary>
        public int Hash { get; set; }
        /// <summary>
        /// The arguments of this event
        /// </summary>
        public List<object> Args { get; set; }
    }
    /// <summary>
    /// 
    /// </summary>
    public class OnCommandEventArgs : EventArgs
    {
        /// <summary>
        /// The <see cref="Client"/> that executed this command.
        /// </summary>
        public Client Sender { get; set; }
        /// <summary>
        /// The name of executed command
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Arguments
        /// </summary>
        public string[] Args { get; set; }
        /// <summary>
        /// If this value was set to true, corresponding handler registered with <see cref="API.RegisterCommand(string, Action{CommandContext})"/> will not be invoked.
        /// </summary>
        public bool Cancel { get; set; } = false;
    }
    /// <summary>
    /// 
    /// </summary>
    public class HandshakeEventArgs : EventArgs
    {
        /// <summary>
        /// The player's ID
        /// </summary>
        public int ID { get; set; }
        /// <summary>
        /// The claimed username
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// The client password hashed with SHA256 algorithm.
        /// </summary>
        public string PasswordHash { get; set; }

        /// <summary>
        /// The <see cref="EndPoint"/> that sent the handshake request.
        /// </summary>
        public IPEndPoint EndPoint { get; set; }
        /// <summary>
        /// Deny the connection attempt
        /// </summary>
        /// <param name="reason"></param>
        public void Deny(string reason)
        {
            DenyReason=reason;
            Cancel=true;
        }
        internal string DenyReason { get; set; }
        internal bool Cancel { get; set; }=false;
    }
}
