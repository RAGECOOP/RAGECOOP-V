using System.Collections.Generic;

namespace CoopServer
{
    public class Blocklist
    {
        public List<string> Username { get; set; } = new();
        public List<string> IP { get; set; } = new();
    }
}
