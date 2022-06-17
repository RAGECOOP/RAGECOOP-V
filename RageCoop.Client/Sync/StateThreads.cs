using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA;
/*
namespace RageCoop.Client.Sync
{
    internal class VehicleStateThread : Script
    {
        public VehicleStateThread()
        {
            Tick+=OnTick;
        }
        int current;
        int toSendPerFrame;
        int sent;
        private void OnTick(object sender, EventArgs e)
        {
            toSendPerFrame=EntityPool.allVehicles.Length*5/(int)Game.FPS+1;
            if (!Networking.IsOnServer) { return; }
            for(; sent<toSendPerFrame; sent++)
            {
                if (current>=EntityPool.allVehicles.Length)
                {
                    current=0;
                }
                Networking.SendVehicleState(EntityPool.allVehicles[current])
            }
        }
    }
}
*/