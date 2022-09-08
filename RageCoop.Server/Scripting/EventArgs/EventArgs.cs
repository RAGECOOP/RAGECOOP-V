using System;
using System.Collections.Generic;
using System.Net;

namespace RageCoop.Server.Scripting
{
    /// <summary>
    /// 
    /// </summary>
    public class ChatEventArgs : EventArgs
    {
        /// <summary>
        /// The client that sent this message, will be null if sent from server
        /// </summary>
        public Client Client { get; set; }
        /// <summary>
        /// Message
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Only used when sending a message via <see cref="API.SendChatMessage(string, List{Client}, string, bool?)"/>
        /// </summary>
        public string ClaimedSender { get; set; }
    }
    /// <summary>
    /// 
    /// </summary>
    public class CustomEventReceivedArgs : EventArgs
    {
        /// <summary>
        /// The <see cref="RageCoop.Server.Client"/> that triggered this event
        /// </summary>
        public Client Client { get; set; }

        /// <summary>
        /// The event hash
        /// </summary>
        public int Hash { get; set; }

        /// <summary>
        /// Supported types: byte, short, ushort, int, uint, long, ulong, float, bool, string, Vector3, Quaternion, Vector2 <see cref="ServerObject.Handle"/>
        /// </summary>
        public object[] Args { get; set; }
    }
    /// <summary>
    /// 
    /// </summary>
    public class OnCommandEventArgs : EventArgs
    {
        /// <summary>
        /// The <see cref="RageCoop.Server.Client"/> that executed this command, will be null if sent from server.
        /// </summary>
        public Client Client { get; set; }
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
            DenyReason = reason;
            Cancel = true;
        }
        internal string DenyReason { get; set; }
        internal bool Cancel { get; set; } = false;
    }
}
