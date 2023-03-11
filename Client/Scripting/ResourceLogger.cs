using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace RageCoop.Client.Scripting
{
    public class ResourceLogger : Core.Logger
    {
        public static readonly ResourceLogger Default = new();
        public ResourceLogger()
        {
            FlushImmediately = true;
            OnFlush += FlushToMainModule;
        }

        private unsafe void FlushToMainModule(LogLine line, string fomatted)
        {
            fixed (char* pMsg = line.Message)
            {
                APIBridge.LogEnqueue(line.LogLevel, pMsg);
            }
        }

    }
}
