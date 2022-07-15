using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Text;
using GTA;  

namespace RageCoop.Core
{

    internal class EntitiesData
    {
        public EntitiesData()
        {

        }
        public EntitiesData(NetIncomingMessage msg)
        {
            Peds=new PedData[msg.ReadInt32()];
            Vehicles=new VehicleData[msg.ReadInt32()];

            for (int i = 0; i<Peds.Length; i++)
            {
                Peds[i]=msg.ReadPed();
            }
            for (int i = 0; i<Vehicles.Length; i++)
            {
                Vehicles[i]=msg.ReadVehicle();
            }
        }
        public PedData[] Peds;
        public VehicleData[] Vehicles;
        public void WriteTo(NetOutgoingMessage message)
        {
            message.Write((byte)PacketType.EntitySync);

            // Write length 
            message.Write(Peds.Length);
            message.Write(Vehicles.Length);

            foreach (var p in Peds)
            {
                message.Write(p);
            }
            foreach (var v in Vehicles)
            {
                message.Write(v);
            }

        }

    }
}
