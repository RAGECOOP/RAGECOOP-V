using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RageCoop.Client.Scripting
{
    public class PlayerInfo
    {
        public byte HolePunchStatus { get; internal set; }
        public bool IsHost { get; internal set; }
        public string Username { get; internal set; }
        public int ID { get; internal set; }
        public int EntityHandle { get; internal set; }
        public IPEndPoint InternalEndPoint { get; internal set; }
        public IPEndPoint ExternalEndPoint { get; internal set; }
        public float Ping { get; internal set; }
        public float PacketTravelTime { get; internal set; }
        public bool DisplayNameTag { get; internal set; }
        public bool HasDirectConnection { get; internal set; }
    }
}
