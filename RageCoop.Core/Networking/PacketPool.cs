using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.ObjectPool;
namespace RageCoop.Core
{
    internal class PacketPool
    {
        public ObjectPool<Packets.VehicleSync> VehicleSyncPool=ObjectPool.Create<Packets.VehicleSync>();
        public ObjectPool<Packets.PedSync> PedSyncPool = ObjectPool.Create<Packets.PedSync>();

        public void Recycle(Packets.VehicleSync p)
        {
            VehicleSyncPool.Return(p);
        }
        public void Recycle(Packets.PedSync p)
        {
            PedSyncPool.Return(p);
        }
        public Packets.PedSync GetPedPacket()
        {
            return PedSyncPool.Get();
        }
        public Packets.VehicleSync GetVehiclePacket()
        {
            return VehicleSyncPool.Get();
        }
        public T Get<T>() where T : Packet
        {
            var type=typeof(T);
            if (type==typeof(Packets.VehicleSync))
            {
                return (T)(Packet)VehicleSyncPool.Get();
            }
            else if (type==typeof(Packets.PedSync))
            {
                return (T)(Packet)PedSyncPool.Get();
            }
            return null;
        }
    }
}
