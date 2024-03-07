using System.Collections.Generic;
using GTA;
using GTA.Math;
using Lidgren.Network;

namespace RageCoop.Core
{
    internal partial class Packets
    {
        internal class PedSync : Packet
        {
            public override PacketType Type => PacketType.PedSync;
            public int ID { get; set; }

            public int OwnerID { get; set; }

            public int VehicleID { get; set; }
            public VehicleSeat Seat { get; set; }
            public PedDataFlags Flags { get; set; }

            public int Health { get; set; }

            public LVector3 Position { get; set; }

            public LVector3 Rotation { get; set; }

            public LVector3 Velocity { get; set; }

            public byte Speed { get; set; }

            public LVector3 AimCoords { get; set; }


            public float Heading { get; set; }

            protected override void Serialize(NetOutgoingMessage m)
            {
                m.Write(ID);
                m.Write(OwnerID);
                m.Write((ushort)Flags);
                m.Write(Health);
                m.Write(Speed);
                if (Flags.HasPedFlag(PedDataFlags.IsRagdoll))
                {
                    m.Write(HeadPosition);
                    m.Write(RightFootPosition);
                    m.Write(LeftFootPosition);
                }
                else
                {
                    if (Speed >= 4)
                    {
                        m.Write(VehicleID);
                        m.Write((byte)(Seat + 3));
                    }

                    m.Write(Position);
                }

                m.Write(Rotation);
                m.Write(Velocity);


                if (Flags.HasPedFlag(PedDataFlags.IsAiming)) m.Write(AimCoords);

                m.Write(Heading);

                if (Flags.HasPedFlag(PedDataFlags.IsFullSync))
                {
                    m.Write(ModelHash);
                    m.Write((uint)CurrentWeapon);
                    m.Write(Clothes);
                    if (WeaponComponents != null)
                    {
                        m.Write(true);
                        m.Write((ushort)WeaponComponents.Count);
                        foreach (var component in WeaponComponents)
                        {
                            m.Write(component.Key);
                            m.Write(component.Value);
                        }
                    }
                    else
                    {
                        // Player weapon doesn't have any components
                        m.Write(false);
                    }

                    m.Write(WeaponTint);

                    // In vehicle
                    if (Speed == 4)
                        m.Write((uint)VehicleWeapon);

                    m.Write((byte)BlipColor);
                    if ((byte)BlipColor != 255)
                    {
                        m.Write((ushort)BlipSprite);
                        m.Write(BlipScale);
                    }
                }
            }

            public override void Deserialize(NetIncomingMessage m)
            {
                #region NetIncomingMessageToPacket

                ID = m.ReadInt32();
                OwnerID = m.ReadInt32();
                Flags = (PedDataFlags)m.ReadUInt16();
                Health = m.ReadInt32();
                Speed = m.ReadByte();

                if (Flags.HasPedFlag(PedDataFlags.IsRagdoll))
                {
                    HeadPosition = m.ReadVector3();
                    RightFootPosition = m.ReadVector3();
                    LeftFootPosition = m.ReadVector3();
                    Position = HeadPosition;
                }
                else
                {
                    // Vehicle related
                    if (Speed >= 4)
                    {
                        VehicleID = m.ReadInt32();
                        Seat = (VehicleSeat)(m.ReadByte() - 3);
                    }

                    // Read player position
                    Position = m.ReadVector3();
                }

                Rotation = m.ReadVector3();
                Velocity = m.ReadVector3();

                if (Flags.HasPedFlag(PedDataFlags.IsAiming))
                    // Read player aim coords
                    AimCoords = m.ReadVector3();

                Heading = m.ReadFloat();

                if (Flags.HasPedFlag(PedDataFlags.IsFullSync))
                {
                    // Read player model hash
                    ModelHash = m.ReadInt32();

                    // Read player weapon hash
                    CurrentWeapon = (WeaponHash)m.ReadUInt32();

                    // Read player clothes
                    Clothes = m.ReadBytes(36);

                    // Read player weapon components
                    if (m.ReadBoolean())
                    {
                        WeaponComponents = new Dictionary<uint, bool>();
                        var comCount = m.ReadUInt16();
                        for (ushort i = 0; i < comCount; i++) WeaponComponents.Add(m.ReadUInt32(), m.ReadBoolean());
                    }

                    WeaponTint = m.ReadByte();

                    if (Speed == 4)
                        VehicleWeapon = (VehicleWeaponHash)m.ReadUInt32();

                    BlipColor = (BlipColor)m.ReadByte();

                    if ((byte)BlipColor != 255)
                    {
                        BlipSprite = (BlipSprite)m.ReadUInt16();
                        BlipScale = m.ReadFloat();
                    }
                }

                #endregion
            }

            #region RAGDOLL

            public LVector3 HeadPosition { get; set; }
            public LVector3 RightFootPosition { get; set; }
            public LVector3 LeftFootPosition { get; set; }

            #endregion

            #region FULL

            public int ModelHash { get; set; }

            public WeaponHash CurrentWeapon { get; set; }

            public VehicleWeaponHash VehicleWeapon { get; set; }

            public byte[] Clothes { get; set; }

            public Dictionary<uint, bool> WeaponComponents { get; set; }

            public byte WeaponTint { get; set; }
            public BlipColor BlipColor { get; set; } = (BlipColor)255;

            public BlipSprite BlipSprite { get; set; } = 0;
            public float BlipScale { get; set; } = 1;

            #endregion
        }
    }
}