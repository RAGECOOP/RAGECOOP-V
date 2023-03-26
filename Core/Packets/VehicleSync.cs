using System.Collections.Generic;
using GTA;
using GTA.Math;
using Lidgren.Network;

namespace RageCoop.Core
{
    internal partial class Packets
    {
        public class VehicleSync : Packet
        {
            public override PacketType Type => PacketType.VehicleSync;
            public EntityData ED;
            public VehicleData VD;
            public VehicleDataFull VDF;
            public VehicleDataVar VDV;

            protected override void Serialize(NetOutgoingMessage m)
            {
                m.Write(ref ED);
                m.Write(ref VD);
                if (VD.Flags.HasVehFlag(VehicleDataFlags.IsFullSync))
                {
                    m.Write(ref VDF);
                    VDV.WriteTo(m);
                }
            }

            public override void Deserialize(NetIncomingMessage m)
            {
                m.Read(out ED);
                m.Read(out VD);
                if (VD.Flags.HasVehFlag(VehicleDataFlags.IsFullSync))
                {
                    m.Read(out VDF);
                    VDV.ReadFrom(m);
                }
            }
        }
    }
}