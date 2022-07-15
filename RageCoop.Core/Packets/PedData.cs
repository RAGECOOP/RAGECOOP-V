using System;
using System.Collections.Generic;
using System.Text;
using GTA;
using GTA.Math;

namespace RageCoop.Core
{
    internal class PedData
    {
        public int ID { get; set; }
        public int OwnerID { get; set; }
        public PedDataFlags Flag { get; set; }

        public int Health { get; set; }

        public Vector3 Position { get; set; }

        public Vector3 Rotation { get; set; }

        public Vector3 Velocity { get; set; }

        public Vector3 RotationVelocity { get; set; }

        public byte Speed { get; set; }

        public Vector3 AimCoords { get; set; }

        public uint CurrentWeaponHash { get; set; }

        public float Heading { get; set; }
        public PedStateData State { get; set; }
    }
    internal class PedStateData
    {

        public int ModelHash { get; set; }

        public byte[] Clothes { get; set; }

        public Dictionary<uint, bool> WeaponComponents { get; set; }

        public byte WeaponTint { get; set; }
        public BlipColor BlipColor { get; set; } = (BlipColor)255;

        public BlipSprite BlipSprite { get; set; } = 0;
        public float BlipScale { get; set; } = 1;
    }
}
