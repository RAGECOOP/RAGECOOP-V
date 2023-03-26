using System;
using System.Collections.Generic;
using System.Linq;
using GTA.Math;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace RageCoop.Core
{

    internal struct LQuaternion
    {
        public LQuaternion(float x, float y, float z, float w)
        {
            X = x; Y = y; Z = z; W = w;
        }
        public float X, Y, Z, W;
        public static implicit operator LQuaternion(Quaternion q) => new(q.X, q.Y, q.Z, q.W);
        public static implicit operator Quaternion(LQuaternion q) => new(q.X, q.Y, q.Z, q.W);
    }
}
