using GTA;
using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RageCoop.Core
{
    /// <summary>
    /// Common data for synchronizing an entity
    /// </summary>
    internal struct EntityData
    {
        public int ID;
        public int OwnerID;
        public LQuaternion Quaternion;
        public LVector3 Position;
        public LVector3 Velocity;
        public int ModelHash;
    }

    internal struct VehicleData
    {
        public VehicleDataFlags Flags;
        public float ThrottlePower;
        public float BrakePower;
        public float SteeringAngle;
        public VehicleLockStatus LockStatus;
        public float DeluxoWingRatio;
    }
    internal struct VehicleDataFull
    {

        public float EngineHealth;
        public (byte, byte) Colors;
        public byte ToggleModsMask;
        public VehicleDamageModel DamageModel;
        public int Livery;
        public byte HeadlightColor;
        public byte RadioStation;
        public ushort ExtrasMask;
        public byte RoofState;
        public byte LandingGear;
    }

    /// <summary>
    /// Non-fixed vehicle data
    /// </summary>
    internal struct VehicleDataVar
    {
        public string LicensePlate;
        public (int, int)[] Mods;
        public void WriteTo(NetOutgoingMessage m)
        {
            m.Write(LicensePlate);
            m.Write((byte)Mods.Length);
            for(int i = 0;i < Mods.Length; i++)
            {
                m.Write(ref Mods[i]);
            }
        }
        public void ReadFrom(NetIncomingMessage m)
        {
            LicensePlate = m.ReadString();
            Mods = new (int, int)[m.ReadByte()];
            for(int i = 0; i < Mods.Length; i++)
            {
                Mods[i] = m.Read<(int, int)>();
            }
        }
    }
}
