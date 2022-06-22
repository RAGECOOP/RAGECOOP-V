using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RageCoop.Core.Scripting;

namespace RageCoop.Client.Scripting
{
    internal class BaseScript : ClientScript
    {
        public override void OnStart()
        {
            API.RegisterCustomEventHandler(CustomEvents.SetAutoRespawn,SetAutoRespawn);
        }

        public override void OnStop()
        {
        }
        void SetAutoRespawn(CustomEventReceivedArgs args)
        {
            API.Config.EnableAutoRespawn=(bool)args.Args[0];
        }
    }
}
