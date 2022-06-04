using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RageCoop.Core.Scripting;
namespace RageCoop.Server.Scripting
{
    internal class Engine:ScriptingEngine
    {
        public Engine() : base(typeof(ServerScript), Program.Logger)
        {

        }
        public static void Load()
        {

        }
    }
}
