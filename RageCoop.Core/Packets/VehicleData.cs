using System;
using System.Collections.Generic;
using System.Text;
using GTA;
using GTA.Math;

namespace RageCoop.Core
{
    internal class VehicleData
    {
        public int ID { get; set; }

        public int OwnerID { get; set; }

        public Vector3 Position { get; set; }

        public Quaternion Quaternion { get; set; }
        // public Vector3 Rotation { get; set; }

        public Vector3 Velocity { get; set; }

        public Vector3 RotationVelocity { get; set; }

        public float ThrottlePower { get; set; }
        public float BrakePower { get; set; }
        public float SteeringAngle { get; set; }
        public float DeluxoWingRatio { get; set; } = -1;

        public VehicleStateData State;
    }
    internal class VehicleStateData
    {
        public int ModelHash { get; set; }

        public float EngineHealth { get; set; }

        public byte[] Colors { get; set; }

        public Dictionary<int, int> Mods { get; set; }

        public VehicleDamageModel DamageModel { get; set; }

        public byte LandingGear { get; set; }
        public byte RoofState { get; set; }

        public VehicleDataFlags Flag { get; set; }


        public VehicleLockStatus LockStatus { get; set; }

        public int Livery { get; set; } = -1;

        /// <summary>
        /// VehicleSeat,PedID
        /// </summary>
        public Dictionary<int, int> Passengers { get; set; }

        public byte RadioStation { get; set; } = 255;
        public string LicensePlate { get; set; }
    }
}
