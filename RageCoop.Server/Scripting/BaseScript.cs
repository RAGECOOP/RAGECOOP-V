using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RageCoop.Core.Scripting;

namespace RageCoop.Server.Scripting
{
    internal class BaseScript:ServerScript
    {
        public override void OnStart()
        {
        }
        public override void OnStop()
        {
        }
        public void SetAutoRespawn(Client c,bool toggle)
        {
            c.SendCustomEvent(CustomEvents.SetAutoRespawn, new() { toggle });
        }
    }
}
