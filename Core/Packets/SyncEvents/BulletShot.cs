﻿using GTA.Math;
using Lidgren.Network;

namespace RageCoop.Core
{
    internal partial class Packets
    {
        internal class BulletShot : Packet
        {
            public override PacketType Type => PacketType.BulletShot;
            public int OwnerID { get; set; }

            public uint WeaponHash { get; set; }
            
            public LVector3 EndPosition { get; set; }

            protected override void Serialize(NetOutgoingMessage m)
            {
                // Write OwnerID 
                m.Write(OwnerID);

                // Write weapon hash
                m.Write(WeaponHash);

                // Write EndPosition
                m.Write(EndPosition);
            }

            public override void Deserialize(NetIncomingMessage m)
            {
                #region NetIncomingMessageToPacket

                // Read OwnerID
                OwnerID = m.ReadInt32();

                // Read WeaponHash
                WeaponHash = m.ReadUInt32();

                // Read EndPosition
                EndPosition = m.ReadVector3();

                #endregion
            }
        }
    }
}