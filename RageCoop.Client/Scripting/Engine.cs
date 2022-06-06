using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;
using System.CodeDom.Compiler;

namespace RageCoop.Client.Scripting
{
    internal class Engine : Core.Scripting.ScriptingEngine
    {
        public Engine() : base("RageCoop.Client.Scripting.ClientScript", Main.Logger) { }

    }
}
