using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RageCoop.Client.Scripting
{
    /// <summary>
    /// Inherit from this class, constructor will be called when the script is loaded.
    /// </summary>
    public abstract class ClientScript:Core.Scripting.IScriptable
    {
        public abstract void OnStart();
        public abstract void OnStop();
    }
}
