using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace RageCoop.Server.Scripting
{
    public class ChatEventArgs : EventArgs
    {
        public Client Sender { get; set; }
        public string Message { get; set; }
    }
    public class CustomEventReceivedArgs : EventArgs
    {
        public Client Sender { get; set; }
        public int Hash { get; set; }
        public List<object> Args { get; set; }
    }
    public class OnCommandEventArgs : EventArgs
    {
        public Client Sender { get; set; }
        public string Name { get; set; }
        public string[] Args { get; set; }
        /// <summary>
        /// If this value was set to true, corresponding handler registered with <see cref="API.RegisterCommand(string, Action{CommandContext})"/> will not be invoked.
        /// </summary>
        public bool Cancel { get; set; } = false;
    }
    public class HandshakeEventArgs : EventArgs
    {
        public int ID { get; set; }
        public string Username { get; set; }
        public IPEndPoint EndPoint { get; set; }
        public void Deny(string reason)
        {
            DenyReason=reason;
            Cancel=true;
        }
        internal string DenyReason { get; set; }
        internal bool Cancel { get; set; }=false;
    }
}
