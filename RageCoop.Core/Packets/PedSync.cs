using System;
using System.Collections.Generic;
using System.Text;
using GTA.Math;
using GTA;
using Lidgren.Network;

namespace RageCoop.Core
{
    internal partial class Packets
    {


        internal class PedSync : Packet
        {
            public override PacketType Type  => PacketType.PedSync;
            public int ID { get; set; }

            public int OwnerID { get; set; }
            public PedDataFlags Flags { get; set; }

            public int Health { get; set; }

            public Vector3 Position { get; set; }

            public Vector3 Rotation { get; set; }

            public Vector3 Velocity { get; set; }

            #region RAGDOLL
            public Vector3 HeadPosition { get; set; }
            public Vector3 RightFootPosition { get; set; }
            public Vector3 LeftFootPosition { get; set; }

            #endregion

            public byte Speed { get; set; }

            public Vector3 AimCoords { get; set; }


            public float Heading { get; set; }
            
            #region FULL

            public int ModelHash { get; set; }

            public uint CurrentWeaponHash { get; set; }

            public byte[] Clothes { get; set; }

            public Dictionary<uint, bool> WeaponComponents { get; set; }

            public byte WeaponTint { get; set; }
            public BlipColor BlipColor { get; set; } = (BlipColor)255;

            public BlipSprite BlipSprite { get; set; } = 0;
            public float BlipScale { get; set; } = 1;
#endregion

            public override byte[] Serialize()
            {
                
                List<byte> byteArray = new List<byte>();

                // Write ped ID
                byteArray.AddInt(ID);

                // Write OwnerID
                byteArray.AddInt(OwnerID);


                // Write ped flags
                byteArray.AddRange(BitConverter.GetBytes((ushort)Flags));

                // Write ped health
                byteArray.AddRange(BitConverter.GetBytes(Health));

                // ragdoll sync
                if (Flags.HasPedFlag(PedDataFlags.IsRagdoll))
                {
                    byteArray.AddVector3(HeadPosition);
                    byteArray.AddVector3(RightFootPosition);
                    byteArray.AddVector3(LeftFootPosition);

                }
                else
                {
                    // Write ped position
                    byteArray.AddVector3(Position);
                }

                // Write ped rotation
                byteArray.AddVector3(Rotation);

                // Write ped velocity
                byteArray.AddVector3(Velocity);


                // Write ped speed
                byteArray.Add(Speed);


                if (Flags.HasPedFlag(PedDataFlags.IsAiming))
                {
                    // Write ped aim coords
                    byteArray.AddVector3(AimCoords);
                }

                byteArray.AddFloat(Heading);

                if (Flags.HasPedFlag(PedDataFlags.IsFullSync))
                {
                    // Write model hash
                    byteArray.AddInt(ModelHash);

                    // Write ped weapon hash
                    byteArray.AddUint(CurrentWeaponHash);

                    byteArray.AddRange(Clothes);

                    // Write player weapon components
                    if (WeaponComponents != null)
                    {
                        byteArray.Add(0x01);
                        byteArray.AddRange(BitConverter.GetBytes((ushort)WeaponComponents.Count));
                        foreach (KeyValuePair<uint, bool> component in WeaponComponents)
                        {
                            byteArray.AddRange(BitConverter.GetBytes(component.Key));
                            byteArray.AddRange(BitConverter.GetBytes(component.Value));
                        }
                    }
                    else
                    {
                        // Player weapon doesn't have any components
                        byteArray.Add(0x00);
                    }

                    byteArray.Add(WeaponTint);

                    byteArray.Add((byte)BlipColor);
                    if ((byte)BlipColor!=255)
                    {
                        byteArray.AddUshort((ushort)BlipSprite);
                        byteArray.AddFloat(BlipScale);
                    }
                }

                return byteArray.ToArray();
            }

            public override void Deserialize(byte[] array)
            {
                #region NetIncomingMessageToPacket
                BitReader reader = new BitReader(array);

                // Read player netHandle
                ID = reader.ReadInt32();

                OwnerID=reader.ReadInt32();

                // Read player flags
                Flags = (PedDataFlags)reader.ReadUInt16();

                // Read player health
                Health = reader.ReadInt32();

                if (Flags.HasPedFlag(PedDataFlags.IsRagdoll))
                {
                    HeadPosition=reader.ReadVector3();
                    RightFootPosition=reader.ReadVector3();
                    LeftFootPosition=reader.ReadVector3();
                    Position=HeadPosition;
                }
                else
                {
                    // Read player position
                    Position = reader.ReadVector3();
                }

                // Read player rotation
                Rotation = reader.ReadVector3();

                // Read player velocity
                Velocity = reader.ReadVector3();

                // Read player speed
                Speed = reader.ReadByte();


                // Try to read aim coords
                if (Flags.HasPedFlag(PedDataFlags.IsAiming))
                {
                    // Read player aim coords
                    AimCoords = reader.ReadVector3();
                }

                Heading=reader.ReadSingle();

                if (Flags.HasPedFlag(PedDataFlags.IsFullSync))
                {
                    // Read player model hash
                    ModelHash = reader.ReadInt32();

                    // Read player weapon hash
                    CurrentWeaponHash = reader.ReadUInt32();

                    // Read player clothes
                    Clothes =reader.ReadBytes(36);

                    // Read player weapon components
                    if (reader.ReadBoolean())
                    {
                        WeaponComponents = new Dictionary<uint, bool>();
                        ushort comCount = reader.ReadUInt16();
                        for (ushort i = 0; i < comCount; i++)
                        {
                            WeaponComponents.Add(reader.ReadUInt32(), reader.ReadBoolean());
                        }
                    }
                    WeaponTint=reader.ReadByte();

                    BlipColor=(BlipColor)reader.ReadByte();

                    if ((byte)BlipColor!=255)
                    {
                        BlipSprite=(BlipSprite)reader.ReadUInt16();
                        BlipScale=reader.ReadSingle();
                    }
                }
                #endregion
            }
        }




    }
}
